using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienGameSample;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceOctopus
{
    class MainMenuScreen : MenuScreen
    {
        public MainMenuScreen()  : base("Main") {
            CreateMenus();
         }

        public void CreateMenus()
        {
            MenuEntries.Clear();
            bool withResume = Core.IsThereAGameToContinue();
            // Create our menu entries.
            MenuEntry startGameMenuEntry = new MenuEntry("CONTINUE");
            MenuEntry onePlayerMenuEntry = new MenuEntry("1 PLAYER");
            MenuEntry twoPlayerMenuEntry = new MenuEntry("2 PLAYER");
            MenuEntry optionsMenuEntry = new MenuEntry("OPTIONS");

            // Hook up menu event handlers.
            startGameMenuEntry.Selected += StartGameMenuEntrySelected;
            onePlayerMenuEntry.Selected += StartOnePlayerGame;
            twoPlayerMenuEntry.Selected += StartTwoPlayerGame;
            optionsMenuEntry.Selected += OptionsMenuEntrySelected;

            // Add entries to the menu.
            if (withResume) MenuEntries.Add(startGameMenuEntry);

            MenuEntries.Add(onePlayerMenuEntry);
            MenuEntries.Add(twoPlayerMenuEntry);

            if (Version.IsTrialMode)
            {
                MenuEntry buyNowMenuEntry = new MenuEntry("GET FULL VERSION");
                buyNowMenuEntry.Selected += BuyNowMenuEntrySelected;
                MenuEntries.Add(buyNowMenuEntry);
            }

            MenuEntries.Add(optionsMenuEntry);
            //MenuEntries.Add(new MenuEntry("1 PLAYER + AI"));

#if WINDOWS
            MenuEntry exitMenuEntry = new MenuEntry("QUIT");
            exitMenuEntry.Selected += OnCancel;
            MenuEntries.Add(exitMenuEntry);
#endif
        }

        void StartGameMenuEntrySelected(object sender, EventArgs e)
        {
            if (!Core.IsThereARunningGameToContinue())
            {
                Core.Instance.nextGameType = GameType.ContinueSavedGame;
                Core.Instance.Reset();
            } //otherwise, let the current instance carry on.
            CreateGameplayScreen();
        }

        void StartOnePlayerGame(object sender, EventArgs e)
        {
            TopUpTrial();
            Core.Instance.nextGameType = GameType.OnePlayer;
            Core.Instance.Reset();
            CreateGameplayScreen();
        }

        void StartTwoPlayerGame(object sender, EventArgs e)
        {
            TopUpTrial();
            Core.Instance.nextGameType = GameType.TwoPlayer;
            Core.Instance.Reset();
            CreateGameplayScreen();
        }

        void TopUpTrial()
        {
            if (Version.IsTrialMode)
            {
                TrialLimits.Instance.TopUp();
            }
        }

        void CreateGameplayScreen()
        {
            GameplayScreen screen = new GameplayScreen(this);
            ScreenManager.AddScreen(screen);
        }

        void OptionsMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen());
        }

        protected override void OnCancel()
        {
            ScreenManager.Game.Exit();
        }


        void BuyNowMenuEntrySelected(object sender, EventArgs e)
        {
            Guide.ShowMarketplace(PlayerIndex.One);
        }

    }
}
