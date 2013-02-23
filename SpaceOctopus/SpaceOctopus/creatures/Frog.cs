﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceOctopus.creatures
{
    class Frog : Enemy
    {
        float xV;
        float yV;
        float friction = 0.997f;
        int stillTime;
        static int defaultMaxStillTime = 30;
        public int MaxStillTime;
        static int maxJumpDist = Window.Width;
        static int defaultROF = 6000;
        static float defaultSpeed = 0.1f;
        int hops = 0;
        private int waveDelay;
        private int fastHopThreshhold = 3;

        public Frog(int waveDelay)
            : base(Core.Instance.Art.Frog, defaultROF, defaultSpeed)
        {
            this.waveDelay = waveDelay;
            Art = Core.Instance.Art;
            //shootSound = n/a frogs don't shoot
            //sound die
            Direction = 2; //ignored
            MaxStillTime = defaultMaxStillTime;
            stillTime = Core.Instance.Random.Next(0, MaxStillTime);
            //create explosion map
        }

        public override void Shoot()
        {
            //base.Shoot();
        }

        public override void Move(int delta)
        {
            if (waveDelay > 0)
            {
                waveDelay--; //FIXME: not framerate independent
                return;
            }
            xV *= (float)Math.Pow(friction, delta);
            yV *= (float)Math.Pow(friction, delta);
            Position.X += xV * delta;
            Position.Y += yV * delta;
            if (Math.Abs(xV) < 0.01) xV = 0;
            if (Math.Abs(yV) < 0.01) yV = 0;
            //Local fearDirection:Float = starsAvoidShots()

            if (Math.Abs(xV) < 0.04 && Math.Abs(yV) < 0.04)
            {
                stillTime++;
            }

            if (Math.Abs(yV) > 0.06f)
            {
                Picture = Art.FrogLeap;
            }
            else
            {
                Picture = Art.Frog;
            }

            if (stillTime > MaxStillTime)
            {
                hops++;
                if (hops >= fastHopThreshhold)
                {
                    //fast mode
                    xV = 0;
                    yV = 0.4f;
                    stillTime = 0;
                    friction = 0.996f;
                    MaxStillTime = 9 + Core.Instance.Random.Next(0, 2);
                }
                else
                {
                    //slow mode
                    stillTime = 0;
                    int destX = Core.Instance.Random.Next(0, Window.Width);
                    //make sure destination is in range.
                    destX = (int)Math.Min(destX, Position.X + maxJumpDist / 2);
                    destX = (int)Math.Max(destX, Position.X - maxJumpDist / 2);

                    //calculate Y coordinate using trig, to get a destinationpoint that is maxJumpDist away.
                    //c^2 = a^2 - b^2
                    int c = (int)Math.Sqrt(maxJumpDist * maxJumpDist - Math.Abs(Position.X - (destX * destX)));
                    int destY = (int)(c + Position.Y);
                    xV = (destX - Position.X) * 6 / maxJumpDist / 18;
                    yV = (destY - Position.Y) * 6 / maxJumpDist / 18;
                }




            }
            DoCollisions();
        }

    }
}