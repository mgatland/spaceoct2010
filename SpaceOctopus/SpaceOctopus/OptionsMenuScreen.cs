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
    //    MenuEntry soundMenuEntry;
        MenuEntry musicMenuEntry;
   //     MenuEntry motionMenuEntry;
        MenuEntry fullScreenMenuEntry;

        public OptionsMenuScreen()
            : base("options") 
        {
         //   soundMenuEntry = new MenuEntry(SoundText());
         //   soundMenuEntry.Selected += SoundMenuEntrySelected;
         //   MenuEntries.Add(soundMenuEntry);

            musicMenuEntry = new MenuEntry(MusicText());
            musicMenuEntry.Selected += MusicMenuEntrySelected;
            MenuEntries.Add(musicMenuEntry);

            fullScreenMenuEntry = new MenuEntry(FullScreenText());
            fullScreenMenuEntry.Selected += FullScreenMenuEntrySelected;
            MenuEntries.Add(fullScreenMenuEntry);

//            motionMenuEntry = new MenuEntry(MotionText());
//            motionMenuEntry.Selected += MotionMenuEntrySelected;
//            MenuEntries.Add(motionMenuEntry);

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
            spriteBatch.DrawString(ScreenManager.Font, "SPACE OCTOPUS MONO       VERSION 1.4", new Vector2(10 + ScreenManager.screenX, Window.Height - 80 + yOff + ScreenManager.screenY), Color.White, 0,
                                    new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(ScreenManager.Font, "SUPPORT: WWW.NEWNORTHROAD.COM", new Vector2(10 + ScreenManager.screenX, Window.Height - 40 + yOff + ScreenManager.screenY), Color.White, 0,
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

        /*private static string SoundText()
        {
            return "SOUND " + (Options.Instance.EnableSound ? "ON" : "OFF");
        }

        private static string MotionText()
        {
            return (Options.Instance.VerticalMotion ? "4 WAY MOVEMENT" : "CLASSIC MOVEMENT");
        }*/

        void CreditsMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new CreditsScreen());
        }

     /*   void SoundMenuEntrySelected(object sender, EventArgs e)
        {
            Tweaking.EnableSound = !Tweaking.EnableSound;
            soundMenuEntry.Text = SoundText();
        }*/

        void MusicMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.EnableMusic = !Options.Instance.EnableMusic;
            musicMenuEntry.Text = MusicText();
        }

        void FullScreenMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.FullScreen = !Options.Instance.FullScreen;
            fullScreenMenuEntry.Text = FullScreenText();
        }

    /*    void MotionMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.VerticalMotion = !Options.Instance.VerticalMotion;
            motionMenuEntry.Text = MotionText();
        }*/

    }
}
