// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// MenuScreen.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;

namespace AlienGameSample
{
  /// <summary>
  /// Base class for screens that contain a menu of options. The user can
  /// move up and down to select an entry, or cancel to back out of the screen.
  /// </summary>
  abstract class MenuScreen : GameScreen
  {
    List<MenuEntry> menuEntries = new List<MenuEntry>();
    int selectedEntry;
    string menuTitle;

    float scaleFactor = 1.0f;

    /// <summary>
    /// Gets the list of menu entries, so derived classes can add
    /// or change the menu contents.
    /// </summary>
    protected IList<MenuEntry> MenuEntries
    {
      get { return menuEntries; }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public MenuScreen(string menuTitle)
    {
      this.menuTitle = menuTitle;

      TransitionOnTime = TimeSpan.FromSeconds(0.6);
      TransitionOffTime = TimeSpan.FromSeconds(0.0);
    }

    public override void LoadContent()
    {
      base.LoadContent();
    }

    /// <summary>
    /// Responds to user input, changing the selected entry and accepting
    /// or cancelling the menu.
    /// </summary>
    /// 
    public override void HandleInput(InputState input)
    {
      // Move to the previous menu entry?
      if (input.MenuUp)
      {
        selectedEntry--;

        if (selectedEntry < 0)
          selectedEntry = menuEntries.Count - 1;
      }

      // Move to the next menu entry?
      if (input.MenuDown)
      {
        selectedEntry++;

        if (selectedEntry >= menuEntries.Count)
          selectedEntry = 0;
      }

      // Accept or cancel the menu?
      if (input.MenuSelect)
      {
        OnSelectEntry(selectedEntry);
      }
      else if (input.MenuCancel)
      {
        OnCancel();
      }
    }

    /// <summary>
    /// Handler for when the user has chosen a menu entry.
    /// </summary>
    protected virtual void OnSelectEntry(int entryIndex)
    {
      menuEntries[selectedEntry].OnSelectEntry();
    }

    /// <summary>
    /// Handler for when the user has cancelled the menu.
    /// </summary>
    protected virtual void OnCancel()
    {
      ExitScreen();
    }

    /// <summary>
    /// Helper overload makes it easy to use OnCancel as a MenuEntry event handler.
    /// </summary>
    protected void OnCancel(object sender, EventArgs e)
    {
      OnCancel();
    }

    /// <summary>
    /// Updates the menu.
    /// </summary>
    public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                   bool coveredByOtherScreen)
    {
      base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

      // Update each nested MenuEntry object.
      for (int i = 0; i < menuEntries.Count; i++)
      {
        bool isSelected = IsActive && (i == selectedEntry);

        menuEntries[i].Update(this, isSelected, gameTime);
      }
    }

    /// <summary>
    /// Draws the menu.  Tweaked a bit from the sample so that it draws menus on the bottom left corner and transitions
    /// on and off from the bottom.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
      if (menuEntries.Count > 0)
      {
        //Get the total height of the whole menu up front, for positioning
          int totalmenuHeight = 0;
          foreach (MenuEntry menuEntry in menuEntries)
          {
              totalmenuHeight += menuEntry.GetHeight(this) + menuEntry.ExtraSpacing;
          }
          //substract the last entry's extra spacing, as it won't be used.
          totalmenuHeight -= MenuEntries[MenuEntries.Count - 1].ExtraSpacing;

        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
        SpriteFont font = ScreenManager.Font;

        Vector2 position = new Vector2(60, 400 + totalmenuHeight/2);

        // Make the menu slide into place during transitions, using a
        // power curve to make things look more interesting (this makes
        // the movement slow down as it nears the end).
        float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

        spriteBatch.Begin();

        // Draw each menu entry in turn.
        for (int i = menuEntries.Count - 1; i >= 0; --i)
        {
          MenuEntry menuEntry = menuEntries[i];

          bool isSelected = IsActive && (i == selectedEntry);

          position.X = 240 - font.MeasureString(menuEntry.Text).X / 2;
          Vector2 scaledPosition = new Vector2((position.X + ScreenManager.screenX) * scaleFactor, (position.Y + ScreenManager.screenY) * scaleFactor);
          menuEntry.Draw(this, scaledPosition, isSelected, gameTime, transitionOffset);

          position.Y -= menuEntry.GetHeight(this) + menuEntry.ExtraSpacing;
        }

        DrawExtraThings();

        spriteBatch.End();

      }
    }

    public virtual void DrawExtraThings()
    {

    }
  }
}