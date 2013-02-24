using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace SpaceOctopus.Creatures
{
    public class Creature : Drawable
    {
        public float Speed;
        public int Direction; //-1 is lrft, 1 is right, 2 is down\special
        public int RefireTimer;
        public int ROF; //Rate Of Fire
        public SoundEffect ShootSound;
        public ArtSet Art;
        public Creature(Sprite texture)
            : base(texture)
        {
        }

    }
}
