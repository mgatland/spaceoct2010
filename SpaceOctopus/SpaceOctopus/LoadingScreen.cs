using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienGameSample;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;


namespace SpaceOctopus
{

    class LoadingScreen : GameScreen
    {
 //       private Thread backgroundThread;

        public LoadingScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.0);
        }

        void BackgroundLoadContent()
        {
            //Load all game content (unless we have some assets loaded per-level)
            Assets.LoadContent(ScreenManager);
        }


        public override void LoadContent()
        {
    //        if (backgroundThread == null)
    //        {
    //            backgroundThread = new Thread(BackgroundLoadContent);
    //            backgroundThread.Start();
   //         }
            BackgroundLoadContent();

            base.LoadContent();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
     //       if (backgroundThread != null && backgroundThread.Join(10))
     //       {
      //          backgroundThread = null;
                this.ExitScreen();
                MainMenuScreen mainMenuScreen = new MainMenuScreen();
                ScreenManager.AddScreen(mainMenuScreen);
                if (Options.Instance.RabbleControls)
                {
                    Core.Instance.nextGameType = GameType.TwoPlayer;
                    Core.Instance.Reset();
                    GameplayScreen screen = new GameplayScreen(mainMenuScreen);
                    ScreenManager.AddScreen(screen);
                }
                ScreenManager.Game.ResetElapsedTime();
       //     }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }


    }

}
