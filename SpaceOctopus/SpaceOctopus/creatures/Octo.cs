using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;

namespace SpaceOctopus.Creatures
{
    public class Octo : Enemy
    {
        public int Row; //our current row up the screen.       
        public bool isFiring; //was bFire

        public static int DefaultROF = 6000;
        public static float DefaultSpeed = (Window.Width / 6000f); //0.05f
        public static SoundEffect DefaultShootSound;

        private static int DistanceAtEdge = 5;
        public static int RowHeight;

        public Octo()
            : base(Core.Instance.Art.Octo, DefaultROF, DefaultSpeed)
        {
            Debug.Assert(RowHeight > 0);
            Art = Core.Instance.Art;
            ShootSound = DefaultShootSound;
            Row = 0;
            Direction = 1;
        }

        public override void Shoot()
        {
            Beam beam = Core.Instance.GetFreeBeam();
            if (beam != null)
            {
                ShootSound.Play();
                beam.Fire(centerX(), centerY(), this);
                isFiring = true;

            }
        }

        public override void Move(int delta)
        {
            if (!IsAlive) return;
            MoveGun(delta);
            if (!isFiring)
            {
                if (Direction == 2)
                {
                    Position.Y += Speed * delta;
                }
                else
                {
                    Position.X += Direction * Speed * delta; //works because the Direction values for right and left are 1 and -1.
                }

                //change direction if neccessary
                if (Direction == -1)
                { //LEFT
                    if (Position.X <= DistanceAtEdge)
                    {
                        Direction = 2; //DOWN
                        //redirect excess movement
                        Position.Y += (DistanceAtEdge - Position.X);
                        Position.X = DistanceAtEdge;
                    }

                }
                else if (Direction == 1)
                { //Right
                    if (Position.X > Window.Width - DistanceAtEdge - Width)
                    {
                        Direction = 2;
                        //redirect excess movement
                        Position.Y += (Position.X - (Window.Width - DistanceAtEdge - Width));
                        Position.X = Window.Width - DistanceAtEdge - Width;
                    }
                }
                else if (Direction == 2)
                { //Downwards
                    //next row yet?
                    if (Position.Y >= (Row + 1) * RowHeight)
                    {
                        Row++;
                        //Left, or right?
                        if (Position.X < Window.Width / 2)
                        {
                            Direction = 1;
                        }
                        else
                        {
                            Direction = -1;
                        }
                        //Redirect excess movement
                        Position.X += Direction * (Position.Y - Row * RowHeight);
                        Position.Y = Row * RowHeight;
                    }
                }
            }
            else
            { //alien is firing.
                if ((RefireTimer < ROF - 1700) || RefireTimer <= 0)
                { //Don't know what that first expression is for...
                    isFiring = false;
                }
            }
            DoCollisions();
        }

    }
}
