using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienGameSample;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.GamerServices;
using System.Xml.Linq;
using SpaceOctopus.Creatures;
using SpaceOctopus.Projectiles;

namespace SpaceOctopus
{

    class GameplayScreen : GameScreen
    {

        //Screen dimension consts
        const float screenScaleFactor = 1.0f;
        const float screenHeight = 800.0f * screenScaleFactor; // Real screen is 800.0f x 480.0f
        const float screenWidth = 480.0f * screenScaleFactor;
        const int leftOffset = 25;
        const int topOffset = 50;
        const int bottomOffset = 20;

        Player player;
        Player player2;

        //arg tight coupling
        MainMenuScreen menuScreen;

        public GameplayScreen(MainMenuScreen menuScreen)
        {
            Debug.Assert(Core.Instance.IsActive);
            this.menuScreen = menuScreen;
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.0);
        }

        #region Input

        /// <summary>
        /// Input helper method provided by GameScreen.  Packages up the various input
        /// values for ease of use.  Here it checks for pausing and handles controlling
        /// the player's tank.
        /// </summary>
        /// <param name="input">The state of the gamepads</param>
        public override void HandleInput(InputState input)
        {

            if (input == null)
                throw new ArgumentNullException("input");

            if (input.PauseGame)
            {
                Debug.WriteLine("Back");
                    finishCurrentGame();
            }
            else
            {
                Core.Instance.Input.HandleInputs(input, player, player2);
                if (Core.Instance.InTrialLimbo)
                {
                    if (Core.Instance.Input.UpsellAction.Equals(UpsellAction.BUY))
                    {
                        Guide.ShowMarketplace(PlayerIndex.One);
                    }
                    else if (Core.Instance.Input.UpsellAction.Equals(UpsellAction.BACK))
                    {
                        finishCurrentGame();
                    }
                }
            }
        }

        #endregion

        private void finishCurrentGame()
        {

            if (menuScreen != null)
            {
                //Refresh the main menu's options
                menuScreen.CreateMenus();
            }
            this.ExitScreen();
        }

        public override void LoadContent()
        {
            if (MediaPlayer.GameHasControl) //unneccessary? might be needed for the IsRepeating
            {
                Debug.Assert(Snd.music != null);
                //Only play if a song is not already playing - i.e. don't play on return from the pause menu.
                if (!(MediaPlayer.State == MediaState.Playing) && Options.Instance.EnableMusic)
                {
                    MediaPlayer.Play(Snd.music);
                    MediaPlayer.IsRepeating = true;
                }
            }

            base.LoadContent();
            Start();
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
        }

        /// <summary>
        /// Starts a new game session, setting all game states to initial values.
        /// </summary>
        void Start()
        {
            Core core = Core.Instance;
            player = core.P;
            player2 = core.P2;
        }

        /// <summary>
        /// Runs one frame of update for the game.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime,
            bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            //bit of a hack - keep us in sync with the game's player
            player = Core.Instance.P;
            player2 = Core.Instance.P2;
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Core.Instance.Update(gameTime);
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }


        /// <summary>
        /// Draw the game world, effects, and HUD
        /// </summary>
        /// <param name="gameTime">The elapsed time since last Draw</param>
        public override void Draw(GameTime gameTime)
        {
            Core.Instance.Draw(gameTime, ScreenManager);
        }

    }

    // Space Octopus (non tutorial) code follows

    public static class GameWindow
    {
        public const int Width = 480; //300 in original SOM
        public const int Height = 800; //500 in original SOM
    }

    public class HighScores : Drawable
    {
        ScoreSet scores;
        float highlightAlpha; //for the highlighted element
        float alphaCycle = -0.001f;
        bool isTwoPlayer;

        public HighScores()
            : base(null)
        {
            r = 255;
            g = 255;
            b = 255;
            a = 255;
            IsAlive = true;
            //if 2 player, get 2 player scores
            if (Core.Instance.P2 == null)
            {
                isTwoPlayer = false;
                scores = new ScoreSet("1playerscores.txt");
            }
            else
            {
                isTwoPlayer = true;
                scores = new ScoreSet("2playerscores.txt");
            }

        }

        public override void Move(int delta)
        {
            base.Move(delta);
            highlightAlpha += alphaCycle * delta;
            if (highlightAlpha < 0f || highlightAlpha > 1.0f)
            {
                alphaCycle = -alphaCycle;
                highlightAlpha = Math.Max(Math.Min(1f, highlightAlpha), 0);
            }
        }

        private void drawString(string text, int x, int y, int xOff, int yOff, ScreenManager screenManager)
        {
            float fontScale = 1.0f;
            Vector2 textPos = new Vector2(x + xOff + screenManager.screenX, y + yOff + screenManager.screenY);
            screenManager.SpriteBatch.DrawString(Message.GameFont, text, textPos, new Color(r, g, b, a), 0, new Vector2(0, Message.GameFont.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
        }

        //Note: ignores xOff and yOff.
        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (!IsAlive) return;

            //ignore offset.
            xOff = 0;
            yOff = 0; 
            a = 255;
            if (isTwoPlayer)
            {
                drawString("Team Scores", 40, 60, xOff, yOff, screenManager);
            }
            else
            {
                drawString("High Scores", 40, 60, xOff, yOff, screenManager);
            }
            

            int yPos = 120;
            foreach (Score s in scores.Scores)
            {
                if (s.isNew)
                {
                    a = (byte)(highlightAlpha * 255f * 0.75f + 255f * 0.25f);
                }
                else
                {
                    a = 255;
                }
                //TODO: fix string creation here, it uses memory every frame.

                if (isTwoPlayer)
                {
                    drawString("Level: " + s.level, 40, yPos, xOff, yOff, screenManager);
                    drawString("Total score: " + (s.score + s.score2), GameWindow.Width / 2- 30, yPos, xOff, yOff, screenManager);
                    //drawString("Player scores: " + s.score + ", " + s.score2, 40, yPos + 40, xOff, yOff, screenManager);
                }
                else
                {
                    drawString("Level: " + s.level, 40, yPos, xOff, yOff, screenManager);
                    drawString("Score: " + s.score, 180, yPos, xOff, yOff, screenManager);
                    drawString("Rank: " + s.rank, 40, yPos + 40, xOff, yOff, screenManager);
                }

                yPos += 100;

            }
        }

        public void AddScoreAndSave(Score s)
        {
            scores.AddScore(s);
            scores.Save();
        }

    }
    public class Message : Drawable
    {
        String text;
        float alpha; //warning: duplicates int a, but gives more precision. Er, different precision.
        float alphaDrain;
        public int Row;
        public static SpriteFont GameFont;
        private float fontScale = 1f;

        public Message(String text, int row)
            : this(text, null, row)
        {
        }

        public Message(String text, Sprite picture, int row)
            : base(picture)
        {
            this.text = text;
            this.Row = row;
            IsAlive = true;
            a = 255;
            alpha = 1f;
            alphaDrain = 0.0002f;

            r = 255;
            g = 255;
            b = 255;

            if (text != null)
            {
                Vector2 size = GameFont.MeasureString(text);
                size *= fontScale;
                Position.Y = GameWindow.Height / 2 + (row * size.Y) - (size.Y / 2);
                Position.X = GameWindow.Width / 2 - (size.X / 2);
            }
        }

        //TODO: Is this ever actually used in single player?
        public Message(String text, int x, int y)
            : this(text, -99) //set row to -99, not a real row.
        {
            Position.X = x;
            Position.Y = y;
        }

        public static Message CreateAtRowWithYOffset(String text, int row, int yPos)
        {
            Message m = new Message(text, row);
            Vector2 size = GameFont.MeasureString(text);
            size *= m.fontScale;
            m.Position.Y = GameWindow.Height / 2 + (row * size.Y) - (size.Y / 2);
            m.Position.X = yPos - (size.X / 2);
            m.Row = -1; //exclude from the row management system.
            return m;
        }


        public static Message CreateImageAt(Sprite pic, int x, int y)
        {
            Message m = new Message(null, pic, -1);
            m.Position.Y = y;
            m.Position.X = x;
            m.Row = -1; //exclude from the row management system.
            return m;
        }

        public override void Move(int delta)
        {
            alpha -= alphaDrain * delta;
            if (alpha < 0) IsAlive = false;
            a = (byte)(alpha * 255);
        }

        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (!IsAlive) return;

            if (Picture != null)
            {
                base.Draw(xOff, yOff, screenManager);
            }
            if (text != null)
            {
                var screenPos = new Vector2(Position.X + screenManager.screenX, Position.Y + screenManager.screenY);
                screenManager.SpriteBatch.DrawString(GameFont, text, screenPos, new Color(r, g, b, a), 0, new Vector2(0, GameFont.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
            }
        }

    }

    public class PowerUp : Drawable
    {
        float xV;
        float yV;

        public enum PowerUpTypes { NULL, DOUBLEFIRE, RAPIDFIRE, MEGASHOT, CANNON, BUBBLE, REVIVE_IMMEDIATE};
        //no longer used: FASTMOVE, HEIGHTBOOST
        //immediate means it works immediately - it's not a permanent effect that attaches to the ship

        private const float DefaultFallSpeed = GameWindow.Height / 7142f;

        PowerUp NextP;
        PowerUp PrevP;
        PowerUpTypes type;

        static Sprite DoubleImage;
        static Sprite RapidImage;
        static Sprite FastImage;
        static Sprite HeightImage;
        static Sprite MegaImage;
        static Sprite CannonImage;
        static Sprite BubbleImage;
        static Sprite ReviveImage;

        public static void LoadImages(ScreenManager screenManager)
        {
            DoubleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrdouble"), 6, screenManager.GraphicsDevice);
            RapidImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrrapid"), 6, screenManager.GraphicsDevice);
            FastImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrfast"), 6, screenManager.GraphicsDevice);
            HeightImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrheight"), 6, screenManager.GraphicsDevice);
            MegaImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrmega"), 6, screenManager.GraphicsDevice);
            CannonImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrcannon"), 6, screenManager.GraphicsDevice);
            BubbleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrbubble"), 6, screenManager.GraphicsDevice);
            ReviveImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrrevive"), 6, screenManager.GraphicsDevice);
        }

        public void add(PowerUp other)
        {
            if (NextP != null)
            {
                other.NextP = NextP;
                NextP.PrevP = other;
            }
            NextP = other;
            other.PrevP = this;
        }

        public PowerUp() : this(PowerUpTypes.NULL) { }

        public PowerUp(PowerUpTypes type)
            : base(null) //must call base with null texture for now because we won't know our texture until later :/
        {
            while (type == PowerUpTypes.NULL)
            {
                if (Core.Instance.P2 != null && (!Core.Instance.P2.IsAlive || !Core.Instance.P.IsAlive) && Core.Instance.PowerUps == null) {
                    //hack: you always get a revive powerup first when one player dies in a two player game.
                    type = PowerUpTypes.REVIVE_IMMEDIATE;
                } else {
                    Array values = Enum.GetValues(typeof(PowerUpTypes));
                    type = (PowerUpTypes)values.GetValue(Core.Instance.Random.Next(values.Length));
                    if (type == PowerUpTypes.REVIVE_IMMEDIATE && (Core.Instance.P2 == null || (Core.Instance.P2.IsAlive && Core.Instance.P.IsAlive))) type = PowerUpTypes.NULL; //revive is only allowed in two player mode when one player is dead.
                }

            }

            //Set our picture, which we didn't do in base()
            switch (type)
            {
                case PowerUpTypes.DOUBLEFIRE: Picture = DoubleImage; break;
                case PowerUpTypes.RAPIDFIRE: Picture = RapidImage; break;
                case PowerUpTypes.MEGASHOT: Picture = MegaImage; break;
                case PowerUpTypes.CANNON: Picture = CannonImage; break;
                case PowerUpTypes.BUBBLE: Picture = BubbleImage; break;
                case PowerUpTypes.REVIVE_IMMEDIATE: Picture = ReviveImage; break;
            }

            this.type = type;
            Width = Picture.Width;
            Height = Picture.Height;

            xV = 0;
            yV = DefaultFallSpeed;
            IsAlive = true;
        }

        public override void Move(int delta)
        {
            if (NextP != null) NextP.Move(delta);
            Position.X += xV * delta;
            Position.Y += yV * delta;
            if (!isOnScreen()) IsAlive = false;
            //ridiculous linked list code.
            if (!IsAlive)
            {
                if (NextP != null && PrevP != null)
                {
                    NextP.PrevP = PrevP;
                    PrevP.NextP = NextP;
                    NextP = null;
                    PrevP = null;
                }
                else if (NextP == null && PrevP != null)
                {
                    PrevP.NextP = null;
                    PrevP = null;
                }
                else if (NextP != null && PrevP == null)
                {
                    Core.Instance.PowerUps = NextP;
                    NextP.PrevP = null;
                    NextP = null;
                }
                else
                {
                    Core.Instance.PowerUps = null;
                }
            }
        }

        public void TestPickedUp(Player p, Player p2)
        {
            if (NextP != null) NextP.TestPickedUp(p, p2);
            if (!IsAlive) return;
            bool touchP1 = p != null && p.IsAlive && testCollision(p);
            bool touchP2 = p2 != null && p2.IsAlive && testCollision(p2);
            if (touchP1 && !touchP2)
            {
                giveTo(p);
            }
            if (!touchP1 && touchP2)
            {
                giveTo(p2);
            }
            if (touchP1 && touchP2)
            {
                IsAlive = false;
                int dist1 = Math.Abs(centerX() - p.centerX());
                int dist2 = Math.Abs(centerX() - p2.centerX());
                if (dist1 <= dist2) //p1 gets a 1-pixel advantage :p
                {
                    giveTo(p);
                }
                else
                {
                    giveTo(p2);
                }
            }
        }

        private void giveTo(Player p)
        {
            IsAlive = false;
            p.Upgrade(type);
        }

        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (NextP != null) NextP.Draw(xOff, yOff, screenManager);
            base.Draw(xOff, yOff, screenManager);
        }
    }

    public class Sprite
    {
        public Texture2D Texture;
        public Color[] ExplosionMap;
        public int ExpWidth;
        public int ExpHeight;

        //useful globals
        public static int LargestExpWidth = -1;
        public static int LargestExpHeight = -1;

        public Sprite(Texture2D texture) : this(texture, texture) { }

        public Sprite(Texture2D texture, Texture2D explosionTexture)
        {
            this.Texture = texture;
            if (explosionTexture != null) {
                SetExplosionMapFrom(explosionTexture);
            }
        }

        public void SetExplosionMapFrom(Texture2D texture)
        {
            ExplosionMap = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(ExplosionMap);
            ExpWidth = texture.Width;
            ExpHeight = texture.Height;
            if (ExpWidth > LargestExpWidth) LargestExpWidth = ExpWidth;
            if (ExpHeight > LargestExpHeight) LargestExpHeight = ExpHeight;

        }

        public int Width {get {return Texture.Width;}}
        public int Height {get {return Texture.Height;}}
    }

    public class Drawable
    {
        public Vector2 Position;
        public int Width;
        public int Height;
        //FIXME:
        public byte r { get { return color.R; } set { color.R = value; } }
        public byte g { get { return color.G; } set { color.G = value; } }
        public byte b { get { return color.B; } set { color.B = value; } }
        public byte a { get { return color.A; } set { color.A = value; } }

        private Color color = new Color(255, 255, 255, 255);

        public bool IsAlive; //bLive
        public Sprite Picture;

        public Drawable(Sprite picture)
        {
            this.Picture = picture;
            if (picture != null)
            {
                Width = picture.Width;
                Height = picture.Height;
            }
        }

        public void SetColor(Color color)
        {
            this.color = color;
        }

        public virtual void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (!IsAlive) return;
            if (Picture != null)
            {
                screenManager.SpriteBatch.Draw(Picture.Texture, new Rectangle((int)Position.X + xOff + screenManager.screenX, (int)Position.Y + yOff + screenManager.screenY, (int)Width, (int)Height), color);
            }
            else
            {
                //Draw a rectangle in the specified color.
                screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle((int)Position.X + xOff + screenManager.screenX, (int)Position.Y + yOff + screenManager.screenY, (int)Width, (int)Height), color);
            }
        }

        #region HelperMethods

        public bool isOnScreen()
        {
            if (Position.X + Width < 0 || Position.X > GameWindow.Width) return false;
            if (Position.Y + Height < 0 || Position.Y > GameWindow.Height) return false;
            return true;
        }

        public bool isFullyOnScreen()
        {
            if (Position.X < 0 || Position.X + Width > GameWindow.Width) return false;
            if (Position.Y < 0 || Position.Y + Height > GameWindow.Height) return false;
            return true;
        }

        public int centerX()
        {
            return (int)(Position.X + Width / 2);
        }

        public int centerY()
        {
            return (int)(Position.Y + Height / 2);
        }

        //What position should I be at to be centered at this position?
        public int offCenterX(int inX)
        {
            return inX - Width / 2;
        }

        //What position should I be at to be centered at this position?
        public int offCenterY(int inY)
        {
            return inY - Height / 2;
        }

        #endregion

        public virtual void Move(int delta) { }

        public bool testCollision(Drawable o)
        {
            if (o == null) return false;
            int x = (int)Position.X;
            int y = (int)Position.Y;
            int oX = (int)o.Position.X;
            int oY = (int)o.Position.Y;
            if (oX + o.Width > x && oX < x + Width
                && oY + o.Height > y && oY < y + Height)
            {
                return true;
            }
            return false;
        }

        public bool testCollision(Vector2 pos)
        {
            if (pos == null) return false;
            int x = (int)Position.X;
            int y = (int)Position.Y;
            int oX = (int)pos.X;
            int oY = (int)pos.Y;
            if (oX >= x && oX < x + Width
                && oY >= y && oY < y + Height)
            {
                return true;
            }
            return false;
        }

        //TODO: move into Sprite.
        public void MakeIntoParticles(Sprite picture, ParticleType expType, int xOff, int yOff)
        {
            Debug.Assert(picture.ExplosionMap != null);

            ParticleGroup pGroup = Core.Instance.CreateParticleGroup(picture.ExpWidth, picture.ExpHeight, expType);
            if (pGroup == null) return; //must have run out of particle groups.
            int pos = 0;
            for (int y = 0; y < picture.ExpHeight; y++)
            {
                for (int x = 0; x < picture.ExpWidth; x++)
                {
                    //Debug.WriteLine(ExplosionMap[pos].B);
                    Color color = picture.ExplosionMap[pos];
                    if (color.A > 64 && y % Tweaking.particleSize == 0 && x % Tweaking.particleSize == 0)
                    {
                        pGroup.AddParticle((int)(Position.X + x + xOff), (int)(Position.Y + y + yOff), color, expType, (float)x / (float)picture.ExpWidth, (float)y / (float)picture.ExpHeight);
                    }
                    pos++;
                }
            }
        }

        public void MakeIntoParticles(ParticleType expType, int xOff, int yOff)
        {
            MakeIntoParticles(Picture, expType, xOff, yOff);
        }

        public void MakeIntoParticles(ParticleType expType)
        {
            MakeIntoParticles(expType, 0, 0);
        }

    }

     //Only instantiate during the loading phase, this loads art assets.
    public class PlayerUpgradesArt
    {
        public Sprite DoubleImage;
        public Sprite RapidImage;
        public Sprite FastImage;
        public Sprite HeightImage;
        public Sprite MegaImage;
        public Sprite CannonImage;
        public Sprite BubbleImage;

        public PlayerUpgradesArt(ScreenManager screenManager, string playerSuffix)
        {
            DoubleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partdouble" + playerSuffix), Gfx.StandardScale, screenManager.GraphicsDevice);
            RapidImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partrapid" + playerSuffix), Gfx.StandardScale, screenManager.GraphicsDevice);
            FastImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partfast" + playerSuffix), Gfx.StandardScale, screenManager.GraphicsDevice);
            HeightImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partheight" + playerSuffix), Gfx.StandardScale, screenManager.GraphicsDevice);
            MegaImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partmega" + playerSuffix), Gfx.StandardScale, screenManager.GraphicsDevice);
            CannonImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partcannon" + playerSuffix), Gfx.StandardScale, screenManager.GraphicsDevice);
            BubbleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partbubble" + playerSuffix), Gfx.StandardScale, screenManager.GraphicsDevice);
        }
    }

    public class PlayerUpgrades
    {
        const int boostShotCount = 1;
        const int boostROF = -150; //a quarter off your firing delay. Maxes out after you get two.
        const float boostSpeed = 0.5f;
        public const int boostDesiredY = -40;
        const int boostMegaShots = 30;
        const int cannonTimeBoost = 3000; //'millisecs
        const int cannonTimeMax = 6000;
        int cannonTime = 0;
        float cannonWarmth = 0;
        const int cannonWarmthMax = 1000; //'double the millisecs to warm up the cannon

        public int MegaShots;

        float bubbleHealth = 0;
        const int bubbleHealthMax = 100;
        int bubbleTime = 0;
        const int bubbleTimeMax = 1000; //'millisecs a bubble lasts after it is first damaged

        readonly Player p;

        bool hasDouble;
        bool hasRapid;
        bool hasHeight;
        bool hasMega;
        bool hasSpeed;
        public bool hasCannon; //public due to haxx
        bool hasBubble;

        public int AnyBonus; //number of bonuses we have.

        public PlayerUpgradesArt Art;

        public static SoundEffect DefaultBeamSound;
        private SoundEffectInstance BeamSoundInstance;


        public PlayerUpgrades(Player p, PlayerUpgradesArt art)
        {
            Debug.Assert(DefaultBeamSound != null);
            this.p = p;
            this.Art = art;
            this.BeamSoundInstance = DefaultBeamSound.CreateInstance();
            BeamSoundInstance.IsLooped = true;
        }

        #region serialization

        public void LoadFromPlayerData(PlayerData pd)
        {
            PlayerUpgrades u = p.Upgrades;
            u.SilentUpgrade(PowerUp.PowerUpTypes.BUBBLE, pd.Bubbles);
            u.SilentUpgrade(PowerUp.PowerUpTypes.CANNON, pd.Cannon);
            u.SilentUpgrade(PowerUp.PowerUpTypes.DOUBLEFIRE, pd.DoubleFire);
           // u.SilentUpgrade(PowerUp.PowerUpTypes.FASTMOVE, pd.FastMove);
           // u.SilentUpgrade(PowerUp.PowerUpTypes.HEIGHTBOOST, pd.Height);
            u.SilentUpgrade(PowerUp.PowerUpTypes.MEGASHOT, pd.Mega);
            u.SilentUpgrade(PowerUp.PowerUpTypes.RAPIDFIRE, pd.Rapid);
            //pd.SpikeWings is unused
        }

        public void GetPlayerData(PlayerData pd)
        {
            pd.Bubbles = hasBubble ? 1 : 0;
            pd.Cannon = hasCannon ? 1 : 0;
            pd.DoubleFire = hasDouble ? 1: 0;
            pd.FastMove = hasSpeed ? 1 : 0;
            pd.Height = hasHeight ? 1 : 0;
            pd.Mega = hasMega ? 1 : 0;
            pd.Rapid = hasRapid ? 1 : 0;
            pd.SpikeWings = 0;

            //special handling for multi-part upgrades:
            //only doublefire (triplefire) is supported, others will be discarded.
            if (hasDouble && p.ShotCount == 3)
            {
                pd.DoubleFire = 2; 
            }
        }

        #endregion

        private void SilentUpgrade(PowerUp.PowerUpTypes type, int amount)
        {
            if (amount <= 0) return;
            for (int i = 0; i < amount; i++)
            {
                Upgrade(type, true);
            }
        }

        //note all parameters are by reference!
        //this is a mutator kind of thing.
        public void PushDown(ref float impact, ref double shakeImpact, ref bool loseUpgrades)
        {
            //FIXME bubble protection -
            //only affects damage that would've removed upgrades. Other damage is probably self-inflicted
            if (hasBubble && bubbleHealth > 0 && loseUpgrades)
            {
                bubbleHealth -= impact;
                impact /= 3;
                shakeImpact /= 2;
                loseUpgrades = false; //protect us from losing upgrades, woot!

            }
        }

        private void drawImage(Sprite image, int x, int y, ScreenManager screenManager)
        {
            int xBack = XBack(image);
            int yBack = YBack(image);

            screenManager.SpriteBatch.Draw(image.Texture, new Rectangle(x - xBack + screenManager.screenX, y - yBack + screenManager.screenY, (int)(image.Width), (int)(image.Height)), Color.White);
        }

        private int XBack(Sprite image)
        {
            return (image.Width - p.Picture.Width) / 2; ;
        }

        private int YBack(Sprite image)
        {
            //Hack to deal with unusually sized player 2
            int pHeight = p.Picture.Height;
            if (pHeight / Gfx.StandardScale % 2  == 0) pHeight -= Gfx.StandardScale;
            return (image.Height - pHeight) / 2;
        }

        private Color beamColor = new Color(255, 255, 255, 0);
        private void drawCannon(int x, int y, ScreenManager screenManager)
        {
            beamColor.A = Math.Min((byte)255, (byte)(254 * 2 * cannonWarmth / cannonWarmthMax));
            screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle(x + screenManager.screenX, 0 + +screenManager.screenY, p.Width, y), beamColor);

        }


        //classic draworder: under: cannon, double, rapid, spike, height, mega, cannon
        // over: speed, bubble

        public void DrawUnder(int xOff, int yOff, ScreenManager screenManager)
        {
            if (hasCannon && cannonWarmth > 0)
            {
                drawCannon((int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

        }


        public void DrawOver(int xOff, int yOff, ScreenManager screenManager)
        {
            if (hasDouble)
            {
                drawImage(Art.DoubleImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasCannon)
            {
                drawImage(Art.CannonImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasSpeed)
            {
                drawImage(Art.FastImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasHeight)
            {
                drawImage(Art.HeightImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasRapid)
            {
                drawImage(Art.RapidImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasMega)
            {
                drawImage(Art.MegaImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasBubble)
            {
                drawImage(Art.BubbleImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }
        }

        static int powerUpTypesCount = Enum.GetValues(typeof(PowerUp.PowerUpTypes)).Length;
        int[] pwrUpXOff = new int[powerUpTypesCount];
        int[] pwrUpYOff = new int[powerUpTypesCount];

        const int bubbleXOff = -26;
        const int bubbleYOff = -20;

        public void Move(int delta)
        {

            if (hasBubble && bubbleHealth < bubbleHealthMax - 1) //the minus one is for int to double rounding errors (in BlitzMax, may be obselete now)
            {
                bubbleTime -= delta; //damaged bubbles decay.
                if (bubbleTime <= 0)
                {
                    bubbleHealth = 0;
                }
                if (bubbleHealth <= 0)
                {
                    ExplodeComponent(true, Art.BubbleImage);
                    hasBubble = false;
                    AnyBonus--;
                }
            }

            if (hasCannon)
            {
                if (p.RefireTimer > 0)
                {
                    cannonWarmth += delta;
                    if (cannonWarmth < cannonWarmth / 2) //hack for players who don't hold down shoot, make the cannon stay on until it's warmed up
                    {
                        p.RefireTimer = 100;
                    }
                }
                else
                {
                    cannonWarmth -= delta;
                }
                if (cannonWarmth > 0)
                {
                    MoveCannon(delta);
                    if (BeamSoundInstance.State != SoundState.Playing)
                    {
                        BeamSoundInstance.Play();
                    }
                    BeamSoundInstance.Volume = (cannonWarmth / cannonWarmthMax);
                }
                else
                {
                    //SetChannelVolume(playerBeamWeaponChannel,(1.0*beamWeaponChannelActive)/beamWeaponChannelActiveTime)
                    //fade out beam sound.
                }
            }
            else
            {
                cannonWarmth = 0;
            }

            if (cannonWarmth <= 0 && BeamSoundInstance.State == SoundState.Playing)
            {
                BeamSoundInstance.Stop();
            }
        }

        private void MoveCannon(int delta)
        {
            Debug.Assert(hasCannon);
            if (cannonWarmth > cannonWarmthMax) { cannonWarmth = cannonWarmthMax; }

            //while at low power, and not dealing damage, don't use up as much power
            //This is so to not disadvantage players who tap instead of holding fire down
            //(it still uses some power, so that holding down is a viable tactic too.)

            if (cannonWarmth < cannonWarmthMax / 2)
            {
                cannonTime -= delta / 2;
            }
            else
            {
                cannonTime -= delta; //it's firing, so use full power
            }

            if (cannonTime < 0)
            {
                cannonTime = 0;
                hasCannon = false;
                AnyBonus--;
            }
            else
            {
                if (cannonWarmth > cannonWarmthMax / 2)
                {
                    //self damage - no, this was always a lame idea  pushDown(0.035*delta,0,False)

                    //destroy enemies
                    foreach (Enemy a in Core.Instance.AlienList)
                    {
                        if (a != null && a.IsAlive && a.isOnScreen())
                        {
                            if ((a.Position.X + a.Width > p.Position.X)
                            && (a.Position.X < p.Position.X + p.Width)
                            && (a.Position.Y < p.Position.Y + 10))
                            {
                                a.Die(ParticleType.FLASH);
                                p.Kills++;
                                p.Score += a.Points;
                            }
                        }
                    }

                    //destroy projectiles
                    foreach (Shot s in Core.Instance.shotArray)
                    {
                        //Only shots completely within the beam are destroyed - i.e. the shot's width is excluded, not included.
                        if (s != null && s.IsAlive && s.isOnScreen()
                           && (s.Position.X > p.Position.X)
                            && (s.Position.X + s.Width < p.Position.X + p.Width)
                            && (s.Position.Y < p.Position.Y + 10))
                        {
                            s.IsAlive = false;
                        }
                    }
                }
                else
                {
                    //minor self damage - no, lame pushDown(0.01*delta,0,False)
                }
            }
        }

        public void Upgrade(PowerUp.PowerUpTypes type)
        {
            Upgrade(type, true);
        }

        private void Upgrade(PowerUp.PowerUpTypes type, bool showMessage)
        {
            String text = "--";
            switch (type)
            {
                case PowerUp.PowerUpTypes.DOUBLEFIRE:
                    if (p.ShotCount >= 3)
                    {
                        text = "Upgrade not needed";
                    }
                    else
                    {
                        p.ShotCount += boostShotCount;
                        hasDouble = true;
                        AnyBonus++;
                        if (p.ShotCount == 3)
                        {
                            text = "Triple fire";
                        }
                        else if (p.ShotCount == 2)
                        {
                            text = "Double fire";
                        }
                        else
                        {
                            text = "Error. unknown shotcount " + p.ShotCount;
                        }

                    }
                    break;
                case PowerUp.PowerUpTypes.RAPIDFIRE:
                    int maxROF = Player.DefaultROF / 2; //but less is more. er. so it's minROF really.
                    if (p.ROF > maxROF)
                    {
                        p.ROF += boostROF;
                        hasRapid = true;
                        AnyBonus++;
                        if (p.ROF < maxROF)
                        {
                            p.ROF = maxROF;
                            text = "Maximum firing rate";
                        }
                        else
                        {
                            text = "Firing rate increased";
                        }
                    }
                    else
                    {
                        text = "Upgrade not needed";
                    }
                    break;
                case PowerUp.PowerUpTypes.MEGASHOT:
                    //assumes unlimited megashot ammo
                    if (!hasMega)
                    {
                        hasMega = true;
                        AnyBonus++;
                        text = "Spirit Shots";
                    }
                    else
                    {
                        text = "Upgrade not needed";
                    }
                    MegaShots += boostMegaShots;
                    break;
                case PowerUp.PowerUpTypes.CANNON:
                    if (!hasCannon)
                    {
                        hasCannon = true;
                        AnyBonus++;
                        text = "Lightwave Cannon (limited use)";
                    }
                    else
                    {
                        text = "Lightwave Cannon recharged";
                    }
                    cannonTime += cannonTimeBoost;
                    if (cannonTime > cannonTimeMax)
                    {
                        cannonTime = cannonTimeMax;
                    }
                    break;
                case PowerUp.PowerUpTypes.BUBBLE:
                    if (!hasBubble || bubbleHealth < bubbleHealthMax)
                    {
                        text = "Safety Bubble";
                        hasBubble = true;
                        AnyBonus++;
                        bubbleHealth = bubbleHealthMax;
                        bubbleTime = bubbleTimeMax;
                    }
                    else
                    {
                        text = "Upgrade not needed";
                    }
                    break;
                case PowerUp.PowerUpTypes.REVIVE_IMMEDIATE:
                    if (Core.Instance.P2 != null)
                    {
                        if (!Core.Instance.P.IsAlive)
                        {
                            text = "Rescued Player 1!";
                            Core.Instance.P.revive();
                        }
                        else if (!Core.Instance.P2.IsAlive)
                        {
                            
                            text = "Rescued Player 2!";
                            Core.Instance.P2.revive();
                        }
                        else
                        {
                            text = "Upgrade not needed";
                        }
                    }
                    break;
                default:
                    text = "Unrecognised upgrade " + type;
                    Debug.WriteLine("Unrecognised upgrade " + type);
                    break;
            }
            if (showMessage && !Tweaking.lowTextMode) Core.Instance.CreateMessage(text, 2, 2);
        }

        public void LoseUpgrades()
        {
            p.resetAbilities();
            //megaShots = 0
            cannonTime = 0;
            cannonWarmth = 0;
            bubbleHealth = 0; //unneccessary
            bubbleTime = 0; //unneccessary

            ExplodeComponent(hasDouble, Art.DoubleImage);
            ExplodeComponent(hasRapid, Art.RapidImage);
            ExplodeComponent(hasHeight, Art.HeightImage);
            ExplodeComponent(hasMega, Art.MegaImage);
            ExplodeComponent(hasSpeed, Art.FastImage);
            ExplodeComponent(hasCannon, Art.CannonImage);
            ExplodeComponent(hasBubble, Art.BubbleImage);

            hasDouble = false;
            hasRapid = false;
            hasHeight = false;
            hasMega = false;
            hasSpeed = false;
            hasCannon = false;
            hasBubble = false;
            AnyBonus = 0;
        }

        private void ExplodeComponent(bool hasComponent, Sprite texture)
        {
            if (hasComponent)
            {
                p.MakeIntoParticles(texture, ParticleType.FAST_SCATTER, -XBack(texture), -YBack(texture));
            }
        }
    }

    public class Player : Creature
    {
        public static SoundEffect DefaultHitSound;
        public static SoundEffect DefaultPowerUpSound;

        public SoundEffect HitSound;
        public SoundEffect PowerUpSound;

        public int Kills;
        public int Score;
        public double Shake;
        public const double MaxShake = 200;
        double YSpeed = (GameWindow.Height / 50000d);
        public double DefaultSpeed;

        double Shield = 1;
        double SuperShield = 0.7;
        int SuperY;
        const int DeathY = GameWindow.Height;
        bool isAI;
        float ShotSpeedMulti = 1;

        public bool left;
        public bool right;
        public bool up;
        public bool down;

        int Id; //for 2 player

        public Vector2 LastPosition; //used by RelativeAnchors
        public Vector2 LastMove; //used by RelativeAnchors control

        //powerup stuff
        const int DefaultShotCount = 1;
        public const int DefaultROF = 600;
        const float DefaultShotSpeedMulti = 1.0f;
        public const int DefaultDesiredY = DeathY - 130;

        public readonly PlayerUpgrades Upgrades;

        private const int BaseDamageDistanceFactor = (int)(((float)DeathY - (float)DefaultDesiredY) / 100f);

        public int DesiredY;

        public int ShotCount;
        int CurrentShot;
        private int invulnerabilityTimer = 0;
        private const int RESPAWN_INVULERABILITY_TIME = 100;

        public void Upgrade(PowerUp.PowerUpTypes type)
        {
            PowerUpSound.Play();
            Upgrades.Upgrade(type);
        }

        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (!IsAlive) return;
            Upgrades.DrawUnder(xOff, yOff, screenManager);
            base.Draw(xOff, yOff, screenManager);
            Upgrades.DrawOver(xOff, yOff, screenManager);
        }

        public Player(int id)
            : base(id == 0? Core.Instance.Art.Player1 : Core.Instance.Art.Player2)
        {
            Art = Core.Instance.Art;
            Position.X = GameWindow.Width / 2 - Width / 2;
            Position.Y = DefaultDesiredY;
            LastMove.X = 0;
            LastMove.Y = 0;
            LastPosition.X = Position.X;
            LastPosition.Y = Position.Y;
            Id = id;
            //set keys.

            //Base creation (common to all players)
            HitSound = DefaultHitSound;
            PowerUpSound = DefaultPowerUpSound;
            ShootSound = Snd.PlayerShotSound;

            if (id == 0)
            {
                Upgrades = new PlayerUpgrades(this, Core.Instance.Art.Player1Upgrades);
            }
            else
            {
                Upgrades = new PlayerUpgrades(this, Core.Instance.Art.Player2Upgrades);
            }

            IsAlive = true;
            SuperY = DeathY - (int)Height;

            isAI = false;
            resetAbilities();
        }

        public void resetAbilities()
        {
            DefaultSpeed = 0.3f;
            Speed = (float)DefaultSpeed; //    'horizonal
            ROF = DefaultROF;
            ShotSpeedMulti = DefaultShotSpeedMulti;
            ShotCount = DefaultShotCount;
            Upgrades.MegaShots = 0;
            DesiredY = DefaultDesiredY;
        }

        private void PushDown(float impact, double shakeImpact, bool loseUpgrades)
        {
            if (invulnerabilityTimer > 0)
            {
                return;
            }
            impact *= BaseDamageDistanceFactor;
            Upgrades.PushDown(ref impact, ref shakeImpact, ref loseUpgrades);
            int y = (int)Position.Y;
            if (y > SuperY)
            {
                Position.Y += (float)(impact * SuperShield);
            }
            else
            {
                Position.Y += (float)(impact * Shield);
            }
            Shake += 6.67 * shakeImpact;
            if (Shake > MaxShake) Shake = MaxShake;

            if (loseUpgrades)
            {
                Upgrades.LoseUpgrades();
            }
        }

        public void PushDown(float impact, double shakeImpact)
        {
            PushDown(impact, shakeImpact, true);
        }

        private void Shoot()
        {
            /*FIXME should be in PlayerUpgrades */
            if (Upgrades.hasCannon)
            {
                RefireTimer = 100;
                return;
            }
            CurrentShot++;
            ShootSound.Play();

            if (ShotCount == 1)
            {
                SpawnShot(centerX(), (int)Position.Y);
                RefireTimer = ROF;
            }
            else if (ShotCount == 2)
            {
                if (CurrentShot == 1)
                {
                    SpawnShot((int)(Position.X+Gfx.StandardScale/2), (int)Position.Y);
                }
                else
                {
                    SpawnShot((int)(Position.X + Width - Gfx.StandardScale / 2), (int)Position.Y);
                }
                RefireTimer = (int)(ROF * 0.65); //0.5 makes more sense for 2 barrels, but is too powerful.
            }
            else
            { //shotCount == 3
                if (CurrentShot == 1)
                {
                    SpawnShot(centerX(), (int)Position.Y);
                }
                else if (CurrentShot == 2)
                {
                    SpawnShot((int)(Position.X + Gfx.StandardScale / 2), (int)Position.Y);
                }
                else
                {
                    SpawnShot((int)(Position.X + Width - Gfx.StandardScale / 2), (int)Position.Y);
                }
                RefireTimer = (int)(ROF * .5); //0.3333 makes more sense for 3 barrels, but is too powerful
            }
            if (CurrentShot >= ShotCount) CurrentShot = 0;

        }

        private void SpawnShot(int inX, int inY)
        {
            Shot s;
            if (Upgrades.MegaShots > 0)
            {
                //MegaShots--; don't currently limit ammo
                s = Shot.CreatePlayerShot(Shot.ShotType.Mega, inX, inY, ShotSpeedMulti, Id);
            }
            else
            {
                s = Shot.CreatePlayerShot(Shot.ShotType.Normal, inX, inY, ShotSpeedMulti, Id);
            }
            s.Owner = Id;
            Core.Instance.AddShot(s);

        }

        private void DoCollisions()
        {
            //Shots
            Core core = Core.Instance;
            foreach (Shot s in core.shotArray)
            {
                if (s == null) break; //A null means we've reached the end of the real values.
                if (s.IsAlive && s.HurtsPlayer > 0 && testCollision(s))
                {
                    s.Die();
                    PushDown(s.HurtsPlayer, s.HurtsPlayer);
                    Snd.Instance.PlayHitSound(HitSound);
                    //note, the player never explodes.
                }
            }
        }

        public override void Move(int delta)
        {
            if (!IsAlive) return;

            if (invulnerabilityTimer > 0)
            {
                invulnerabilityTimer--;
            }

            DoCollisions();
            if (isAI)
            {
                //AiMove(delta); FIXME
            }
            else
            {
                //FIXME: keyboard input
                if (left) Position.X -= Speed * delta;
                if (right) Position.X += Speed * delta;

                if (Options.Instance.VerticalMotion)
                {
                    if (up) Position.Y -= Speed * delta;
                    //downwards movement - note that we cannot fly off the bottom of the screen, but we can otherwise be pushed off.
                    if (down && Position.Y + Height < GameWindow.Height)
                    {
                        Position.Y += Speed * delta;
                        if (Position.Y + Height > GameWindow.Height) Position.Y = GameWindow.Height - Height;
                    }
                    if (Position.Y < 0) Position.Y = 0;
                }

                if (Options.Instance.RabbleControls)
                {
                    //infinite speed.
                    MouseState ms = Mouse.GetState();
                    if (Id == 0)
                    {
                        
                        Position.X = ms.X * GameWindow.Width / 1366;
                    }
                    else
                    {
                        Position.X = ms.Y * GameWindow.Width / 768;
                    }
                }

                if (Position.X < 0) Position.X = 0;
                if (Position.X + Width > GameWindow.Width) Position.X = GameWindow.Width - Width;

                if (up || Upgrades.hasCannon || Options.Instance.RabbleControls && Core.Instance.EnemyCount > 0)
                {
                    if (RefireTimer == 0)
                    {
                        Shoot();
                    }
                }
            }

            Upgrades.Move(delta);

            RefireTimer -= delta;
            if (RefireTimer < 0) RefireTimer = 0;
            if (Position.Y > DesiredY)
            {
                Position.Y -= (float)(YSpeed * delta);
            }

            //crude hacks - if verticle motion was enabled, but is now disabled, the player may be higher than allowed.
            //make the player fly down to an appropriate level.
            //We don't want to make players who got and lost a height boost fly downwards, we let them keep a little extra height in case that was the case.
            //a proper fix would be to alter the max allowed height when the player gets (and loses) a height boost, or remove the heightboost powerup...
            if (!Options.Instance.VerticalMotion && Position.Y < DesiredY + PlayerUpgrades.boostDesiredY) 
            {
                Position.Y += (float)(YSpeed * delta);
            }

            //calculate how much we moved this frame
            LastMove.X = Position.X - LastPosition.X;
            LastMove.Y = Position.Y - LastPosition.Y;
            LastPosition.X = Position.X; //reset for next frame
            LastPosition.Y = Position.Y;
        }
        /*Method aiMove(delta:Int)
           Local direction:Float
           Local thresh:Float = 0.00
           Local rightHelp:Int=0
           Local leftHelp:Int=0
             'head for lowest enemy
             If centreX() > (coreType.getInstance().lowestEnemyCentreX) + 5 Then
                 direction = -0.01
             ElseIf centreX()  < (coreType.getInstance().lowestEnemyCentreX) - 5 Then
                 direction = 0.01
             Else
                 direction = 0
             End If          
             Local danger:Float      
            
             'remember the way to the centre for when we don't know which way to go
             If centreX() > windowType.width / 2 Then 
               leftHelp = 3
             Else
               rightHelp = 3
             EndIf
            
             'a beam gets us when our centres are within 18,
             'so an actual hurt zone's danger rating is as low as +- 1/18 = 0.055            
             Local beam:beamType[] = coreType.getInstance().beam 
             For Local i:Int = 0 To beam.dimensions()[0] - 1
                 If beam[i].bLive = True Then                
                     If beam[i].timer > beam[i].growspan Then danger = 1.0 Else danger = (beam[i].timer * 1.0) / beam[i].growspan                            
                     If (x + width + 20 > beam[i].x) And (x < beam[i].x + beam[i].width2 + 20) Then '20s are a 'safety margin'                   
                             'DebugLog("AI: look out, a beam!")
                                     Local dist:Int = centreX() - beam[i].centreX()
                                     'treat anywhere underneath a beam equally   - helps us not get stuck.                   
                         If dist < 0 And dist > -(width+beam[i].width)/2 Then dist = -(width+beam[i].width)/2 + leftHelp
                                   If dist => 0 And dist < (width+beam[i].width)/2 Then dist = (width+beam[i].width)/2 + rightHelp
                                
                                   'stuck in a corner?
                                   If beam[i].x < width + 5 And dist < 0 Then dist = -dist '5 is a safety margin
                                   If beam[i].x + beam[i].width > windowType.width - (width + 5) And dist > 0 Then dist = -dist '5 is a safety margin

                         direction = direction + danger/dist
                     End If                  
                 End If
             Next

       'an oncoming shot should never be more dangerous than an actual hurtzone (0.055). fixit?
             Local core:coreType = coreType.getInstance()
             Local s:shotType
             For Local i:Int = 0 To core.shotFirstEmpty - 1 Step 1
                 s=core.shotArray[i]

                 If s.bLive = True And s.hurtsPlayer > 0 Then
                     If s.x + s.width + 30 > x And s.x < x + width + 30 Then  '30s are a 'safety margin'
                         If s.speed > 0 And s.y < y + height Then 'above us, coming down
                            
                           Local timeLeft%
                             timeLeft= (y - (s.y + s.height)) / s.speed 'ticks until collision.
                             'scaled to ignore projectiles more than 400 ticks away
                             If timeLeft < 400 Then
                                 'DebugLog("AI: look out, a shot! - distance " + timeLeft)                           
                               If timeLeft < 1 Then timeLeft = 1
                                 danger = 400.0 / timeLeft
                                 Local dist:Int = centreX() - s.centreX()
            
                                 'stop getting stuck between two parallel shots :(
                                 'treat anywhere underneath a shot equally.
                       If dist < 0 And dist > -(width+s.width)/2 Then dist = -(width+s.width)/2+leftHelp
                                 If dist => 0 And dist < (width+s.width)/2 Then dist = (width+s.width)/2 + rightHelp 'offset to stop getting stuck                               
                                
                                 'stuck in a corner?
                                 If s.x < width + 5 And dist < 0 Then dist = -dist '5 is a safety margin
                                 If s.x + s.width > windowType.width - (width + 5) And dist > 0 Then dist = -dist '5 is a safety margin
                                
                       direction = direction + danger/dist
                             EndIf                           
                         EndIf
                   EndIf
                 EndIf
             Next

             If direction > 0 + thresh Then
                 x:+ speed*delta
                 If x > 300 - width Then x = 300 - width     
             ElseIf direction < 0 - thresh Then
                 x:- speed*delta
                 If x < 0 Then x = 0
             End If
            
             If coreType.getInstance().special = Null Then       
                 If lastFired = 0 Then
                     shoot()
                 EndIf   
           EndIf
         EndMethod*/

        public PlayerData GetPlayerData()
        {
            PlayerData pd = new PlayerData();
            pd.IsNull = false;
            pd.IsAlive = IsAlive;
            pd.Id = Id;
            pd.Score = Score;
            Upgrades.GetPlayerData(pd);
            return pd;
        }

        public static Player LoadFromPlayerData(PlayerData pd)
        {
            if (pd.IsNull) return null;
            Player p = new Player(pd.Id);
            p.IsAlive = pd.IsAlive;
            p.Score = pd.Score;
            p.Upgrades.LoadFromPlayerData(pd);
            return p;
        }

        internal void revive()
        {
            IsAlive = true;
            Position.Y = GameWindow.Height - 1;
            invulnerabilityTimer = RESPAWN_INVULERABILITY_TIME;
        }
    }

    public enum ParticleType { SCATTER_UNUSED, FAST_SCATTER, FLASH, STILL };

    public class PlayerData
    {
        private static string DATA_HEADER = "[PlayerData]";
        private static string DATA_FOOTER = "[/PlayerData]";
        public static PlayerData Deserialize(StreamReader reader)
        {
            string header = reader.ReadLine();
            if (!header.Equals(DATA_HEADER)) {
                throw new FormatException("Error: corrupt player save data - wrong header " + header);
            }
            PlayerData pd = new PlayerData();
            pd.IsNull = IOUtil.ReadBool(reader);
            if (!pd.IsNull) 
            {
                pd.IsAlive = IOUtil.ReadBool(reader);
                pd.Id = IOUtil.ReadInt(reader);
                pd.Score = IOUtil.ReadInt(reader);
                pd.Bubbles = IOUtil.ReadInt(reader);
                pd.Cannon = IOUtil.ReadInt(reader);
                pd.DoubleFire = IOUtil.ReadInt(reader);
                pd.FastMove = IOUtil.ReadInt(reader);
                pd.Height = IOUtil.ReadInt(reader);
                pd.Mega = IOUtil.ReadInt(reader);
                pd.Rapid = IOUtil.ReadInt(reader);
                pd.SpikeWings = IOUtil.ReadInt(reader);
            }
            string footer = reader.ReadLine();
             if (!footer.Equals(DATA_FOOTER)) {
                Debug.WriteLine("Error: corrupt player save data - wrong footer " + footer);
            }
            return pd;
        }

        public void Serialize(StreamWriter writer)
        {
            writer.WriteLine(DATA_HEADER);
            writer.WriteLine(IsNull);
            if (!IsNull) 
            {
                writer.WriteLine(IsAlive);
                writer.WriteLine(Id);
                writer.WriteLine(Score);
                writer.WriteLine(Bubbles);
                writer.WriteLine(Cannon);
                writer.WriteLine(DoubleFire);
                writer.WriteLine(FastMove);
                writer.WriteLine(Height);
                writer.WriteLine(Mega);
                writer.WriteLine(Rapid);
                writer.WriteLine(SpikeWings);
            }
            writer.WriteLine(DATA_FOOTER);
        }
        public bool IsNull = true;
        public bool IsAlive;
        public int Id;
        public int Score;
        public int Bubbles;
        public int Cannon;
        public int DoubleFire;
        public int FastMove;
        public int Height;
        public int Mega;
        public int Rapid;
        public int SpikeWings;
    }

    public class SavedGame
    {
        #region singleton
        static SavedGame instance;
        public static SavedGame Instance
        {
            get
            {
                if (instance == null) instance = new SavedGame();
                return instance;
            }
        }
        #endregion

        const string filename = "savedgame.txt";
        const int version = 1;
        private bool isAlive;
        public bool IsAlive { get {return isAlive;}}
        int level;
        public int Level { get { return level; } }
        PlayerData P;
        PlayerData P2;

        private SavedGame()
        {
            Action<StreamReader> handler = delegate(StreamReader reader)
            {
                int version = IOUtil.ReadInt(reader);
                if (version == 1)
                {
                    level = IOUtil.ReadInt(reader);
                    if (level == -1)
                    {
                        isAlive = false;
                        return;
                    }
                    P = PlayerData.Deserialize(reader);
                    P2 = PlayerData.Deserialize(reader);
                    isAlive = true;
                }
            };
            Action<int> onFailure = delegate(int i) { isAlive = false; };

            IOUtil.ReadFile(filename, handler, onFailure);
        }

        public void Delete()
        {
            isAlive = false;
            //TODO: just delete the file!
            Save(-1, null, null);
        }

        public void Save(int level, Player player1, Player player2)
        {
            isAlive = true;
            //populate my fields with the new data.
            this.level = level;
            P = ( player1 == null) ? new PlayerData() : player1.GetPlayerData();
            P2 = ( player2 == null) ? new PlayerData() : player2.GetPlayerData();

            //persist.

            Action<StreamWriter> handler = delegate(StreamWriter writer)
            {
                writer.WriteLine(version);
                writer.WriteLine(level);
                P.Serialize(writer);
                P2.Serialize(writer);
            };

            IOUtil.WriteFile(filename, handler, IOUtil.NothingAction);
            if (Tweaking.DebugFileStuff) DebugFileStuff.DisplayFileContents(filename);
        }

        public Player GetP1()
        {
            return Player.LoadFromPlayerData(P);
        }

        public Player GetP2() {
            return Player.LoadFromPlayerData(P2);
        }
    }

    public static class DebugFileStuff
    {
        public static void DisplayFileContents(string filename)
        {
            Action<StreamReader> handler = delegate(StreamReader reader)
            {
                Debug.WriteLine("Displaying file " + filename);
                Debug.WriteLine("--- file begins ---");
                Debug.WriteLine(reader.ReadToEnd());
                Debug.WriteLine("--- file ends ---");
            };
            IOUtil.ReadFile(filename, handler,IOUtil.NothingAction);
        }
    }

    public static class IOUtil
    {
        public static Action<int> NothingAction = delegate(int i) { };

        public static int ReadInt(StreamReader reader)
        {
            string s = reader.ReadLine();
            return Int32.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool ReadBool(StreamReader reader)
        {
            string s = reader.ReadLine();
            return bool.Parse(s);
        }

        //because we can't use ASCIIEncoding class
        public static byte[] StringToAscii(string s)
        {
            byte[] retval = new byte[s.Length];
            for (int ix = 0; ix < s.Length; ++ix)
            {
                char ch = s[ix];
                if (ch <= 0x7f) retval[ix] = (byte)ch;
                else retval[ix] = (byte)'?';
            }
            return retval;
        }


        public static void WriteFile(string filename, Action<StreamWriter> handler, Action<int> onFailure)
        {
            bool success = false;
            try
            {
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    handler.Invoke(writer);
                    writer.Flush();
                    writer.Close();
                    success = true;
                }
            }
            catch (Exception)
            {
                success = false;
            }
            if (!success)
            {
                onFailure(0);
            }
        }

        public static void ReadFile(string filename, Action<StreamReader> handler, Action<int> onFailure)
        {
            bool success = false;
            try
            {
                if (File.Exists(filename))
                {
                    using (StreamReader reader = new StreamReader(filename))
                    {
                        handler.Invoke(reader);
                        reader.Close();
                        success = true;
                    }
                }
            }
            catch (Exception)
            {
                success = false;
            }
            if (!success)
            {
                onFailure(0);
            }
        }

        public static bool FileExists(string filename)
        {
            if (File.Exists(filename))
            {
                return true;
            }
            return false;
        }
    }

    public class Options
    {
        //singleton
        static Options instance;
        public static Options Instance
        {
            get
            {
                if (instance == null) instance = new Options();
                return instance;
            }
        }

        const string filename = "options.txt";

        //internal data
        int version = 1;
        bool enableSound = true;
        bool enableMusic = true;
        bool fullScreen = true;
        bool verticalMotion = false;
        bool mouseControls = false;
        private SpaceOctGame game;

        //public properties
        public bool EnableMusic
        {
            get { return enableMusic; }
            set
            {
                enableMusic = value;
                if (!enableMusic && MediaPlayer.GameHasControl && MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.Stop();
                }
                Save();
            }
        }

        //public properties
        public bool FullScreen
        {
            get { return fullScreen; }
            set
            {
                fullScreen = value;
                game.SetFullScreen(fullScreen);
                Save();
            }
        }

        public bool EnableSound
        {
            get { return enableSound; }
            set
            {
                EnableSound = value;
                Save();
            }
        }

        public bool VerticalMotion
        {
            get { return verticalMotion; }
            set
            {
                verticalMotion = value;
                Save();
            }
        }

        public bool RabbleControls
        {
            get { return mouseControls; }
            set
            {
                mouseControls = value;
                Save();
            }
        }

        private void Save()
        {
            Action<StreamWriter> handler = delegate(StreamWriter writer)
            {
                writer.WriteLine(version);
                writer.WriteLine(mouseControls);
                writer.WriteLine(enableMusic);
                writer.WriteLine(FullScreen);
            };

            IOUtil.WriteFile(filename, handler, IOUtil.NothingAction);
        }

        public void SetGame(SpaceOctGame game)
        {
            this.game = game;
        }

        private Options()
        {
            Action<StreamReader> handler = delegate(StreamReader reader)
            {
                int version = IOUtil.ReadInt(reader);
                mouseControls = IOUtil.ReadBool(reader);
                enableMusic = IOUtil.ReadBool(reader);
                FullScreen = IOUtil.ReadBool(reader);
            };
            IOUtil.ReadFile(filename, handler, IOUtil.NothingAction);
        }
    }

    public class Tweaking
    {
        public const bool lowTextMode = true;

        //Settings here
        public static bool isCheatsEnabled = false;
        public const bool SimulateTrial = false;

        //other random stuff
        public static bool EnableSound = true; //true
        //These must be set correctly before release

        public const bool DebugFileStuff = false; // set to false
        public const bool DrawUpsellButtons = false; // set to false

        public static bool ShowPerfStats = false; //false
        public const bool ParticleStressTest = false; //false makes particles last forever
        public const int particleSize = 4; //4 is the minimum OK value. 12 gives you 1-to-1 particles to Big Pixels.
        public const bool AllowParticlePushing = true; //true - this drops the particle limit on my PC from 13,360 to 10,000
    }

    public class ParticleGroup : Drawable
    {
        #region pooling
        public const int PoolSize = 50; // in testing, 20 was the max in single player
        private static ParticleGroupPool Pool;

        public static ParticleGroup Create(ParticleType expType)
        {
            Debug.Assert(Pool != null, "Pool not initialized");
            ParticleGroup p = Pool.Fetch();
            p.Initialize(expType);
            return p;
        }

        private static void Release(ParticleGroup p)
        {
            Debug.Assert(Pool != null, "Pool not initialized");
            //TODO: check that the object has not already been released. That could get weird.
            Pool.Release(p);
        }

        public static void CreatePool()
        {
            if (Pool != null) return;
            Pool = new ParticleGroupPool(PoolSize);
        }
        #endregion


        float normalAlphaDrain;
        float alpha; //duplicates int a;
        Particle[] ptArray;
        int ptFirstEmpty;
        Core core;
        ParticleType expType;

        //Should only be called by the pool
        public ParticleGroup()
            : base(null)
        {
            core = Core.Instance;
            IsAlive = false; //Create me properly with Initialize
            //This will only work if all artsets have already been loaded, so that we know the max size.
            Debug.Assert(Sprite.LargestExpHeight > 0);
            Debug.Assert(Sprite.LargestExpWidth > 0);
            ptArray = new Particle[Sprite.LargestExpHeight*Sprite.LargestExpWidth/Tweaking.particleSize/Tweaking.particleSize];
        }

        public void Initialize(ParticleType expType)
        {
            IsAlive = true;
            alpha = 1.0f;
            a = (byte)(alpha * 255f);
            this.expType = expType;
            if (expType == ParticleType.SCATTER_UNUSED)
            {
                normalAlphaDrain = 1f;
            }
            else if (expType == ParticleType.FAST_SCATTER)
            {
                normalAlphaDrain = 1f;
            }
            else
            {
                normalAlphaDrain = 0.5f;
            }       
            
            //All of our particles from our last lifetime should have already been released
            Debug.Assert(ptFirstEmpty == 0);
        }

        public void AddParticle(int x, int y, Color color, ParticleType expType, float relX, float relY)
        {
            Particle pt = Particle.Create(x, y, color, relX, relY, expType);
            ptArray[ptFirstEmpty] = pt;
            ptFirstEmpty++;
            if (ptFirstEmpty == ptArray.Length)
            {
                Debug.WriteLine("Warning! out of particles");
                ptFirstEmpty = 0;
            }
        }

        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (!IsAlive) return;
            for (int i = 0; i < ptFirstEmpty; i++)
            {
                Particle pt = ptArray[i];
                if (pt != null)
                {
                    pt.Draw(xOff, yOff, screenManager); //pass in color, alpha
                }
            }
        }

        public override void Move(int delta)
        {
            if (!IsAlive) return;
            if (alpha <= 0)
            {
                IsAlive = false;
                return;
            }
            float alphaDrain = 0.001f; //Should be passed in, used to tweak performance
            if (Tweaking.ParticleStressTest) alphaDrain = 0f;
            alpha -= normalAlphaDrain * alphaDrain * delta;
            if (alpha < 0) alpha = 0;
            a = (byte)(alpha * 255f);
            bool canBePushed = Tweaking.AllowParticlePushing;
            Core core = Core.Instance; //another unverified performance tweak. Pass it in so it doesn't have to be looked up. Can't think it makes a difference.
            for (int i = 0; i < ptFirstEmpty; i++)
            {
                Particle pt = ptArray[i];
                if (pt != null)
                {
                    core.ActiveParticleCounter++;
                    pt.Move(delta, a, canBePushed, core);
                }
                //We don't delete dead particles. They all disappear when the ParticleGroup does.
            }
        }

        public void ReleaseToPool()
        {
            for (int i = 0; i < ptFirstEmpty; i++)
            {
                Particle p = ptArray[i];
                if (p != null)
                {
                    Particle.Release(p);
                    ptArray[i] = null;
                }
            }
            ptFirstEmpty = 0;
            ParticleGroup.Release(this);
        }
    }

    public class Particle : Drawable
    {
        float xV;
        float yV;
        const float speed = 0.2f;
        private ParticleType expType;

        public override void Move(int delta)
        {
            throw new InvalidOperationException("Particles don't actually support the normal move method, use the special one instead. Wow this is naughty.");
        }

        #region pooling
        private const int PoolSize = 8000; //performance degrades at 5000
        private static ParticlePool Pool;

        public static Particle Create(int x, int y, Color color, float relX, float relY, ParticleType type)
        {
            Debug.Assert(Pool != null, "Pool not initialized");
            Particle p = Pool.Fetch();
            initialize(p, x, y, color, type);
            return p;
        }

        public static void Release(Particle p)
        {
            Debug.Assert(Pool != null, "Pool not initialized");
            Debug.Assert(p.IsAlive);
            p.IsAlive = false;
            Pool.Release(p);
        }

        public static void CreatePool()
        {
            if (Pool != null) return;
            Pool = new ParticlePool(PoolSize);
        }
        #endregion

        //should only be called by Pool
        public Particle()
            : base(null)
        {
        }


        private static void initialize(Particle p, int x, int y, Color color, ParticleType type)
        {
            Debug.Assert(color.A > 0);
            p.r = color.R;
            p.g = color.G;
            p.b = color.B;
            p.a = 255; //Controlled by particleGroup
            p.Position.X = x;
            p.Position.Y = y;
            p.Width = Tweaking.particleSize;
            p.Height = Tweaking.particleSize;
            p.expType = type;
            p.IsAlive = true;
            Core core = Core.Instance;

            switch (type)
            {
                case ParticleType.SCATTER_UNUSED:
                    p.xV = (float)(core.Random.NextDouble() * randomBetween(-1.5, 1.5));
                    p.yV = (float)(core.Random.NextDouble() * randomBetween(-1.5, 1.5));
                    break;
                case ParticleType.FAST_SCATTER:
                    p.xV = (float)(core.Random.NextDouble() * randomBetween(-1.5, 1.5));
                    p.yV = (float)(core.Random.NextDouble() * randomBetween(-1.5, 1.5));
                    break;
                case ParticleType.FLASH:
                    p.xV = 0;
                    p.yV = 0;
                    // all particles are white
                    p.r = 255;
                    p.g = 255;
                    p.b = 255;
                    //p.xV = (float)(core.Random.NextDouble() * randomBetween(-1.5, 1.5));
                    //p.yV = (float)(core.Random.NextDouble() * randomBetween(-1.5, 1.5));
                    break;
            }
        }

        /*Case SMALLSCATTER
        p.xV = Rnd(0.5,-0.5) * speed * Rnd()
        p.yV = Rnd(0.5,-0.5) * speed * Rnd()
        p.r = r
        p.g = g
        p.b = b                                          
    Case SPIN   
        p.r = r
        p.g = g
        p.b = b                                     
        p.xV = -(relX * 2 - 1) * speed * 5
        p.yV = 0        
    Case IMPLODE
        p.r = r
        p.g = g
        p.b = b                     
        p.xV = -(relX * 2 - 1) * speed * 2
        p.yV = -(relY * 2 - 1) * speed * 2*/

        private static double randomBetween(double low, double high)
        {
            Random random = Core.Instance.Random;
            double range = high - low;
            return (random.NextDouble() * range) + low;

        }

        public void Move(int delta, byte a, bool canBePushed, Core core)
        {
            Position.X += xV;
            Position.Y += yV;

            this.a = a;

            //friction
            xV *= 0.98f;
            yV *= 0.98f;

            //gravity:
            if (expType==ParticleType.FAST_SCATTER) {
                yV += 0.2f;
            }

            //collisions
            if (canBePushed)
            {
                //Find all shots that are vertically near us.
                int cell = Core.PositionToIndex(Position);
                List<Shot> shots1;
                if (core.shotLookup.TryGetValue(cell, out shots1)) 
                {
                    TryGettingPushedByShots(delta, shots1);
                }
                //check the cell above as well, in case we are near the edge of a cell.
                List<Shot> shots2;
                if (core.shotLookup.TryGetValue(cell - 1, out shots2)) {
                    TryGettingPushedByShots(delta, shots2);
                };
            }
        }

        private void TryGettingPushedByShots(int delta, List<Shot> shots)
        {
            //Loop through the shots.
            if (shots != null && shots.Count > 0)
            {

                for (int i = 0; i < shots.Count(); i++)
                {
                    Shot s = shots[i];
                    if (s.IsAlive)
                    {
                        //Check if it is horizontally near us.
                        double yDist = Math.Abs(s.centerX() - centerX()) / (s.Width * 2.5);
                        if (yDist < 1d)
                        {
                            //check that it vertically passed us this frame.
                            float heightDiff = s.Position.Y - Position.Y;
                            int dir = Math.Sign(s.YSpeed);
                            heightDiff *= dir;
                            int deltaDist = (int)(dir * s.YSpeed * delta * 1.5); //the distance the shot moves per frame ( plus half a frame to be safe)
                            if (heightDiff > 0 && heightDiff < deltaDist)
                            {
                                float multi = Math.Min(4f, 6f - ((float)yDist * 6f)); // from distance 0-2 speed = 4f, after that there's a linear decline.
                                int direction = Math.Sign(Position.X - s.centerX());
                                if (direction == 0) direction = 1; //we must be on one side of the shot or the other.
                                xV = xV + 0.005f * multi * delta * direction;
                                //HAXX:
                                /*r = (byte)Core.Instance.Random.Next(0, 255);
                                g = (byte)Core.Instance.Random.Next(0, 255);
                                b = (byte)Core.Instance.Random.Next(0, 255);//*/
                            }

                        }

                    }
                }

            }
        }


    }

    /*Type particleType Extends drawable

'assumes shot array is sorted (low to high)
'finds the first (loweest) shot that can effect it
'uses binary search
'takes lowest and highest shot to check (0,shotFirstEmpty-1)
Method findFirstNearbyShot:Int(shotArray:shotType[],lo:Int,hi:Int)

    If lo >= hi Then 
        Return 0
    EndIf

    Local maxShotHeight:Int = coreType.getInstance().maxShotHeight

    Local midPoint:Int
        
    While (hi - lo > 1)
        
        'take middle shot
        midPoint = lo + (hi - lo) / 2
        Local midY:Int = shotArray[midPoint].y
                
        If midY > y + height Then
            'his y is too large, go to lower ys
            'lo = lo
            hi = midPoint - 1
        'ElseIf midY + maxShotHeight < y Then
        '   'his y is too small, go to larger ys
        '   lo = midPoint + 1
        '   'hi = hi
        Else
            lo = lo
            hi = midPoint
        EndIf
    Wend
        
    If shotArray[lo].y + maxShotHeight >= y Then
        Return lo
    Else
        Return hi
    EndIf
EndMethod


EndType
*/

    public class Beam : Drawable
    {
        int Timer;
        Enemy Parent;
        const int LifeSpan = 1400;
        const int GrowSpan = 900;
        const float BeamSpeed = GameWindow.Height * 0.67f / 500;
        const int Width2 = GameWindow.Width * 15 / 300;
        const int InitialWidth = GameWindow.Width * 2 / 300;
        const float Damage = 0.25f;
        const float Shake = 0.15f;

        public Beam()
            : base(null)
        {
            Position.X = -1;
            Position.Y = -1;
            Width = InitialWidth;
            Height = 0;
            r = 255;
            g = 255;
            b = 255;
        }

        public void Fire(int XIn, int YIn, Enemy a)
        {
            Width = InitialWidth;
            Position.X = offCenterX(XIn);
            Position.Y = YIn;
            Height = 0;
            IsAlive = true;
            Timer = 0;
            Parent = a;
        }

        public void Move(int delta, Player p, Player p2)
        {
            if (!IsAlive) return;
            Timer += delta;
            if (Timer > LifeSpan || Parent == null || !Parent.IsAlive)
            {
                IsAlive = false;
            }

            bool isHit = false;
            Height += (int)(delta * BeamSpeed);
            if (Timer > GrowSpan)
            {
                //make sure we are at full width.
                if (Width < Width2)
                {
                    int diff = Width - Width2;
                    Width = Width2;
                    Position.X += diff / 2;
                }
                //Hit and hurt the player
                if (p.IsAlive && (p.Position.X + p.Width > Position.X) && (p.Position.X < Position.X + Width) && (p.Position.Y + p.Height > Position.Y))
                {
                    p.PushDown(delta * Damage, delta * Shake);
                    isHit = true;
                }
                if (p2 != null && p2.IsAlive && (p2.Position.X + p2.Width > Position.X) && (p2.Position.X < Position.X + Width) && (p2.Position.Y + p2.Height > Position.Y))
                {
                    p2.PushDown(delta * Damage, delta * Shake);
                    isHit = true;
                }
                if (isHit)
                {
                    Snd.Instance.PlayerHitByBeam();
                }
            }
            else
            {
                //still growing - only grow by even amount
                int newWidth = (Timer * Width2) / GrowSpan;
                if (newWidth % 2 == 0)
                {
                    int diff = Width - newWidth;
                    Width = newWidth;
                    Position.X += diff / 2;
                }
            }
        }

        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (Timer < GrowSpan)
            {
                //FIXME: set alpha to 1.0*Timer/GrowSpan
                a = (byte)(255f * ((float)Timer / (float)GrowSpan));
                r = (byte)(255f * ((float)Timer / (float)GrowSpan));
                b = (byte)(255f * ((float)Timer / (float)GrowSpan));
                g = (byte)(255f * ((float)Timer / (float)GrowSpan));
                base.Draw(xOff, yOff, screenManager);
            }
            else
            {
                a = 255;
                base.Draw(xOff, yOff, screenManager);
            }
        }
    }

    public class Snd
    {
        public static Song music;

        public static SoundEffect PlayerShotSound;
        public static SoundEffect AlienDieSound;
        private static Snd instance;

        public static SoundEffect envSound;
        private SoundEffectInstance envSoundInstance;
        private int envChannelAmount;
        private static int EnvChannelMaxAmount = 100 * 500;

        int lastHitSoundTime;
        private static int hitSoundDamageDelay = 90;

        public static Snd Instance
        {
            get
            {
                if (instance == null) instance = new Snd();
                return instance;
            }
        }

        private Snd()
        {
            Debug.Assert(envSound != null);
            envSoundInstance = envSound.CreateInstance();
        }

        //Stop the sound from being played rapidly in succession.
        public void PlayHitSound(SoundEffect sound)
        {
            Debug.Assert(sound != null);
            if (Core.Instance.Tick - lastHitSoundTime < hitSoundDamageDelay)
            {
                return;
            }
            lastHitSoundTime = Core.Instance.Tick;
            sound.Play();
        }

        public void PlayerHitByBeam()
        {
            envChannelAmount = EnvChannelMaxAmount;
            envSoundInstance.Volume = 1.0f;
            if (envSoundInstance.State != SoundState.Playing)
            {
                envSoundInstance.Play();
            }
        }

        public void Update(int delta)
        {
            if (envChannelAmount > 0)
            {
                envSoundInstance.Volume = ((float)envChannelAmount / (float)EnvChannelMaxAmount);
                envChannelAmount -= delta;
                if (envChannelAmount <= 0)
                {
                    envSoundInstance.Stop();
                }
            }
        }

        /*If beamWeaponChannelActive > 0 Then
              If (beamWeaponChannelActive < beamWeaponChannelActiveTime - 1) Then
                  'only set volume while cooling down, because the cannon wants to control it at other times.
                  SetChannelVolume(playerBeamWeaponChannel,(1.0*beamWeaponChannelActive)/beamWeaponChannelActiveTime)
              EndIf
              beamWeaponChannelActive:- 1
          Else
            If (playerbeamWeaponChannel <> Null) Then
                  PauseChannel(playerbeamWeaponChannel) 'beam sound should stop
              EndIf
          EndIf       
      EndFunction*/


        /*       public void PlayPlayerDamage()
               {

               }

               public void PlayPlayerEnvSound()
               {
               }*/

        /*
        Type snd
    Const CHANNEL_COUNT:Int = 8
    Global fxChannel:TChannel[CHANNEL_COUNT]
    Global beamChannel:TChannel[1]
    Global currentBeamChannel:Int
    
    Global playerEnvChannel:TChannel
    Global EnvChannelActive:Int 'counts down when channel is inactive, to fade out the effect
    Global envChannelActiveTime:Int = 100 'time env channel stays active after stimulus stops.

    Global playerBeamWeaponChannel:TChannel
    Global beamWeaponChannelActive:Int 'counts down when channel is inactive, to fade out the effect
    Global beamWeaponChannelActiveTime:Int = 20 'cool down time.
    Global beamWeaponSound:TSound = LoadSound("resources\snd\Carpassing.wav", True) 'looping damage sound. Only one suppored for now. Hacky.
    
    Global musicChannel:TChannel
    Global musicSound:TSound = LoadSound("resources\snd\01 - BrunoXe - Angustia.ogg", True);
    
    Global currentChannel:Int
    
    Function playMusic()
      musicChannel = AllocChannel();
      PlaySound(musicSound,musicChannel)
    EndFunction
    
    Function playBeam(sound:TSound)     
        If sound <> Null Then
          PlaySound(sound,beamChannel[currentBeamChannel])
        Else
            DebugLog("Played null sound!")
        EndIf
        currentBeamChannel:+1
        If currentBeamChannel = beamChannel.dimensions()[0] Then currentBeamChannel = 0     
    EndFunction
        
    'If the sound is not playing, play it
    'volume is passed in, unless we're fading out.
    Function playPlayerBeamWeaponSound(volume:Float)
      If (beamWeaponChannelActive<=0) Then
            ResumeChannel(playerBeamWeaponChannel)
        EndIf
        If (beamWeaponChannelActive < beamWeaponChannelActiveTime) Then
            SetChannelVolume(playerBeamWeaponChannel,volume)
            beamWeaponChannelActive = beamWeaponChannelActiveTime
        EndIf
    EndFunction 

    'If the sound is not playing, play it
    'set it to max volume.
    Function playPlayerEnvSound()
      If (EnvChannelActive<=0) Then
            ResumeChannel(playerEnvChannel)
        EndIf
        If (envChannelActive < envChannelActiveTime) Then
            SetChannelVolume(playerEnvChannel,1.0)
            envChannelActive = envChannelActiveTime
        EndIf
    EndFunction 

EndType*/
    }

    public enum GameType { OnePlayer, TwoPlayer, ContinueSavedGame }

    public class Core
    {
        public Player P;
        public Player P2;
        public List<Enemy> AlienList;
        ParticleGroup[] ptArray = new ParticleGroup[ParticleGroup.PoolSize + 30]; //the +30 is kinda for no reason, to handle pool overflows, idontknow

        public ArtSet Art;

        int ptFirstEmpty;

        public Shot[] shotArray = new Shot[Shot.PoolSize];
        int shotFirstEmpty;

        //Shots about to be added to the shot array.
        public Shot[] pendingShots = new Shot[Shot.PoolSize];
        int pendingShotFirstEmpty;


        //experimental
        public Dictionary<int, List<Shot>> shotLookup = new Dictionary<int, List<Shot>>();

        public int ActiveParticleCounter; //for performance tuning only. FIXME probably should remove.

        //if there is a shot taller than this, there will be strange errors you can't figure out
        //used for collision check optimization
        const int maxShotHeight = 50;

        // bool hasQuit;
        bool hasLost;
        //Field window:WindowType

        int maxBeams; //not a constant!
        Beam[] beam = new Beam[0];

        public int Tick;
        int level;
        Special special;
        int lostTick = 0;
        int fpsAlertLevel; //0 OK, 1 LOW, 2 WORST, 3 EMERGENCY
        const int lowFPS = 55; //'below this, drop non-essential cleanup and reduce effect durations
        const int worstFPS = 50; //'below here: No new explosion effects 
        //decrease sfx duration even more
        //Note that the game will look ugly without explosion effects
        const int emergencyFPS = 30;
        int lowestEnemyCentreX = 0;
        double particleDrain;
        double defaultParticleDrain = 0.0003; //changes with low framerate.

        string trialModeString;
        int trialModeStringWidth;

        public GameType nextGameType = GameType.OnePlayer;
        private bool isTwoPlayer = false;

        public bool IsActive = false;

        static Core instance;

        public Random Random;

        public static Core Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Core();
                    //instance.Reset(); must manually be called.
                }
                return instance;
            }

        }

        public static bool IsThereAGameToContinue()
        {
            if (instance != null && instance.IsActive && !instance.InTrialLimbo) return !instance.HasLost();
            return SavedGame.Instance.IsAlive;
        }

        public static bool IsThereARunningGameToContinue()
        {
            if (instance != null && instance.IsActive && !instance.InTrialLimbo) return !instance.HasLost();
            return false;
        }

        List<Message> messageList;
        public PowerUp PowerUps; //linked list chain
        float PowerUpChance;

        private Core() //Singleton
        {
            Random = new Random();
            messageList = new List<Message>();
            AlienList = new List<Enemy>();
            Input = new Inputs();
            Art = ArtSet.grey;
        }

        //Duplicate code from HighScores, and similar to Message.draw
        private void drawString(string text, int x, int y, int xOff, int yOff, ScreenManager screenManager)
        {
            float fontScale = 1.0f;
            Vector2 textPos = new Vector2(x + xOff, y + yOff);
            screenManager.SpriteBatch.DrawString(Message.GameFont, text, textPos, Color.White, 0, new Vector2(0, Message.GameFont.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
        }

        //TODO: don't instantiate or null out anything on reset, just set isAlive to false for each thing.
        public void Reset()
        {
            IsActive = true;
            if (nextGameType == GameType.ContinueSavedGame && SavedGame.Instance.IsAlive)
            {
                level = SavedGame.Instance.Level;
                P = SavedGame.Instance.GetP1();
                P2 = SavedGame.Instance.GetP2();
                isTwoPlayer = (P2 != null);
                nextGameType = P2 == null ? GameType.OnePlayer : GameType.TwoPlayer;
            } else {
                level = 1;
                if (nextGameType == GameType.TwoPlayer)
                {
                    isTwoPlayer = true;
                    P = new Player(0);
                    P2 = new Player(1);
                }
                else
                {
                    isTwoPlayer = false;
                    P = new Player(0);
                    P2 = null;
                }
            }



            //All shots must be released to the pool.
            for (int i = 0; i < shotFirstEmpty; i++)
            {
                shotArray[i].IsAlive = false;
            }

            if (AlienList != null) AlienList.Clear();

            scores = null;

            //Note, we don't clear particle groups.



            if (messageList != null) messageList.Clear();
            PowerUps = null;
            maxBeams = 0;
            hasLost = false;
            Tick = 0; //unneccessary?
            special = null;
            lowestEnemyCentreX = 0; //unneccessary
            particleDrain = 0; //unneccessary
            lostTick = 0;
            lostMessage = false;
            scores = null;

            if (P2 != null)
            {
                P2.Position.X = GameWindow.Width / 4 * 1 - P2.Width / 2;
                P.Position.X = GameWindow.Width / 4 * 3 - P.Width / 2;
            }
            GenerateLevel();
            particleDrain = defaultParticleDrain;

            for (int i = 0; i < beam.Length; i++)
            {
                beam[i] = new Beam();
            }

        }

        private void SetLost()
        {
            hasLost = true;
            lostTick = Tick;
            SavedGame.Instance.Delete();
        }

        public bool HasLost()
        {
            return hasLost;
        }

        private void updateParticles(int delta, int xOff)
        {
            ActiveParticleCounter = 0;
            for (int i = 0; i < ptFirstEmpty; i++)
            {
                ParticleGroup pt = ptArray[i];
                if (pt.IsAlive) //should always be true.
                {
                    pt.Move(delta);
                }
                if (!pt.IsAlive)
                {
                    //Release the particle group and its particles
                    pt.ReleaseToPool();
                    //swap me out of the active set.
                    if (i == ptFirstEmpty - 1)
                    {
                        ptFirstEmpty--;
                    }
                    else
                    {
                        ptArray[i] = ptArray[ptFirstEmpty - 1];
                        Debug.Assert(ptArray[i] != null);
                        ptArray[ptFirstEmpty - 1] = pt;
                        ptFirstEmpty--;
                        i--; //this slot has a new entry so process it again.
                    }
                }
            }
        }

        //Variables that used to exist in the GameLoop method.
        int xOff;
        int yOff;


        bool cheatManyPowerUps;

        public void CheatManyPowerUps(bool val)
        {
            if (!Tweaking.isCheatsEnabled) return;
            cheatManyPowerUps = val;
            CreateMessage("extra powerups " + val, 0);
            cheatTimer = 10;
        }

        int resetDelay = 3000;
        bool lostMessage = false;

        int slowestFrameInThisLevel;

        private HighScores scores;

        public Inputs Input;


        private int enemyCount;
        public int EnemyCount { get { return enemyCount; }}

        private int cheatTimer;
        public void CheatSkipLevelTo(int levelSkip)
        {
            if (cheatTimer > 0) return;
            cheatLevelToSkipTo = (levelSkip) * 5;
            if (cheatLevelToSkipTo == 0) cheatLevelToSkipTo = 1;
            isLevelSkipRelative = false;
        }
        int cheatLevelToSkipTo = -1;
        bool isLevelSkipRelative = false;
        public void CheatSkipLevel(int amount)
        {
            if (cheatTimer > 0) return;
            cheatLevelToSkipTo = amount;
            isLevelSkipRelative = true;
        }

        bool hasDrawn = false; //FIXME: for performance info

        WeakReference gcTracker = new WeakReference(new object());

        public void Update(GameTime gameTime)
        {
            //TODO: improve this perf info code.
            if (!gcTracker.IsAlive)
            {
                Debug.WriteLine("A garbage collection occurred!");
                gcTracker = new WeakReference(new object());
            }

            //make XNA timing compatible with my old milliseconds based code.
            int newTick = (int)gameTime.TotalGameTime.TotalMilliseconds;
            //we ought to use gameTime.ElapsedGameTime for the delta.
            int delta = newTick - Tick;
            Tick = newTick;

            int trueDelta = delta;
            if (delta > 50) delta = 50;

            if (hasLost)
            {
                if ((!lostMessage) && Tick > lostTick + (resetDelay))
                {

                    scores = new HighScores();
                    if (P2 == null)
                    {
                        scores.AddScoreAndSave(new Score(P.Score, level, GetRank(level, P.Kills), "", true));
                    }
                    else
                    {
                        scores.AddScoreAndSave(new Score(P.Score, P2.Score, level, GetRank(level, P.Kills), "", true));
                    }

                    if (!Options.Instance.RabbleControls)
                    {
                        CreateMessage("Press Enter to restart", 6, 0);
                    }
                    lostMessage = true;
                }

                if (Tick > lostTick + resetDelay && Input.HitEnterThisFrame)
                {
                    Reset();
                }
                else if (Options.Instance.RabbleControls && Tick > lostTick + resetDelay * 4)
                {
                    //in Mouse Control mode you can't hit enter so let's just advance
                    Reset();
                }

            }

            if (gameTime.IsRunningSlowly)
            {
                Debug.WriteLine("Running slowly");
            }
            if (!hasDrawn)
            {
                Debug.WriteLine("Frame dropped! (" + ActiveParticleCounter + " particles)");
                if (Tweaking.ShowPerfStats) { CreateMessage("frame dropped - particles " + ActiveParticleCounter, -4); }
                return; //Experimental! slow down the game instead of dropping frames.
            }
            hasDrawn = false;

            if (P != null) P.Move(delta);
            if (P2 != null) P2.Move(delta);

            if (P != null && P2 != null)
            {
                if (!P.IsAlive && !P2.IsAlive && !hasLost)
                {
                    if (!Tweaking.lowTextMode)
                    {
                        CreateDeathMessage("You are both destroyed!");
                    }
                    SetLost();
                }
            }

            if (!P.isOnScreen())
            {
                if (P.IsAlive)
                {
                    P.IsAlive = false;
                    if (!hasLost && P2 == null)
                    {
                        if (!Tweaking.lowTextMode)
                        {
                            CreateDeathMessage("You are destroyed!");
                        }
                        SetLost();
                    }
                    if (P2 != null && P2.IsAlive && !Tweaking.lowTextMode)
                    {
                        CreateMessage("Player 1 is lost!", 0);
                    }
                }
            }

            if (P2 != null && !P2.isOnScreen())
            {
                if (P2.IsAlive)
                {
                    P2.IsAlive = false;
                    if (P.IsAlive && !Tweaking.lowTextMode)
                    {
                        CreateMessage("Player 2 is lost!", 0);
                    }
                }
            }

            if (scores != null)
            {
                scores.Move(delta);
            }

            if (P2 != null)
            {
                //two players, a constantly high reward rate
                PowerUpChance = 0.07f;
            }
            else
            {
                //one player - make rewards more likely when player has no PowerUps

                if (P.Upgrades.AnyBonus == 0 && PowerUps == null)
                {
                    PowerUpChance = 0.15f;
                }
                else
                {
                    PowerUpChance = 0.035f;
                }
            }

            //make rewards less likely on higher levels, to compensate for greater number of enemies.
            if (level > 15)
            {
                PowerUpChance *= .75f;
            }

            if (Tweaking.isCheatsEnabled)
            {
                if (cheatTimer > 0)
                {
                    cheatTimer--;
                }
                else
                {
                    if (cheatLevelToSkipTo > 0 || isLevelSkipRelative)
                    {
                        cheatTimer = 10;
                        AlienList.Clear();
                        if (isLevelSkipRelative)
                        {
                            level += cheatLevelToSkipTo - 1; //subtract one because the level will increment.
                        }
                        else
                        {
                            level = cheatLevelToSkipTo - 1; //because the level will increase by one as it begins.
                        }
                        if (level < 0) level = 0;
                        cheatLevelToSkipTo = -1;
                        isLevelSkipRelative = false; //allows -1 to act as a disabling flag arg i code bad.
                        special = null;
                    }
                    if (cheatManyPowerUps)
                    {
                        PowerUpChance = 0.5f;
                    }
                }
            }

            //combine shake for 2 player
            if (P2 != null)
            {
                P.Shake += P2.Shake;
                if (P.Shake > Player.MaxShake) P.Shake = Player.MaxShake;
                P2.Shake = 0;
            }

            //Visual effects
            if (P.Shake > 0)
            {
                P.Shake -= 0.3 * delta;
                if (P.Shake < 0)
                {
                    P.Shake = 0;
                }
            }

            if (special != null) special.Move(delta);

            for (int i = 0; i < beam.Length; i++)
            {
                if (beam[i] != null) //er, this should never be null though.
                {
                    beam[i].Move(delta, P, P2);
                }
            }

            if (PowerUps != null)
            {
                PowerUps.TestPickedUp(P, P2);
                PowerUps.Move(delta); //warning, PowerUps may become Null in this line.
            }


            //Enemies
            int newEnemyCount = 0;
            Enemy lowestEnemy = null;
            for (int i = 0; i < AlienList.Count; i++)
            {
                Enemy e = AlienList[i];
                if (!e.IsAlive)
                {
                    if (fpsAlertLevel == 0)
                    { //only remove enemies if performance is OK
                        AlienList.Remove(e);
                        i--; //nasty hack :/ that was not in the old version.
                    }
                }
                else
                {
                    newEnemyCount++;
                    e.Move(delta);

                    if (e.Position.Y > GameWindow.Height)
                    {
                        e.DieWithNoEffects();
                    }

                    if (lowestEnemy == null || e.Position.Y + e.Height > lowestEnemy.Position.Y + lowestEnemy.Height)
                    {
                        lowestEnemy = e;
                    }
                }

            }

            enemyCount = newEnemyCount;

            if (lowestEnemy != null)
            {
                lowestEnemyCentreX = lowestEnemy.centerX();
            }
            else
            {
                lowestEnemyCentreX = GameWindow.Width / 2;
            }

            //messages
            //setblend alphablend
            //FIXME 
            List<Message> messagesToRemove = new List<Message>();
            for (int i = 0; i < messageList.Count; i++)
            {
                Message m = messageList[i];
                if (!m.IsAlive)
                {
                    messageList.Remove(m);
                    i--;
                }
                else
                {
                    m.Move(delta);
                }
            }

            /*high scores If (scoresList<> Null) Then
            scoresList.move(delta)
            scoresList.Draw(0,0)
        EndIf*/

            if (enemyCount == 0 && !hasLost)
            {
                if (special == null || special.Finished())
                {
                    special = null;
                    ProgressLevel();
                }
            }
            UpdateShots(delta);

            //Particles
            //you must update the shots lookup table shots before doing particles
            updateParticles(delta, xOff);

            if (trueDelta > slowestFrameInThisLevel)
            {
                slowestFrameInThisLevel = trueDelta;
                Debug.WriteLine("New slowest frame for this level: " + (slowestFrameInThisLevel) + " ms");
            }

            /*if (fpsSlot == 0)
            {
                Debug.WriteLine("FPS: " + (1000f / averageFPS));
            }*/

            //fpsAlertLevel      particleDrain
            //3                 default * 20
            //2                 default * 9
            //1                 default * 3
            //0                 defaultParticleDrain

            //show FPS - also hidden.

            Snd.Instance.Update(delta);
        }

        private void UpdateShots(int delta)
        {
            //Add pending shots.
            for (int i = 0; i < pendingShotFirstEmpty; i++)
            {
                Shot s = pendingShots[i];
                Debug.Assert(s != null && s.IsAlive);
                shotArray[shotFirstEmpty++] = s;
                pendingShots[i] = null;
                if (shotFirstEmpty == shotArray.Length)
                {
                    Debug.WriteLine("Error: Too many shots.");
                    //There is no good way to deal with this. Let's just overwrite the last shot added.
                    //TODO: actually, holding the pending shots in the queue until later would be smart.
                    shotFirstEmpty--;
                }
            }
            pendingShotFirstEmpty = 0;

            //Update real shots
            if (Tweaking.AllowParticlePushing)
            {
                shotLookup.Clear();
            }

            for (int i = 0; i < shotFirstEmpty; i++)
            {
                Shot s = shotArray[i];
                if (!s.IsAlive && fpsAlertLevel == 0)
                {
                    Shot.Release(s);
                    shotArray[i] = null;
                    if (i != shotFirstEmpty - 1) 
                    {
                        //If there are shots after me in the list, move the last one up into my space.
                        Debug.Assert(shotArray[shotFirstEmpty - 1] != null);
                        shotArray[i] = shotArray[shotFirstEmpty - 1];
                        shotArray[shotFirstEmpty - 1] = null;
                        i--; //We must rerun the loop for this slot, since a new shot occupies this slot.    
                    }
                    shotFirstEmpty--; //either way, we removed the last element from the list.
                } 
                else if (s.IsAlive)
                {
                    s.move(delta);
                    if (s.IsAlive && Tweaking.AllowParticlePushing)
                    {
                        AddShotToShotMap(s);
                    }
                }

            }
        }

        private void AddShotToShotMap(Shot s)
        {
            int cell = PositionToIndex(s.Position);
            List<Shot> cellShots;
            if (!shotLookup.TryGetValue(cell, out cellShots))
            {
                cellShots = new List<Shot>();
                cellShots.Add(s);
                shotLookup.Add(cell, cellShots);
            }
            else
            {
                cellShots.Add(s);
                shotLookup.Remove(cell);
                shotLookup.Add(cell, cellShots);
                if (cellShots.Count > Shot.PoolSize)
                {
                    Debug.WriteLine("unusually large shotlookup bin with " + cellShots.Count);
                }
            }
        }

        //map coordinates to cells in the shot map dictionary lookup whatever i decide to use.
        public static int PositionToIndex(Vector2 pos)
        {
            //int xCell = (int)(pos.X * 2 / GameWindow.Width);
            int yCell = (int)(pos.Y / 32);
            //int cell = xCell + (2 * yCell);
            return yCell;
        }

        Color CrossColor = new Color(255, 255, 255, 100);

        public void Draw(GameTime gameTime, ScreenManager screenManager)
        {
            hasDrawn = true;
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Visual effects
            if (P.Shake > 0)
            {
                if (xOff == 0)
                {
                    xOff = 10;
                }
                else
                {
                    xOff = (Math.Min(Math.Abs(xOff), GameWindow.Width / 4) + Random.Next(1, 2)) * -Math.Sign(xOff);
                }
            }
            else
            {
                xOff = (Math.Min(Math.Abs(xOff), GameWindow.Width / 4) - Random.Next(1, 2)) * -Math.Sign(xOff);
            }

            screenManager.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle(0 + screenManager.screenX, 0 + screenManager.screenY, GameWindow.Width, GameWindow.Height), Color.Gray);

            if (PowerUps != null)
            {
                PowerUps.Draw(xOff, yOff, screenManager);
            }


            DrawThings(ptArray, xOff, yOff, screenManager);
            //DrawForeground(elapsedTime);
            //DrawHud();
            DrawThings(shotArray, xOff, yOff, screenManager);
            if (P2 != null) P2.Draw(xOff, yOff, screenManager);
            if (P != null) P.Draw(xOff, yOff, screenManager);
            DrawThings(beam, xOff, yOff, screenManager);
            foreach (Drawable d in AlienList)
            {
                if (d != null) d.Draw(xOff, yOff, screenManager);
            }
            DrawThings(messageList.ToArray(), xOff, yOff, screenManager);

            if (Tweaking.ShowPerfStats)
            {
                if (gameTime.IsRunningSlowly)
                {
                    screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle(0, 0, 45, 45), Color.RoyalBlue);
                }
                screenManager.SpriteBatch.DrawString(Message.GameFont, String.Format("Mem: {0:00000}KB", GC.GetTotalMemory(false) / 1024), new Vector2(10, 10), Color.DarkGray);
                screenManager.SpriteBatch.DrawString(Message.GameFont, "particles: " + ActiveParticleCounter, new Vector2(10, 50), Color.DarkGray);
            }

            if (scores != null)
            {
                scores.Draw(xOff, yOff, screenManager);
            }

            if (!Tweaking.lowTextMode)
            {
                if (P2 != null)
                {
                    //FIXME: uses memory every frame!
                    string msg1 = "Score: " + P.Score;
                    string msg2 = "Score: " + P2.Score;
                    drawString(msg2, 30, GameWindow.Height - 30, 0, 0, screenManager);
                    drawString(msg1, GameWindow.Width - 190, GameWindow.Height - 30, 0, 0, screenManager);

                }
                else
                {
                    //FIXME: uses memory every frame!
                    string msg1 = "Score: " + P.Score;
                    drawString(msg1, GameWindow.Width - 190, GameWindow.Height - 30, 0, 0, screenManager);
                }
            }

            if (Tweaking.isCheatsEnabled)
            {
                screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle(45 + screenManager.screenX, 0 + screenManager.screenY, 45, 20), Color.Coral);
            }

#if PROFILING
            screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle(45, 20, 45, 20), Color.AliceBlue);
#endif

            if (Version.IsTrialMode)
            {
                if (InTrialLimbo)
                {
                    //In limbo means we tried to advance to a new level, but we were out of trial levels. Instead we advance to an upsell screen.
                    Input.UpsellBuyButton = DrawUpsell(UPSELL_GET_FULL_VERSION, GameWindow.Height / 5 * 2, 1f, screenManager);
                    //DrawUpsell("MORE GREAT ADVENTURES", GameWindow.Height / 4 + 60, 1f, screenManager);
                    //DrawUpsell("1 OR 2 PLAYERS", GameWindow.Height / 4 + 120, 1f, screenManager);
                    Input.UpsellBackButton = DrawUpsell(UPSELL_BACK, GameWindow.Height / 5 * 2 + 100, 0.5f, screenManager);
                }
                screenManager.SpriteBatch.DrawString(Message.GameFont, trialModeString, new Vector2(GameWindow.Width / 2, 10), Color.White, 0f, new Vector2(trialModeStringWidth/2, 0), 0.5f, SpriteEffects.None, 0f);
            }

            screenManager.SpriteBatch.End();
        }

        private const string UPSELL_GET_FULL_VERSION = "GET THE FULL VERSION";
        private const string UPSELL_BACK = "BACK TO MAIN MENU";

        private Rectangle DrawUpsell(string message, int yPos, float scale, ScreenManager screenManager)
        {
            Vector2 textSize = Message.GameFont.MeasureString(message);
            int width = (int)(textSize.X);
            screenManager.SpriteBatch.DrawString(Message.GameFont, message, new Vector2(GameWindow.Width / 2 + 1, yPos + 1), Color.Black, 0f, new Vector2(width / 2, 0), scale, SpriteEffects.None, 0f);
            screenManager.SpriteBatch.DrawString(Message.GameFont, message, new Vector2(GameWindow.Width / 2, yPos), Color.White, 0f, new Vector2(width / 2, 0), scale, SpriteEffects.None, 0f);
            Rectangle button = new Rectangle((int)(GameWindow.Width / 2 - (textSize.X * scale / 2)), yPos, (int)(textSize.X * scale), (int)(textSize.Y * scale));
            if (Tweaking.DrawUpsellButtons) screenManager.SpriteBatch.Draw(Gfx.Pixel, button, Color.IndianRed);
            return button;
        }

        static Vector2 vectorZero = new Vector2();

        private void DrawAnchor(Texture2D image, Vector2 centrePos, ScreenManager screenManager, bool flipped)
        {
            int centreX = (int)centrePos.X;
            int centreY = (int)centrePos.Y;
            SpriteEffects effect;
            if (flipped)
            {
                effect = SpriteEffects.FlipHorizontally;
            }
            else
            {
                effect = SpriteEffects.None;
            }
            screenManager.SpriteBatch.Draw(image, new Rectangle(centreX - image.Width/2, centreY - image.Height / 2, image.Width, image.Height),
                null, CrossColor, 0f, vectorZero, effect, 0f);
        }

        private void DrawThings(IEnumerable<Drawable> things, int xOff, int yOff, ScreenManager screenManager)
        {
            foreach (Drawable d in things)
            {
                if (d != null) d.Draw(xOff, yOff, screenManager);
            }
        }

        //Increase the level counter, clean up, and then call GenerateLevel().
        //Currently, this method is not called for the first level.
        private void ProgressLevel()
        {
            if (Version.IsTrialMode)
            {
                TrialLimits.Instance.Decrement();
                if (TrialLimits.Instance.LevelsLeft <= 0)
                {
                    //Call UpdateTrialString here since we will never get to where it is normally called in GenerateLevel
                    //unfortunately it gets called every frame here.
                    UpdateTrialString();
                    InTrialLimbo = true;
                    return;
                }
            }
            level++;
            slowestFrameInThisLevel = 33; //reset bad frame detector
            AlienList.Clear(); //i don't think we remove aliens at any other time...
            GenerateLevel();
        }

        private class ShotComparer : IComparer<Shot>
        {
            public int Compare(Shot a, Shot b)
            {
                if (a == null)
                {
                    if (b == null) return 0;
                    return 1;
                }
                if (b == null) return -1;
                return (int)(a.Position.Y - b.Position.Y);
            }
        }

        ShotComparer shotComparer = new ShotComparer();

        private void SortShotArray(int lo, int hi)
        {
            Array.Sort(shotArray, shotComparer);
        }

        public Message CreateMessage(String text, int row)
        {
            return CreateMessage(text, row, 1.0f);
        }

        public Message CreateMessage(String text, int row, float alphaDrainMulti)
        {
            Debug.Assert(row <= 7);
            //Don't create overlapping messages (unless second has faded a bit)
            bool clear = false;
            while (!clear)
            {
                clear = true; //set to false later
                foreach (Message m in messageList)
                {
                    if (m.Row == row && m.a > 128)
                    {
                        clear = false;
                        row++;
                        break;
                    }
                }
            }
            Message message = new Message(text, row);
            messageList.Add(message);
            return message;
        }

        public void CreateKeyboardKeyMessage(Sprite sprite, int x, int y)
        {
            Message key = Message.CreateImageAt(sprite, x, y);
            messageList.Add(key);
        }

        private void CreateDeathMessage(String text)
        {
            CreateMessage(text, -3, 0.02f);
            CreateMessage("Rank: " + GetRank(), -2, 0.01f);
            CreateMessage("Level: " + level, -1, 0.01f);
        }

        public void AddShot(Shot s)
        {
            Debug.Assert(pendingShots[pendingShotFirstEmpty] == null);
            pendingShots[pendingShotFirstEmpty] = s;
            pendingShotFirstEmpty++;
            if (pendingShotFirstEmpty == pendingShots.Length)
            {
                pendingShotFirstEmpty--;
                DebugLog("Overload: adding too many shots in one frame!");
            }
        }

        private void DebugLog(String text)
        {
            Debug.WriteLine(text);
        }

        public void TrySpawnPowerUp(Drawable at)
        {
            if (Random.NextDouble() < PowerUpChance)
            {
                PowerUp pwr = new PowerUp();
                if (PowerUps == null)
                {
                    PowerUps = pwr;
                }
                else
                {
                    PowerUps.add(pwr);
                }
                pwr.Position.X = pwr.offCenterX(at.centerX());
                pwr.Position.Y = pwr.offCenterY(at.centerY());
            }
        }

        //FIXME

        public void CheatPowerUp()
        {
            if (!Tweaking.isCheatsEnabled) return;
            Array values = Enum.GetValues(typeof(PowerUp.PowerUpTypes));
            P.Upgrade((PowerUp.PowerUpTypes)values.GetValue(Random.Next(0, values.Length)));
        }

        public void CheatSad()
        {
            if (!Tweaking.isCheatsEnabled) return;
            Shot s = Shot.CreateMonkShot(P.centerX() - 5, 20, 1);
            AddShot(s);
        }

        public ParticleGroup CreateParticleGroup(int width, int height, ParticleType expType)
        {
            if (ptFirstEmpty == ptArray.Length - 1)
            {
                DebugLog("Overload - too many particle groups");
                return null;
            }
            else
            {
                ParticleGroup ptg = ParticleGroup.Create(expType);
                ptArray[ptFirstEmpty] = ptg;
                ptFirstEmpty++;
                return ptg;
            }
        }

        public Beam GetFreeBeam()
        {
            if (beam != null) //er, never happens
            {
                foreach (Beam b in beam)
                {
                    if (b != null && !b.IsAlive)
                    {
                        return b;
                    }
                }

            }
            return null;
        }

        private void UpdateTrialString()
        {
            if (!Version.IsTrialMode) return;
            if (TrialLimits.Instance.LevelsLeft == 1)
            {
                trialModeString = "FREE TRIAL - " + TrialLimits.Instance.LevelsLeft + " LEVEL REMAINING";
            }
            else
            {
                trialModeString = "FREE TRIAL - " + TrialLimits.Instance.LevelsLeft + " LEVELS REMAINING";
            }
            trialModeStringWidth = (int)Message.GameFont.MeasureString(trialModeString).X;
        }

        public bool InTrialLimbo;

        private void GenerateLevel()
        {
            SavedGame.Instance.Save(level, P, P2);

            if (Version.IsTrialMode)
            {
                InTrialLimbo = false; //we cannot be in limbo if we are in a proper level.
                UpdateTrialString();
            }

            int[] every = new int[11];
            for (int i = 2; i <= 10; i++)
            {
                every[i] = level % i;
            }

            if (!Tweaking.lowTextMode)
            {
                CreateMessage("Level " + level, 0);
            }

            switch (level)
            {
                    //introducing monks
                case 1:
                    ShowInstructions(P2 != null);
                    MakeOctoWave();
                    return;
                case 2:
                    MakeOctoWave();
                    return;
                case 3:
                    MakeOctoWave();
                    return;
                case 4:
                    MakeMonkWave();
                    return;
                case 5: makeSpecialStage(0);
                    return;

                    //introducing stars
                case 6: MakeOctoWave(); return;
                case 7: MakeMonkWave(); return;
                case 8: MakeOctoWave(); return;
                case 9: MakeFrogWave(); return;
                case 10: makeSpecialStage(1); return;

                //introducing snails (and multiple enemy types together)
                case 11: MakeOctoWave();
                    MakeFrogWave();
                    return;
                case 12: MakeMonkWave();
                    return;
                case 13: MakeMonkWave(); 
                    MakeFrogWave(); return;
                case 14: MakeSnailWave(); return;
                case 15: makeSpecialStage(2); return;

                //nothing left to introduce :( let's take a break from monks until the special stage
                case 16:
                    MakeOctoWave();
                    return;
                case 17:  
                    MakeOctoWave();
                    MakeFrogWave();
                    return;
                case 18:
                    MakeOctoWave();
                    MakeSnailWave();
                    return;
                case 19:
                    MakeOctoWave();
                    MakeFrogWave();
                    MakeSnailWave();
                    return;
            }

            if (every[5] == 0)
            {
                makeSpecialStage((level / 5) - 1);
                return;
            }

            //levels 20 to infinity
            //Specials override some of these, remember
            switch (every[7])
            {
                case 0:
                    MakeOctoWave(); //5
                    break;
                case 1:
                    MakeOctoWave();
                    MakeMonkWave(); //3
                    break;
                case 2:
                    MakeFrogWave(); //4
                    MakeSnailWave(); //4
                    break;
                case 3:
                    MakeOctoWave();
                    MakeMonkWave();
                    MakeFrogWave();
                    MakeSnailWave();
                    break;
                case 4:
                    MakeOctoWave();
                    MakeFrogWave();
                    break;
                case 5:
                    MakeFrogWave();
                    MakeMonkWave();
                    MakeSnailWave();
                    break;
                case 6:
                    MakeOctoWave();
                    MakeSnailWave();
                    break;
                default: Debug.WriteLine("Math error, science does not exist.");
                    throw new ArgumentException("Terrible Math Error: Unexpected value for every[7] which was " + every[7]);
            }

        }

        private void makeSpecialStage(int number)
        {
            special = new Special(number);
            if (special.Creatures == 1)
            {
                MakeMonkWave();
            }
        }

        private void ShowInstructions(bool isTwoPlayer)
        {
            if (Options.Instance.RabbleControls) return;

            if (isTwoPlayer)
            {
                int xOffset = Gfx.KeysP1.Width / 2;
                int yPos = GameWindow.Height / 10 * 5;
                CreateKeyboardKeyMessage(Gfx.KeysP2, GameWindow.Width / 4 - xOffset, yPos);
                CreateKeyboardKeyMessage(Gfx.KeysP1, GameWindow.Width / 4 * 3 - xOffset, yPos);

            }
            else
            {
                int xOffset = Gfx.KeysP1.Width / 2;
                int yPos = GameWindow.Height / 10 * 5;
                CreateKeyboardKeyMessage(Gfx.KeysP1, GameWindow.Width / 2 - xOffset, yPos);
            }
        }

        private void MakeSnailWave()
        {
            Snail s = new Snail();
            s.Position.X = 0 - s.Picture.Width;
            s.Position.Y = GameWindow.Height / 2;
            //m.SleepTime = i * 1500;
            AlienList.Add(s);

            Snail s2 = new Snail();
            s2.Position.X = GameWindow.Width;
            s2.Position.Y = GameWindow.Height / 2 - s2.Picture.Height * 3;
            //m.SleepTime = i * 1500;
            AlienList.Add(s2);

            if (level > 10)
            {
                Snail s3 = new Snail();
                s3.Position.X = 0 - s3.Picture.Width;
                s3.Position.Y = GameWindow.Height / 2 - s3.Picture.Height * 1;
                //m.SleepTime = i * 1500;
                AlienList.Add(s3);

                Snail s4 = new Snail();
                s4.Position.X = GameWindow.Width;
                s4.Position.Y = GameWindow.Height / 2 - s4.Picture.Height * 4;
                //m.SleepTime = i * 1500;
                AlienList.Add(s4);
            }
        }

        public void MakeMonkWave()
        {
            int numAliens;
            if (level < 9)
            {
                numAliens = 2 + level / 3;
            }
            else
            {
                numAliens = 2 + 9 / 3 + (level - 9) / 5; //so high levels don't take ages.
            }

            int ySpacing = 78; //FIXME: should be 1.2 * monkheight or something.

            for (int i = 0; i < numAliens; i++)
            {
                Monk m = new Monk();
                m.Position.X = (GameWindow.Width / 5) * Random.Next(1, 5) - m.Width / 2;
                m.Position.Y = -ySpacing;
                m.SleepTime = i * 1500;
                AlienList.Add(m);
            }
        }

        public void MakeOctoWave()
        {
            int numAliens;
            float alienSpeed;
            float baseAlienSpeed;
            float levelMulti = 0.01f;
            int topSquad;
            int rightSquad;

            baseAlienSpeed = Octo.DefaultSpeed;
            if (level < 4)
            {
                numAliens = 4 + level * 2;
            }
            else if (level < 10)
            {
                numAliens = 4 + 4 * 2 + (level - 4) * 1; //increase more slowly so high levels don't take ages.
            }
            else
            {
                numAliens = 4 + 4 * 2 + (10 - 4) * 1 + (level - 10) / 2;
            }

            alienSpeed = baseAlienSpeed + level * levelMulti;

            if (alienSpeed > baseAlienSpeed * 3)
            {
                alienSpeed = baseAlienSpeed * 3; //Maximum speed.
            }

            if (level == 1)
            {
                maxBeams = 0;
            }
            else
            {
                maxBeams = Math.Min(((level - 2) / 7) + 1, 4);
            }

            beam = new Beam[maxBeams];

            for (int i = 0; i < beam.Length; i++)
            {
                beam[i] = new Beam(); //not sure why this is here.
            }

            //Alien Deployment
            if (level <= 8)
            {
                topSquad = (numAliens / 2); //0 to 10 = 11
                rightSquad = (numAliens * 3 / 4); //11 to 15 = 5. heading right, um..
                //16 to 20 = 5
            }
            else
            { //we have reached maximums
                rightSquad = numAliens - 5; //lowest group will have 5 members.
                topSquad = numAliens - 11;  //middle group will have 7 members.
                //all the rest come from the roof.
            }

            int xSpacing = (int)(Core.Instance.Art.Octo.Width * 1.2);
            int ySpacing = Octo.RowHeight;

            for (int i = 0; i <= numAliens; i++)
            {
                Octo e = new Octo();
                e.Speed = alienSpeed;
                //enter from which direction?
                if (i < topSquad)
                {
                    e.Position.X = i * xSpacing + 10; //10 is a magic number to get the rows to converge nicely AFAIK
                    e.Position.Y = -ySpacing;
                    e.Row = -1;
                    e.Direction = -1;
                }
                else if (i <= rightSquad)
                {
                    e.Position.X = -e.Width - (i - topSquad) * xSpacing; //left side
                    e.Position.Y = ySpacing * 2;
                    e.Row = 2;
                    e.Direction = 1;
                }
                else
                {
                    e.Position.X = GameWindow.Width + (i - rightSquad) * xSpacing; //right side
                    e.Position.Y = ySpacing;
                    e.Row = 1;
                    e.Direction = -1;
                }
                AlienList.Add(e);
            }

        }

        public void MakeFrogWave()
        {
            int numAliens = Math.Min(4 + level / 3, 12);
            int waveDelayEachRound = 190;
            int waveDelay = 0;
            while (numAliens > 0)
            {
                for (int i = 1; i <= 4 && numAliens > 0; i++) //weird for loop because of blitzmax old ways. TODO: make start from zero...
                {
                    Frog a = new Frog(waveDelay);
                    a.Position.X = (GameWindow.Width / 5) * i - a.Width / 2;
                    int wave = numAliens / 8;
                    a.Position.Y = -a.Height - 5;
                    AlienList.Add(a);
                    numAliens--;
                }
                waveDelay += waveDelayEachRound;
            }
        }

        private String GetRank()
        {
            return GetRank(level, P.Kills);
        }

        private String GetRank(int lvl, int kills)
        {
            if (kills == 0) return "Pacifist";

            switch (lvl / 3)
            {
                case 0: return "Not Fit For Service";
                case 1: return "Conscript";
                case 2: return "Private";
                case 3: return "Ensign";
                case 4: return "Sub Lieutenant";
                case 5: return "Lieutenant";
                case 6: return "Lieutenant Commander";
                case 7: return "Commander";
                case 8: return "Captain";
                case 9: return "Commodore";
                case 10: return "Rear Admiral";
                case 11: return "Vice Admiral";
                case 12: return "Admiral";
                case 13: return "Fleet Admiral";
                case 14: return "Emperor";
                case 15: return "Emperor of Two Worlds";
                default: return "Emperor of Many Worlds";
            }
        }
    }

    public enum UpsellAction { NONE, BUY, BACK }

    public class Inputs
    {
        public bool HasFingerDown;
        public bool HitEnterThisFrame;

        public bool P1HasFingerDown;
        public bool P2HasFingerDown;

        //Hacks for the upsell screen
        public Rectangle UpsellBuyButton;
        public Rectangle UpsellBackButton;
        public UpsellAction UpsellAction;

        private void UpsellHacksPressed(Vector2 position)
        {
            if (Core.Instance.InTrialLimbo)
            {
                if (UpsellBuyButton != null && UpsellBuyButton.Contains((int)position.X, (int)position.Y))
                {
                    UpsellAction = UpsellAction.BUY;
                } 
                else if (UpsellBackButton != null && UpsellBackButton.Contains((int)position.X, (int)position.Y))
                {
                    UpsellAction = UpsellAction.BACK;
                }
            }
        }

        public void HandleInputs(InputState input, Player player, Player player2)
        {

                bool wasFingerDown = HasFingerDown;
                HasFingerDown = false;
                HitEnterThisFrame = false;
                P1HasFingerDown = false;
                P2HasFingerDown = false;

                UpsellAction = UpsellAction.NONE;

                KeyboardState keyState = Keyboard.GetState();

                if (keyState.IsKeyDown(Keys.Space) || keyState.IsKeyDown(Keys.Enter))
                {
                    HasFingerDown = true;
                }

                if (HasFingerDown && !wasFingerDown)
                {
                    HitEnterThisFrame = true;
                }

                player.left = false;
                player.right = false;
                player.up = false;
                player.down = false;

                if (player2 != null)
                {
                    player2.left = false;
                    player2.right = false;
                    player2.up = false;
                    player2.down = false;
                }


                //cheats can be enabled by holding the magic key combination
            if (keyState.IsKeyDown(Keys.Z) && keyState.IsKeyDown(Keys.L)) {
                Tweaking.isCheatsEnabled = true;
            } else if (Tweaking.isCheatsEnabled && keyState.IsKeyDown(Keys.L))
            {
                Tweaking.isCheatsEnabled = false;
            }

                //use keyboard for cheats
                if (keyState.IsKeyDown(Keys.D1)) Core.Instance.CheatSkipLevelTo(1);
                if (keyState.IsKeyDown(Keys.D2)) Core.Instance.CheatSkipLevelTo(2);
                if (keyState.IsKeyDown(Keys.D3)) Core.Instance.CheatSkipLevelTo(3);
                if (keyState.IsKeyDown(Keys.D4)) Core.Instance.CheatSkipLevelTo(4);
                if (keyState.IsKeyDown(Keys.D5)) Core.Instance.CheatSkipLevelTo(5);
                if (keyState.IsKeyDown(Keys.D6)) Core.Instance.CheatSkipLevelTo(6);
                if (keyState.IsKeyDown(Keys.D7)) Core.Instance.CheatSkipLevelTo(7);
                if (keyState.IsKeyDown(Keys.D8)) Core.Instance.CheatSkipLevelTo(8);
                if (keyState.IsKeyDown(Keys.D9)) Core.Instance.CheatSkipLevelTo(9);
                if (keyState.IsKeyDown(Keys.D0)) Core.Instance.CheatSkipLevelTo(0);
                if (keyState.IsKeyDown(Keys.Q)) Core.Instance.CheatSkipLevel(-1);
                if (keyState.IsKeyDown(Keys.E)) Core.Instance.CheatSkipLevel(1);
                if (keyState.IsKeyDown(Keys.O)) Core.Instance.CheatPowerUp();
                if (keyState.IsKeyDown(Keys.P)) Core.Instance.CheatSad();
                if (keyState.IsKeyDown(Keys.OemMinus)) Core.Instance.CheatManyPowerUps(true);
                if (keyState.IsKeyDown(Keys.OemPlus)) Core.Instance.CheatManyPowerUps(false);
                if (keyState.IsKeyDown(Keys.OemOpenBrackets)) Tweaking.ShowPerfStats = true;
                if (keyState.IsKeyDown(Keys.OemCloseBrackets)) Tweaking.ShowPerfStats = false;

                //Keyboard controls    
                if (keyState.IsKeyDown(Keys.Right))
                {
                    player.right = true;
                    player.left = false;
                }
                else if (keyState.IsKeyDown(Keys.Left))
                {
                    player.right = false;
                    player.left = true;
                }
                if (keyState.IsKeyDown(Keys.Up))
                {
                    player.up = true;
                    player.down = false;
                }
                else if (keyState.IsKeyDown(Keys.Down))
                {
                    player.up = false;
                    player.down = true;
                }

                if (player2 != null)
                {
                    if (keyState.IsKeyDown(Keys.D))
                    {
                        player2.right = true;
                        player2.left = false;
                    }
                    else if (keyState.IsKeyDown(Keys.A))
                    {
                        player2.right = false;
                        player2.left = true;
                    }
                    if (keyState.IsKeyDown(Keys.W))
                    {
                        player2.up = true;
                        player2.down = false;
                    }
                    else if (keyState.IsKeyDown(Keys.S))
                    {
                        player2.up = false;
                        player2.down = true;
                    }
                }
        }

        private float pickAbsoluteSmallest(float one, float two)
        {
            if (Math.Abs(one) < Math.Abs(two))
            {
                return one;
            }
            else
            {
                return two;
            }
        }

    }

    public struct Score
    {
        public int score;
        public int score2;
        public int level;
        public string rank;
        public string name;
        public bool isNew;
        private bool isTwoPlayer;

        public Score(int score, int level, string rank, string name, bool isNew)
        {
            this.score = score;
            this.score2 = 0;
            this.level = level;
            this.rank = rank;
            this.name = name;
            this.isNew = isNew;
            isTwoPlayer = false;
        }

        public Score(int score, int score2, int level, string rank, string name, bool isNew)
        {
            this.score = score;
            this.score2 = score2;
            this.level = level;
            this.rank = rank;
            this.name = name;
            this.isNew = isNew;
            isTwoPlayer = true;
        }
    }

    public class ScoreSet
    {
        public const int Size = 10;
        public List<Score> Scores = new List<Score>(Size);
        public string ScoreFile;

        public ScoreSet(string source)
        {
            ScoreFile = source;
            HighScoreData.Populate(this);
        }

        public void Save()
        {
            HighScoreData.SaveScores(this);
        }

        public void AddScore(Score score)
        {
            for (int i = 0; i < Scores.Count; i++)
            {
                Score s = Scores.ElementAt(i);
                if (s.level < score.level || (s.level == score.level && (s.score + s.score2) <= (score.score + score.score2)))
                {
                    Scores.Insert(i, score);
                    return;
                }
            }
            //add to the end of the list.
            Scores.Add(score);
        }

    }

    public static class HighScoreData
    {
        const int fileFormatVersion = 1;

        /// <summary>
        /// Saves the current highscore to a text file. The StorageDevice was selected during screen loading.
        /// </summary>
        public static void SaveScores(ScoreSet set)
        {
            Action<StreamWriter> handler = delegate(StreamWriter writer)
            {
                writer.WriteLine(fileFormatVersion);
                for (int i = 0; i < set.Scores.Count() && i < 5; i++)
                {
                    Score s = set.Scores[i];
                    writer.WriteLine(s.score.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    writer.WriteLine(s.score2.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    writer.WriteLine(s.level.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    writer.WriteLine(s.rank.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    writer.WriteLine(s.name.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            };

            Action<int> onError = delegate(int i) { Debug.WriteLine("Error while saving highscores."); };

            IOUtil.WriteFile(set.ScoreFile, handler, onError);

            if (Tweaking.DebugFileStuff) DebugFileStuff.DisplayFileContents(set.ScoreFile);
        }

        /// <summary>
        /// Loads the high score from a text file.  The StorageDevice was selected during the loading screen.
        /// </summary>
        public static void Populate(ScoreSet set)
        {
            Action<StreamReader> handler = delegate(StreamReader reader)
            {
                int version = ReadInt(reader);
                while (!reader.EndOfStream)
                {
                    int score1 = ReadInt(reader);
                    int score2 = ReadInt(reader);
                    int level = ReadInt(reader);
                    string rank = reader.ReadLine();
                    string name = reader.ReadLine();
                    Score score = new Score(score1, score2, level, rank, name, false);
                    set.AddScore(score);
                }
            };

            Action<int> onError = delegate(int i) {
                Debug.WriteLine("Error in high scores."); 
            };

            IOUtil.ReadFile(set.ScoreFile, handler, onError);
        }

        private static int ReadInt(StreamReader reader)
        {
            string s = reader.ReadLine();
            return Int32.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }

    }

    //Factory and implementation in one :/
    public class ArtSet
    {
        #region factory
        public static ArtSet grey;

        public static void LoadContent(ScreenManager screenManager)
        {
            Microsoft.Xna.Framework.Content.ContentManager c = screenManager.Game.Content;
            GraphicsDevice gd = screenManager.GraphicsDevice;
            if (grey == null)
            {
                grey = new ArtSet();
                grey.Octo = Gfx.Scale(c.Load<Texture2D>("gfx/octo"), Gfx.StandardScale, gd);
                grey.Monk = Gfx.Scale(c.Load<Texture2D>("gfx/monk"), Gfx.StandardScale, gd);
                grey.Player1 = Gfx.Scale(c.Load<Texture2D>("gfx/fighter"), Gfx.StandardScale, gd);
                grey.Player2 = Gfx.Scale(c.Load<Texture2D>("gfx/fighter2"), Gfx.StandardScale, gd);
                grey.Frog = Gfx.Scale(c.Load<Texture2D>("gfx/frog0"), Gfx.StandardScale, gd);
                grey.FrogLeap = Gfx.Scale(c.Load<Texture2D>("gfx/frog1"), Gfx.StandardScale, gd);
                grey.SnailLeft = Gfx.Scale(c.Load<Texture2D>("gfx/snail_left"), Gfx.StandardScale, gd);
                grey.SnailRight = Gfx.Scale(c.Load<Texture2D>("gfx/snail_right"), Gfx.StandardScale, gd);
                grey.SnailShell = Gfx.Scale(c.Load<Texture2D>("gfx/snail_shell"), Gfx.StandardScale, gd);

                grey.PlayerShotBase1 = Gfx.Scale(c.Load<Texture2D>("gfx/shot01"), Gfx.StandardScale, gd);
                grey.PlayerShotBase2 = Gfx.Scale(c.Load<Texture2D>("gfx/shot02"), Gfx.StandardScale, gd);
                grey.PlayerShotMega = Gfx.Scale(c.Load<Texture2D>("gfx/shotmega"), Gfx.StandardScale, gd);

                grey.ShotHeartPicture = Gfx.Scale(c.Load<Texture2D>("gfx/heart"), Gfx.StandardScale, gd);
                grey.ShotHeartBigPicture = Gfx.Scale(c.Load<Texture2D>("gfx/heartbig"), Gfx.StandardScale, gd);

                grey.MonkShot = Gfx.Scale(c.Load<Texture2D>("gfx/monkshot"), Gfx.StandardScale, gd);
                grey.FrogShotLeft = Gfx.Scale(c.Load<Texture2D>("gfx/frogshot"), Gfx.StandardScale, gd);
                grey.FrogShotRight = Gfx.Scale(c.Load<Texture2D>("gfx/frogshot1"), Gfx.StandardScale, gd);

                grey.Monk = Gfx.Scale(c.Load<Texture2D>("gfx/monk"), Gfx.StandardScale, gd);
                grey.MonkSpecialPicture[0] = Gfx.Scale(c.Load<Texture2D>("gfx/monk01"), Gfx.StandardScale, gd);
                grey.MonkSpecialPicture[1] = Gfx.Scale(c.Load<Texture2D>("gfx/monk10"), Gfx.StandardScale, gd);
                grey.MonkSpecialPicture[2] = Gfx.Scale(c.Load<Texture2D>("gfx/monk00"), Gfx.StandardScale, gd);
                
                //Every variation of the monk should use the explosion of the ammoless monk.
                //This is a a hack, but it _is_ a special case.
                grey.Monk.SetExplosionMapFrom(grey.MonkSpecialPicture[2].Texture);
                grey.MonkSpecialPicture[0].SetExplosionMapFrom(grey.MonkSpecialPicture[2].Texture);
                grey.MonkSpecialPicture[1].SetExplosionMapFrom(grey.MonkSpecialPicture[2].Texture);

                grey.Player1Upgrades = new PlayerUpgradesArt(screenManager, "");
                grey.Player2Upgrades = new PlayerUpgradesArt(screenManager, "2");
            }
        }

        #endregion

        public Sprite Frog;
        public Sprite FrogLeap;
        public Sprite PlayerShotBase1;
        public Sprite PlayerShotBase2;
        public Sprite PlayerShotMega;
        public Sprite ShotHeartPicture;
        public Sprite ShotHeartBigPicture;
        public Sprite MonkShot;
        public Sprite Octo;
        public Sprite Monk;
        public Sprite[] MonkSpecialPicture = new Sprite[3];

        public Sprite SnailLeft;
        public Sprite SnailRight;
        public Sprite SnailShell;

        public Sprite Player1;
        public Sprite Player2;

        public PlayerUpgradesArt Player1Upgrades;
        public PlayerUpgradesArt Player2Upgrades;

        private ArtSet()
        {
            //private constructor.
        }



        public Sprite FrogShotLeft { get; set; }
        public Sprite FrogShotRight { get; set; }
    }

    public static class Gfx
    {
        public static Texture2D Pixel;
        public static Sprite KeysP1;
        public static Sprite KeysP2;

        public const int StandardScale = 12;

        public static Sprite Scale(Texture2D source, int scale, GraphicsDevice graphicsDevice)
        {
            Texture2D scaled = ScaleTexture(source, scale, graphicsDevice);
            Sprite s = new Sprite(scaled, scaled);
            return s;
        }

        public static Texture2D ScaleTexture(Texture2D source, int scale, GraphicsDevice graphicsDevice)
        {
            if (scale == 1) return source;

            Color[] sourceColors = new Color[source.Width * source.Height];
            Color[] destColors = new Color[source.Width * scale * source.Height * scale];
            source.GetData<Color>(sourceColors);

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color color = sourceColors[x + y * source.Width];
                    //scale up our pixel
                    for (int x2 = 0; x2 < scale; x2++)
                    {
                        for (int y2 = 0; y2 < scale; y2++)
                        {
                            int outX = x * scale + x2;
                            int outY = y * scale + y2;
                            destColors[outY * (source.Width * scale) + outX] = color;
                        }
                    }
                }
            }

            Texture2D scaled = new Texture2D(graphicsDevice, source.Width * scale, source.Height * scale);
            scaled.SetData<Color>(destColors);
            return scaled;
        }

    }

    public static class Version
    {
        private static bool isTrialMode;
        public static bool IsTrialMode { get { return isTrialMode; } private set {isTrialMode = value;} }

        public static void UpdateFullVersionStatus()
        {
#if WINDOWS
            isTrialMode = false;
#else
            isTrialMode = Guide.IsTrialMode;
#endif
        }
    }

    public class TrialLimits
    {
        private const int LEVEL_LIMIT = 30;
        private const int TOP_UP_LEVEL_LIMIT = 4;
        private const string filename = "triallimit.txt";
        private const int FILE_VERSION = 1;

        private static TrialLimits instance;
        public static TrialLimits Instance
        {
            get
            {
                if (instance == null) instance = new TrialLimits();
                return instance;
            }
        }


        private int levelsLeft;
        public int LevelsLeft
        {
            get
            {
                return levelsLeft;
            }
            private set
            {
                levelsLeft = value;
            }
        }
        private TrialLimits()
        {
            if (!IOUtil.FileExists(filename))
            {
                levelsLeft = LEVEL_LIMIT;
                return;
            }

            Action<StreamReader> handler = delegate(StreamReader reader)
            {
                int version = IOUtil.ReadInt(reader);
                Debug.Assert(version == FILE_VERSION);
                LevelsLeft = IOUtil.ReadInt(reader);
            };

            Action<int> onError = delegate(int i)
            {
                Debug.WriteLine("Error in trial limits file.");
                levelsLeft = 0;
            };
        }

        public void Decrement()
        {
            //decrement and save
            if (levelsLeft > 0)
            {
                levelsLeft--;
                Save();
            }
        }

        public void TopUp()
        {
            if (levelsLeft < TOP_UP_LEVEL_LIMIT)
            {
                levelsLeft = TOP_UP_LEVEL_LIMIT;
                Save();
            }
        }

        private void Save()
        {
            Action<StreamWriter> handler = delegate(StreamWriter writer)
            {
                writer.WriteLine(FILE_VERSION);
                writer.WriteLine(levelsLeft);
            };
            IOUtil.WriteFile(filename, handler, delegate(int i) { Debug.WriteLine("Error while saving trial limits."); });
        }
    }

    public static class Assets
    {
        public static void LoadContent(ScreenManager ScreenManager)
        {
            Microsoft.Xna.Framework.Content.ContentManager c = ScreenManager.Game.Content;
            GraphicsDevice gd = ScreenManager.GraphicsDevice;

            Snd.music = c.Load<Song>("01-BrunoXe-Angustia");

            ArtSet.LoadContent(ScreenManager);

            Octo.RowHeight = (int)(ArtSet.grey.Octo.Height * 1.09);

            PowerUp.LoadImages(ScreenManager);

            Gfx.Pixel = Gfx.ScaleTexture(c.Load<Texture2D>("gfx/px"), Gfx.StandardScale, gd);

            Gfx.KeysP1 = new Sprite(c.Load<Texture2D>("gfx/keys_arrows"));
            Gfx.KeysP2 = new Sprite(c.Load<Texture2D>("gfx/keys_wasd"));

            Snd.PlayerShotSound = c.Load<SoundEffect>("snd/EugenSopot/menu_select22_edited");
            Snd.AlienDieSound = c.Load<SoundEffect>("snd/space sound jiggled");
            Snd.envSound = c.Load<SoundEffect>("snd/EugenSopot/new_radio9"); //was iSubmarineRunLoop 'looping damage sound
            Monk.DefaultShootSound = c.Load<SoundEffect>("snd/Gulping"); //was illegal\iBodyFall-002.wav
            Octo.DefaultShootSound = c.Load<SoundEffect>("snd/EugenSopot/new_radio13"); //iSpaceElevatorDown
            //FIXME:
            Snail.DefaultShootSound = Monk.DefaultShootSound;

            Player.DefaultPowerUpSound = c.Load<SoundEffect>("snd/EugenSopot/menu_select2"); //' was illegal\imodChainCatch-002.wav
            Player.DefaultHitSound = c.Load<SoundEffect>("snd/EugenSopot/explosion5"); //'was illegal\iBlowMusket-001.wav
            PlayerUpgrades.DefaultBeamSound = c.Load<SoundEffect>("snd/Carpassing");

            Message.GameFont = c.Load<SpriteFont>("fonts/leaguegothic");
            Particle.CreatePool();
            Shot.CreatePool();
            ParticleGroup.CreatePool();
        }
    }
}
