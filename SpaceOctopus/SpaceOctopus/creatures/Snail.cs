using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;

namespace SpaceOctopus.Creatures
{
    public class Snail : Enemy
    {
        public bool isFiring; //was bFire

        private static int DelayAfterFiring = 10;
        public static float DefaultSpeed = 0.04f;
        public static SoundEffect DefaultShootSound;
        private int lastShellX;
        private int layingTimer;
        int startingDelay;
        bool initialized = false;

        public Snail()
            : base(Core.Instance.Art.SnailRight, 0, DefaultSpeed)
        {
            Art = Core.Instance.Art;
            ShootSound = DefaultShootSound;
            Direction = 0;
            lastShellX = 0;
            layingTimer = 0;
        }

        public override void Shoot()
        {
            ShootSound.Play();
            //beam.Fire(centerX(), centerY(), this);
            Shell shell = new Shell();
            shell.Position.Y = Position.Y;
            shell.Position.X = Position.X - (Picture.Width * Direction);
            Core.Instance.AlienList.Add(shell);
            lastShellX = (int)Position.X;
            layingTimer = DelayAfterFiring;
        }

        public override void Move(int delta)
        {
            if (!initialized)
            {
                //initialize direction
                if (Position.X > 0)
                {
                    Picture = Core.Instance.Art.SnailLeft;
                    Direction = -1;
                    lastShellX = GameWindow.Width - Picture.Width;
                }
                else
                {
                    Direction = 1;
                }
                initialized = true;
            }

            if (startingDelay > 0)
            {
                startingDelay--;
                return;
            }
            if (!IsAlive) return;

            if (layingTimer == 0)
            {
                Position.X += Direction * Speed * delta; //works because the Direction values for right and left are 1 and -1.
                bool hasEnoughRoomToLayShell = false;
                if (Direction == 1 && (Position.X > lastShellX + Picture.Width)) {
                    hasEnoughRoomToLayShell = true;
                }
                else if (Direction == -1 && (Position.X < lastShellX - Picture.Width))
                {
                    hasEnoughRoomToLayShell = true;
                }
                if (hasEnoughRoomToLayShell)
                {
                    Shoot();
                }
            }
            else
            {
                layingTimer--;
            }

            if (isWellOffScreen() && ((Direction == 1 && Position.X > 0) || (Direction == -1 && Position.X < 0)))
            {
                Direction = -1 * Direction;
                Position.Y += Picture.Height;
                if (Direction == 1)
                {
                    Picture = Core.Instance.Art.SnailRight;
                    lastShellX = 0;
                }
                else
                {
                    Picture = Core.Instance.Art.SnailLeft;
                    lastShellX = GameWindow.Width - Picture.Width;
                }
            }

            DoCollisions();
        }

        private bool isWellOffScreen()
        {
            if (Position.X < -Picture.Width - Picture.Width / 2)
            {
                return true;
            }
            if (Position.X > GameWindow.Width + Picture.Width / 2)
            {
                return true;
            }
            return false;
        }

    }
}
