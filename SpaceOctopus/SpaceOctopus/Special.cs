using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SpaceOctopus
{

    public class Special
    {
        int duration;
        int time;
        int spawnPeriod;
        int spawned;
        int spawnStart;
        int scoreIncPeriod = 66;
        int lastScoreInc = 0;
        Core core;
        int stage;
        float speedMulti;

        public int Creatures;

        private List<String> terrain;

        enum SpecialType
        {
            WALLS,
            TWO_PATHS,
            AVOID
        }

        public Special(int level)
        {
            terrain = new List<String>();
            stage = level; //do'h
            duration = 15000;
            spawnStart = 200;

            SpecialType specialType;
            switch (stage % 3)
            {
                case 0: specialType = SpecialType.AVOID; break;
                case 1: specialType = SpecialType.WALLS; break;
                case 2: specialType = SpecialType.TWO_PATHS; break;
                default: throw new System.InvalidOperationException("unexpected special level type");
            }

            int difficulty = stage / 3; //divide by the number of SpecialTypes

            Debug.Print("level:" + level);
            Debug.Print("variation:" + specialType);
            Debug.Print("difficulty:" + difficulty);

            if (specialType == SpecialType.TWO_PATHS)
            {
                terrain.Add("x   v     ");
                terrain.Add("   xv     ");
                terrain.Add("    v   x ");
                terrain.Add("    v  x  ");
                terrain.Add("   xv     ");
                terrain.Add("x   v     ");
                terrain.Add("  x v     ");
                terrain.Add("    v     ");
                terrain.Add("    v  x  ");
                terrain.Add("    v    x");
                terrain.Add("    v  x  ");
                terrain.Add("    vx    ");
                terrain.Add("    v     ");
                terrain.Add("  x v     ");
                terrain.Add(" x  v     ");

                speedMulti = 0.75f; //slow
                spawnPeriod = 400;
                if (difficulty == 0) spawnPeriod = (int)(spawnPeriod * 1.3f); //super slow the first time
                if (difficulty % 2 == 1) //every second time
                {
                    spawnPeriod = (int)(spawnPeriod * 1.2f); //make it slower, and
                    Creatures = 1; //add monks
                }
            }
            else if (specialType == SpecialType.WALLS)
            {
                //this comes at a slow pace, with plenty of time between the rows
                terrain.Add("xxxx  xxxx");
                terrain.Add("xx  xxxxxx");
                terrain.Add("xxxxxx  xx");
                terrain.Add("xxxxxxxx  ");
                terrain.Add("xxxx  xxxx");
                terrain.Add("xxxxxxxx  ");
                terrain.Add("  xxxxxxxx");
                terrain.Add("xxx  xxxxx");
                terrain.Add("xxxx  xxxx");
                terrain.Add("x  xxxxxxx");
                terrain.Add("xxxxxxx  x");
                terrain.Add("x  xxxxxxx");

                speedMulti = 0.53f;
                spawnPeriod = (int)(1250 * 1f / 0.7f);
                if (difficulty > 0)
                {
                    Creatures = 1; //monk wave, every time after the first :o
                }
            }
            else
            {
                terrain.Add("x         ");
                terrain.Add("   x      ");
                terrain.Add("      x   ");
                terrain.Add("    x     ");
                terrain.Add("   x      ");
                terrain.Add("x         ");
                terrain.Add("  x       ");
                terrain.Add("    x     ");
                terrain.Add("       x  ");
                terrain.Add("         x");
                terrain.Add("       x  ");
                terrain.Add("     x    ");
                terrain.Add("    x     ");
                terrain.Add("  x       ");
                terrain.Add(" x        ");

                //This one gets faster and faster every time.
                speedMulti = (float)Math.Min(0.5 + difficulty * .2, 1.1);
                spawnPeriod = Math.Max(530 - difficulty * 50, 290);
                if (difficulty % 2 == 1)
                {
                    //monks every second time.
                    speedMulti *= 0.7f;
                    Creatures = 1;
                }
            }
            spawned = 0;
            time = 0;
            core = Core.Instance;
        }

        public bool Finished()
        {
            if (time > duration)
            {
                Debug.WriteLine("Special stage finished");
                return true;
            }
            return false;
        }

        public void Move(int delta)
        {
            time += delta;
            if (time > spawnStart + spawned * spawnPeriod)
            {
                spawned++;
                if (terrain.Count() > 0)
                {
                    int row = (spawned - 1) % terrain.Count();
                    String data = terrain[row];
                    int bulletWidth = Core.Instance.Art.ShotHeartPicture.Width;
                    int spaceBetweenBullets = 12;
                    int x = bulletWidth / 2 + spaceBetweenBullets;
                    foreach (char c in data.ToCharArray())
                    {
                        if (c == 'x')
                        {
                            Shot s = Shot.CreateHeart(x, -Core.Instance.Art.ShotHeartPicture.Height, speedMulti);
                            core.AddShot(s);
                        }
                        if (c == 'v')
                        {
                            Shot s = Shot.CreateHeartBig(x, -Core.Instance.Art.ShotHeartBigPicture.Height, speedMulti);
                            core.AddShot(s);
                        }
                        x += bulletWidth + spaceBetweenBullets;
                    }
                }
            }
            while (time > spawnStart + lastScoreInc + scoreIncPeriod)
            {
                lastScoreInc += scoreIncPeriod;
                if (core.P.IsAlive) core.P.Score++;
                if (core.P2 != null && core.P2.IsAlive) core.P2.Score++;
            }
        }

    }

}
