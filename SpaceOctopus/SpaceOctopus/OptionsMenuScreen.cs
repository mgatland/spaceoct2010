using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienGameSample;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SpaceOctopus
{
    class OptionsMenuScreen : MenuScreen
    {

        public OptionsMenuScreen()
            : base("options") 
        {
         //   soundMenuEntry = new MenuEntry(SoundText());
         //   soundMenuEntry.Selected += SoundMenuEntrySelected;
         //   MenuEntries.Add(soundMenuEntry);

            MenuEntry musicMenuEntry = new MenuEntry(MusicText());
            musicMenuEntry.Selected += MusicMenuEntrySelected;
            MenuEntries.Add(musicMenuEntry);

            MenuEntry fullScreenMenuEntry = new MenuEntry(FullScreenText());
            fullScreenMenuEntry.Selected += FullScreenMenuEntrySelected;
            MenuEntries.Add(fullScreenMenuEntry);

            MenuEntry mouseMotionMenuEntry = new MenuEntry(MouseMotionText());
            mouseMotionMenuEntry.Selected += MouseMotionMenuEntrySelected;
            MenuEntries.Add(mouseMotionMenuEntry);

            MenuEntry creditsMenuEntry = new MenuEntry("CREDITS");
            creditsMenuEntry.Selected += CreditsMenuEntrySelected;
            MenuEntries.Add(creditsMenuEntry);

            MenuEntry exitMenuEntry = new MenuEntry("BACK");
            exitMenuEntry.Selected += OnCancel;
            MenuEntries.Add(exitMenuEntry);
        }


        public override void DrawExtraThings()
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            int yOff = (int)(TransitionPosition * 160);
            spriteBatch.DrawString(ScreenManager.Font, "SPACE OCTOPUS MONO       VERSION 1.4", new Vector2(10 + ScreenManager.screenX, GameWindow.Height - 80 + yOff + ScreenManager.screenY), Color.White, 0,
                                    new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(ScreenManager.Font, "SUPPORT: WWW.NEWNORTHROAD.COM", new Vector2(10 + ScreenManager.screenX, GameWindow.Height - 40 + yOff + ScreenManager.screenY), Color.White, 0,
                    new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
        }

        private static string MusicText()
        {
            return "MUSIC " + (Options.Instance.EnableMusic ? "ON" : "OFF");
        }

        private static string FullScreenText()
        {
            return "FULL SCREEN " + (Options.Instance.FullScreen ? "ON" : "OFF");
        }

        private static string MouseMotionText()
        {
            return ("CONTROLS: " + (Options.Instance.RabbleControls ? "WEIRD" : "KEYBOARD"));
        }

        void CreditsMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new CreditsScreen());
        }

        void MusicMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.EnableMusic = !Options.Instance.EnableMusic;
            ((MenuEntry)sender).Text = MusicText();
        }

        void FullScreenMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.FullScreen = !Options.Instance.FullScreen;
            ((MenuEntry)sender).Text = FullScreenText();
        }

        void MouseMotionMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.RabbleControls = !Options.Instance.RabbleControls;
            ((MenuEntry)sender).Text = MouseMotionText();
        }

    }
}
