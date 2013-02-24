using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;
using SpaceOctopus.Projectiles;

namespace SpaceOctopus.Creatures
{
    public abstract class Enemy : Creature
    {
        //EnemySet Set;
        public SoundEffect DieSound;
        public int Points;

        public Enemy(Sprite texture, int defaultROF, float speed)
            : base(texture)
        {
            Debug.Assert(texture != null);
            Points = 10;
            DieSound = Snd.AlienDieSound;
            Position.X = -1;
            Position.Y = -1;
            r = 255;
            g = 255;
            b = 255;
            IsAlive = true;
            ROF = defaultROF;
            RefireTimer = Core.Instance.Random.Next(0, ROF); //random start so we don't fire in unison.
            Speed = speed;
        }

        public virtual void Shoot()
        {
            //override me
        }

        //You must call doCollisions and moveGun in here.
        public abstract override void Move(int delta);

        public virtual void MoveGun(int delta)
        {
            RefireTimer -= delta;
            if (RefireTimer < 0)
            {
                if (isFullyOnScreen()) Shoot();
                RefireTimer = ROF;
            }
        }

        //call in the move method.
        //Checks for collisions with shots
        //Will cause an explosion, increase score and dead the alien.
        public void DoCollisions()
        {
            if (Position.Y + Height < 0) return; //we're off the top of the screen, therefore invincible.
            Core core = Core.Instance;
            foreach (Shot s in core.shotArray)
            {
                //the next line checks out OWN IsAlive bool. That's because a previous shot may have killed us - we don't want to interact with any further shots if we're dead
                if (IsAlive && s != null)
                {
                    if (s.IsAlive && s.HurtsEnemy > 0 && testCollision(s))
                    {
                        if (s.Owner == 0)
                        {
                            core.P.Kills++;
                            core.P.Score += Points;
                        }
                        else if (s.Owner == 1)
                        {
                            core.P2.Kills++;
                            core.P2.Score += Points;
                        }
                        Die(s.expType);
                        s.Die();
                    }
                }
            }
        }

        public void DieWithNoEffects()
        {
            IsAlive = false;
        }

        public void Die(ParticleType expType)
        {
            IsAlive = false;
            Core.Instance.TrySpawnPowerUp(this);
            Explode(expType);
        }

        public virtual void Explode(ParticleType expType)
        {
            DieSound.Play();
            MakeIntoParticles(expType);
        }
    }
}
