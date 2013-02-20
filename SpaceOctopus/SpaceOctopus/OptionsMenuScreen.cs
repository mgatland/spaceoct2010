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
        MenuEntry soundMenuEntry;
        MenuEntry musicMenuEntry;
        MenuEntry motionMenuEntry;
        MenuEntry AccelSmoothingMenuEntry;
        MenuEntry RecalibrateMenuEntry;

        public OptionsMenuScreen()
            : base("options") 
        {
         //   soundMenuEntry = new MenuEntry(SoundText());
         //   soundMenuEntry.Selected += SoundMenuEntrySelected;
         //   MenuEntries.Add(soundMenuEntry);

            musicMenuEntry = new MenuEntry(MusicText());
            musicMenuEntry.Selected += MusicMenuEntrySelected;
            MenuEntries.Add(musicMenuEntry);

//            motionMenuEntry = new MenuEntry(MotionText());
//            motionMenuEntry.Selected += MotionMenuEntrySelected;
//            MenuEntries.Add(motionMenuEntry);

           /*Accelerometer disabled
            AccelSmoothingMenuEntry = new MenuEntry(AccelSmoothingMenuEntryText());
            AccelSmoothingMenuEntry.Selected += AccelSmoothingMenuEntrySelected;
            MenuEntries.Add(AccelSmoothingMenuEntry);

            RecalibrateMenuEntry = new MenuEntry("Recalibrate accelerometer");
            RecalibrateMenuEntry.Selected += RecalibrateMenuEntrySelected;
            MenuEntries.Add(RecalibrateMenuEntry);*/

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
            spriteBatch.DrawString(ScreenManager.Font, "SPACE OCTOPUS MONO       VERSION 1.4", new Vector2(10, Window.Height - 80 + yOff), Color.White, 0,
                                    new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(ScreenManager.Font, "SUPPORT: WWW.NEWNORTHROAD.COM", new Vector2(10, Window.Height - 40 + yOff), Color.White, 0,
                    new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
        }

        private static string MusicText()
        {
            return "MUSIC " + (Options.Instance.EnableMusic ? "ON" : "OFF");
        }

        private static string SoundText()
        {
            return "SOUND " + (Options.Instance.EnableSound ? "ON" : "OFF");
        }


        private static string AccelSmoothingMenuEntryText()
        {
            return "Accelerometer smoothing: " + (Tweaking.AccelerationSmoothing);
        }
        

        private static string MotionText()
        {
            return (Options.Instance.VerticalMotion ? "4 WAY MOVEMENT" : "CLASSIC MOVEMENT");
        }

        void CreditsMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new CreditsScreen());
        }

        void SoundMenuEntrySelected(object sender, EventArgs e)
        {
            Tweaking.EnableSound = !Tweaking.EnableSound;
            soundMenuEntry.Text = SoundText();
        }

        void AccelSmoothingMenuEntrySelected(object sender, EventArgs e)
        {
            Tweaking.AccelerationSmoothing++;
            if (Tweaking.AccelerationSmoothing > Tweaking.MaxAccelerationSmoothing)
            {
                Tweaking.AccelerationSmoothing = 1;
            }
            AccelSmoothingMenuEntry.Text = AccelSmoothingMenuEntryText();
        }

        void MusicMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.EnableMusic = !Options.Instance.EnableMusic;
            musicMenuEntry.Text = MusicText();
        }

        void MotionMenuEntrySelected(object sender, EventArgs e)
        {
            Options.Instance.VerticalMotion = !Options.Instance.VerticalMotion;
            motionMenuEntry.Text = MotionText();
        }

        void RecalibrateMenuEntrySelected(object sender, EventArgs e)
        {
            Tweaking.RecalibrateAccel = true;
        }

    }
}
