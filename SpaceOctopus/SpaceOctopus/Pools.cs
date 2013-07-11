using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SpaceOctopus.Projectiles;

namespace SpaceOctopus
{
    //from http://blog.gallusgames.com/programming/lean-mean-object-pool-in-c

    //Pool used to be a generic class Pool<T>.
    //This was confusing JSIL so I've removed the generics.
    //Maybe there's a less extreme solution...

    public class ShotPool
    {
        private Shot[] pool;
        private int nextItem = -1;

        private bool hasPoolExausted; //limits debug messages.
        private bool hasPoolOverfilled; //limits debug messages.

        public ShotPool(int capacity)
        {
            pool = new Shot[capacity];
            for (int i = 0; i < capacity; i++)
            {
                pool[i] = new Shot();
            }
        }

        public override string ToString()
        {
            return base.ToString() + "( nextItem " + nextItem + ", capacity " + pool.Length + ")";
        }

        public Shot Fetch()
        {
            if (nextItem == pool.Length - 1)
            {
                Shot newT = new Shot();
                if (!hasPoolExausted)
                {
                    Debug.WriteLine("Pool exausted, creating an instance of " + newT.GetType().Name);
                    hasPoolExausted = true;
                }
                return newT;
            }
            return pool[++nextItem];
        }

        public void Release(Shot instance)
        {
            if (nextItem < 0)
            {
                if (!hasPoolOverfilled)
                {
                    Debug.WriteLine("Too many objects returned to pool.");
                    hasPoolOverfilled = true;
                }
                return;
            }
            pool[nextItem--] = instance;
        }
    }

    public class ParticleGroupPool
    {
        private ParticleGroup[] pool;
        private int nextItem = -1;

        private bool hasPoolExausted; //limits debug messages.
        private bool hasPoolOverfilled; //limits debug messages.

        public ParticleGroupPool(int capacity)
        {
            pool = new ParticleGroup[capacity];
            for (int i = 0; i < capacity; i++)
            {
                pool[i] = new ParticleGroup();
            }
        }

        public override string ToString()
        {
            return base.ToString() + "( nextItem " + nextItem + ", capacity " + pool.Length + ")";
        }

        public ParticleGroup Fetch()
        {
            if (nextItem == pool.Length - 1)
            {
                ParticleGroup newT = new ParticleGroup();
                if (!hasPoolExausted)
                {
                    Debug.WriteLine("Pool exausted, creating an instance of " + newT.GetType().Name);
                    hasPoolExausted = true;
                }
                return newT;
            }
            return pool[++nextItem];
        }

        public void Release(ParticleGroup instance)
        {
            if (nextItem < 0)
            {
                if (!hasPoolOverfilled)
                {
                    Debug.WriteLine("Too many objects returned to pool.");
                    hasPoolOverfilled = true;
                }
                return;
            }
            pool[nextItem--] = instance;
        }
    }


    public class ParticlePool
    {
        private Particle[] pool;
        private int nextItem = -1;

        private bool hasPoolExausted; //limits debug messages.
        private bool hasPoolOverfilled; //limits debug messages.

        public ParticlePool(int capacity)
        {
            pool = new Particle[capacity];
            for (int i = 0; i < capacity; i++)
            {
                pool[i] = new Particle();
            }
        }

        public override string ToString()
        {
            return base.ToString() + "( nextItem " + nextItem + ", capacity " + pool.Length + ")";
        }

        public Particle Fetch()
        {
            if (nextItem == pool.Length - 1)
            {
                Particle newT = new Particle();
                if (!hasPoolExausted)
                {
                    Debug.WriteLine("Pool exausted, creating an instance of " + newT.GetType().Name);
                    hasPoolExausted = true;
                }
                return newT;
            }
            return pool[++nextItem];
        }

        public void Release(Particle instance)
        {
            if (nextItem < 0)
            {
                if (!hasPoolOverfilled)
                {
                    Debug.WriteLine("Too many objects returned to pool.");
                    hasPoolOverfilled = true;
                }
                return;
            }
            pool[nextItem--] = instance;
        }
    }
}
