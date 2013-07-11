using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace SpaceOctopus.Projectiles
{
    public class Shot : Drawable
    {
        #region pooling
        public const int PoolSize = 50;
        private static ShotPool Pool;

        public static Shot Create(Sprite texture)
        {
            Debug.Assert(Pool != null, "Pool not initialized");
            Shot s = Pool.Fetch();
            s.Initialize(texture);
            s.XSpeed = 0f;
            s.YSpeed = 0f;
            return s;
        }

        public static void Release(Shot s)
        {
            Debug.Assert(Pool != null, "Pool not initialized");
            //TODO: check that the shot has not already been released. That could get weird.
            Pool.Release(s);
        }

        public static void CreatePool()
        {
            if (Pool != null) return;
            Pool = new ShotPool(PoolSize);
        }
        #endregion

        public float YSpeed;
        float XSpeed;
        public int HurtsEnemy = 1;
        public int HurtsPlayer = 0;
        public int Pierce;
        public int Owner; //TODO: should be a Player object.
        public ParticleType expType;

        protected const float StandardShotSpeed = Window.Height / 1670f;

        public Shot()
            : base(null)
        {
            IsAlive = false; //You must Initialize a shot to make it alive.
        }

        public virtual void Initialize(Sprite texture)
        {
            IsAlive = true;
            Picture = texture;
            if (texture != null)
            {
                Width = texture.Width;
                Height = texture.Height;
            }
            SetColor(Color.White);
        }

        public void move(int delta)
        {
            Position.Y += YSpeed * delta;
            Position.X += XSpeed * delta;
            if (Position.Y + Height < 0 && YSpeed < 0) IsAlive = false;
            if (Position.Y > Window.Height && YSpeed > 0) IsAlive = false;
            if (Position.X + Width < 0 && XSpeed <= 0) IsAlive = false;
            if (Position.X > Window.Width && XSpeed >= 0) IsAlive = false;
        }

        public void Die()
        {
            if (Pierce == 0)
            {
                IsAlive = false;
            }
            else
            {
                Pierce--;
            }
        }

        internal static Shot CreateHeart(int x, int y, float speedMulti)
        {
            Shot s = Shot.Create(Core.Instance.Art.ShotHeartPicture);
            s.YSpeed = StandardShotSpeed;
            s.HurtsEnemy = 0;
            s.HurtsPlayer = 80;
            s.Pierce = 0;
            s.Owner = -1;
            s.expType = ParticleType.FAST_SCATTER;
            s.Position.X = s.offCenterX(x);
            s.Position.Y = y;
            s.YSpeed *= speedMulti;
            return s;
        }

        internal static Shot CreateHeartBig(int x, int y, float speedMulti)
        {
            Shot s = Shot.Create(Core.Instance.Art.ShotHeartBigPicture); //difference from CreateHeart
            s.YSpeed = StandardShotSpeed;
            s.HurtsEnemy = 0;
            s.HurtsPlayer = 50;
            s.Pierce = 1; //difference from CreateHeart
            s.Owner = -1;
            s.expType = ParticleType.FAST_SCATTER;
            s.Position.X = s.offCenterX(x);
            s.Position.Y = y;
            s.YSpeed *= speedMulti;
            return s;
        }

        internal static Shot CreateMonkShot(float x, float y, float speedMulti)
        {
            Shot s = Shot.Create(Core.Instance.Art.MonkShot);
            s.YSpeed = StandardShotSpeed;
            s.HurtsEnemy = 0;
            s.HurtsPlayer = 80;
            s.Pierce = 0;
            s.Owner = -1;
            s.expType = ParticleType.FAST_SCATTER;
            s.Position.X = x; //not off centered!
            s.Position.Y = y;
            s.YSpeed *= speedMulti;
            return s;
        }

        internal static Shot CreateFrogShot(float x, float y, int xDirection)
        {
            Shot s = Shot.Create(xDirection > 0 ? Core.Instance.Art.FrogShotRight : Core.Instance.Art.FrogShotLeft);
            s.YSpeed = 0f;
            s.XSpeed = xDirection * 0.25f;
            s.HurtsEnemy = 0;
            s.HurtsPlayer = 40;
            s.Pierce = 0;
            s.Owner = -1;
            s.expType = ParticleType.FAST_SCATTER;
            s.Position.X = s.offCenterX((int)x);
            s.Position.Y = s.offCenterY((int)y);
            return s;
        }

        public enum ShotType { Normal, Mega }; //Only used by player shots

        internal static Shot CreatePlayerShot(ShotType type, int inX, int inY, float SpeedMultiplier, int playerId)
        {
            Shot s = Shot.Create(null); //we set the texture, height and width manually so we can pass null here.
            if (type == ShotType.Normal)
            {
                s.Picture = playerId == 0 ? Core.Instance.Art.PlayerShotBase1 : Core.Instance.Art.PlayerShotBase2;
                s.Pierce = 0;
                s.expType = ParticleType.FAST_SCATTER;
                s.YSpeed = -StandardShotSpeed;
            }
            else
            {
                s.YSpeed = -StandardShotSpeed * .83f;
                s.expType = ParticleType.FAST_SCATTER;
                s.Picture = Core.Instance.Art.PlayerShotMega;
                s.Pierce = 10;
            }
            s.Width = s.Picture.Width;
            s.Height = s.Picture.Height;
            s.HurtsEnemy = 1;
            s.HurtsPlayer = 0;
            s.Owner = playerId;
            s.Position.X = s.offCenterX(inX);
            s.Position.Y = inY;
            s.YSpeed *= SpeedMultiplier;
            s.Owner = playerId;

            s.r = 255;
            s.g = 255;
            s.b = 255;
            return s;
        }
    }

}
