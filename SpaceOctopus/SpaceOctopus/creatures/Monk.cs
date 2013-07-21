using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;
using SpaceOctopus.Projectiles;

namespace SpaceOctopus.Creatures
{
    class Monk : Enemy
    {
        float xV;
        float yV;
        float xA;
        float yA;
        static float maxXV = 4f;
        bool left; //has its left foot
        bool right; //has its right foot
        static double friction = 0.99997;
        public int SleepTime;

        static int defaultROF = 4000;
        static float defaultSpeed = 0.0002f;
        //use specImage 2 for explosionmap

        public static SoundEffect DefaultShootSound;

        public Monk()
            : base(Core.Instance.Art.Monk, defaultROF, defaultSpeed)
        {
            this.Art = Core.Instance.Art;
            Direction = 2; //ignored
            xV = 0;
            yV = Speed * 140;
            xA = 0.000001f;
            left = true;
            right = true;
            ShootSound = DefaultShootSound;
        }

        public override void Explode(ParticleType expType)
        {
            if (left) ShootLeft();
            if (right) ShootRight();
            Picture = Art.MonkSpecialPicture[2];
            base.Explode(expType);
        }

        public override void Shoot()
        {
            if (!left && !right) return;
            ShootSound.Play();
            if (left && !right)
            {
                ShootLeft();
                Picture = Art.MonkSpecialPicture[2];
            }
            else if (right && !left)
            {
                ShootRight();
                Picture = Art.MonkSpecialPicture[2];
            }
            else
            {
                if (Core.Instance.Random.Next(0, 2) == 0)
                {
                    ShootLeft();
                    Picture = Art.MonkSpecialPicture[0];
                }
                else
                {
                    ShootRight();
                    Picture = Art.MonkSpecialPicture[1];
                }
            }
        }

        private void ShootLeft()
        {
            Debug.Assert(left);
            left = false;
            Shot s = Shot.CreateMonkShot(Position.X, Position.Y + Height - Art.MonkShot.Height, 1);
            Core.Instance.AddShot(s);
        }

        private void ShootRight()
        {
            Debug.Assert(right);
            right = false;
            Shot s = Shot.CreateMonkShot(Position.X + Width - Art.MonkShot.Width, Position.Y + Height - Art.MonkShot.Height, 1);
            Core.Instance.AddShot(s);
        }

        public override void Move(int delta)
        {
            if (!IsAlive) return;
            if (SleepTime > 0)
            {
                SleepTime -= delta;
            }
            else
            {
                MoveGun(delta);
                float distance = ((GameWindow.Width / 2) - (Position.X + Width / 2)) / GameWindow.Width;
                xA = Math.Sign(distance) * Speed;
                Position.X += xV * delta;
                Position.Y += yV * delta;

                xV *= (float)(Math.Pow(friction, delta));
                yV *= (float)(Math.Pow(friction, delta));
                xV += xA * delta;

                if (xV > maxXV) xV = maxXV;
                if (xV < -maxXV) xV = -maxXV;
                if (isOnScreen()) yV += yA * delta; //accelerate downwards, once on screen.
            }
            DoCollisions();
        }
    }
}
