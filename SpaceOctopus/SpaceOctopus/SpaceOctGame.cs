using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using AlienGameSample;
using System.Diagnostics;
using SpaceOctopus.Data;


namespace SpaceOctopus
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpaceOctGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        ScreenManager screenManager;

        public SpaceOctGame()
        {
            graphics = new GraphicsDeviceManager(this);
#if WINDOWS_PHONE
            graphics.IsFullScreen = true;
#endif
#if WINDOWS
            //this.IsMouseVisible = true;
#endif
            //Set the Windows Phone screen resolution
            graphics.PreferredBackBufferWidth = 480;
            graphics.PreferredBackBufferHeight = 800;

            Content.RootDirectory = "Content";

            #if DEBUG
                if (Tweaking.SimulateTrial) Guide.SimulateTrialMode = true;
            #endif

#if PROFILING
            Components.Add(new ProfilerComponent(this));
#endif

            Version.UpdateFullVersionStatus(); //Warning: Expensive!

            // Frame rate is 30 fps by default for Windows Phone.
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 30.0);

            //Create a new instance of the Screen Manager
            screenManager = new ScreenManager(this);
            Components.Add(screenManager);

            //Add two new screens
            screenManager.AddScreen(new BackgroundScreen());
            screenManager.AddScreen(new LoadingScreen());

            //Collect first-run usage stats
            bool wasFirstRun = false;
            if (Version.IsTrialMode)
            {
                wasFirstRun = new TrackedEvent("firstrun-trial", "", true).Fire();
            } else {
                wasFirstRun = new TrackedEvent("firstrun-full", "", true).Fire();
            }
            /*if (!wasFirstRun)
            {
                if (Version.IsTrialMode)
                {
                    new TrackedEvent("run-trial", "", false).Fire();
                }
                else
                {
                    new TrackedEvent("run-full", "", false).Fire();
                }
            }*/
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Debug.WriteLine("Exiting...");
            base.OnExiting(sender, args);
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            Debug.WriteLine("Activated...");
            base.OnActivated(sender, args);
            /*if (PhoneApplicationService.Current.State.ContainsKey("MenuState"))

  2: {

  3:   ScreenStateList = PhoneApplicationService.Current.State["MenuState"] as List<String>;

  4:   foreach (String screenstate in ScreenStateList)
*/
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            Debug.WriteLine("Deactivated...");
            base.OnDeactivated(sender, args);
            //PhoneApplicationService.Current.State.Add("MenuState", ScreenStateList);
            //what would i save?
            //each player
            //each enemy
            //the level etc info
            //each projectile
            //probably not the particles (although maybe, we have 10 whole seconds.)
        }
    }
}
