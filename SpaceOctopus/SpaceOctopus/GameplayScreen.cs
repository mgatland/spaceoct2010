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
using Microsoft.Xna.Framework.Input.Touch;
using System.Diagnostics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework.GamerServices;


namespace SpaceOctopus
{
    public class TouchAnchor
    {
        public Vector2 Position;
        public Vector2 MovePosition;
        public int Id;
        public TouchAnchor(Vector2 position, int id)
        {
            this.Position = position;
            this.MovePosition = position;
            this.Id = id;
        }
    }

    class GameplayScreen : GameScreen
    {

        // Level changes, nighttime transitions, etc
        float transitionFactor; // 0.0f == day, 1.0f == night
        float transitionRate; // > 0.0f == day to night

        //Screen dimension consts
        const float screenScaleFactor = 1.0f;
        const float screenHeight = 800.0f * screenScaleFactor; // Real screen is 800.0f x 480.0f
        const float screenWidth = 480.0f * screenScaleFactor;
        const int leftOffset = 25;
        const int topOffset = 50;
        const int bottomOffset = 20;

        Player player;
        Player player2;

        //input stuff that should move to Inputs
        AccelerometerReadingEventArgs accelState = null;
//        Accelerometer Accelerometer;

        //arg tight coupling
        MainMenuScreen menuScreen;

        public GameplayScreen(MainMenuScreen menuScreen)
        {
            Debug.Assert(Core.Instance.IsActive);
            this.menuScreen = menuScreen;
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.0);

  /*          Accelerometer = new Accelerometer();
            if (Accelerometer.State == SensorState.Ready)
            {
                Accelerometer.ReadingChanged += (s, e) =>
                {
                    accelState = e;

                };
                Accelerometer.Start();
            }*/
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
                Core.Instance.Input.HandleInputs(input, accelState, player, player2);
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
            //foreach (GameScreen screen in ScreenManager.GetScreens())
            //    screen.ExitScreen();

            if (menuScreen != null)
            {
                //Refresh the main menu's options
                menuScreen.CreateMenus();
            }
            this.ExitScreen();

            //ScreenManager.AddScreen(new BackgroundScreen());
            //ScreenManager.AddScreen(new MainMenuScreen());
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
            //float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
         //   ScreenManager.SpriteBatch.Begin();
         //   ScreenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle((int)(touchAnchor.X - 5), (int)(touchAnchor.Y - 5), 10, 10), Color.Aqua);
         //   ScreenManager.SpriteBatch.End();
        }

    }

    // Space Octopus (non tutorial) code follows

    public static class Window
    {
        public const int Width = 480; //300 in original SOM
        public const int Height = 800; //500 in original SOM
    }

    // Classes that won't be ported:

    /*Initialization code:
     * 
    Global core:CoreType = New CoreType 
    core.startUp() 
    
      and there's this from WindowType:
 
    Method SetWindow()
        AppTitle = "Space Octopus Mono"
        Graphics WIDTH, HEIGHT
        SetClsColor(128,128,128)
        SetBlend ALPHABLEND
        Local myfont:TImageFont=LoadImageFont("resources\fonts\DejaVuSans.ttf",15)
        SetImageFont myfont
        
    EndMethod

EndType
     
     */

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
            //a = (int)(alpha * 255f);
        }

        private void drawString(string text, int x, int y, int xOff, int yOff, ScreenManager screenManager)
        {
            float fontScale = 1.0f;
            Vector2 textPos = new Vector2(x + xOff, y + yOff);
            screenManager.SpriteBatch.DrawString(Message.GameFont, text, textPos, new Color(r, g, b, a), 0, new Vector2(0, Message.GameFont.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
        }

        //Note: ignores xOff and yOff.
        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            //base.Draw(xOff, yOff, screenManager);
            if (!IsAlive) return;
            //if (Core.Instance.isTwoPlayer ....

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
                    a = (byte)(highlightAlpha * 255f);
                }
                else
                {
                    a = 255;
                }
                //TODO: fix string creation here, it uses memory every frame.

                if (isTwoPlayer)
                {
                    drawString("Level: " + s.level, 40, yPos, xOff, yOff, screenManager);
                    drawString("Total score: " + (s.score + s.score2), Window.Width / 2- 30, yPos, xOff, yOff, screenManager);
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
            : base(null)
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

            Vector2 size = GameFont.MeasureString(text);
            size *= fontScale;
            Position.Y = Window.Height / 2 + (row * size.Y) - (size.Y / 2);
            Position.X = Window.Width / 2 - (size.X / 2);
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
            m.Position.Y = Window.Height / 2 + (row * size.Y) - (size.Y / 2);
            m.Position.X = yPos - (size.X / 2);
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
            else
            {
                //screenManager.SpriteBatch.DrawString(GameFont, text, Position, Color.Black, 0, new Vector2(0, GameFont.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
                screenManager.SpriteBatch.DrawString(GameFont, text, Position, new Color(r, g, b, a), 0, new Vector2(0, GameFont.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
            }
        }

        /*FIXME: image messagetype. Function createImg:messageType(fileName:String,x:Int,y:Int)
         Local p:messageType = New messageType
         p.bLive = True
         p.text = ""
         p.image = LoadImage(fileName)
         p.x = x - (p.image.width / 2)
         p.y = y
         p.alpha = 1
         p.alphadrain = 0.0002
         p.row = -99 'images don't use row. Won't be mistaken for a real value.
         Return p
     EndFunction*/
    }

    public class PowerUp : Drawable
    {
        float xV;
        float yV;

        public const int PowerUpTypesCount = 8; //sigh, remove this when i understand c#. Enum.GetValues()
        public const int DOUBLEFIRE = 0;
        public const int RAPIDFIRE = 1;
        public const int SPIKEWINGS = 2;
        public const int HEIGHTBOOST = 3;
        public const int MEGASHOT = 4;
        public const int CANNON = 5;
        public const int FASTMOVE = 6;
        public const int BUBBLE = 7;
        //no. public static enum PowerUpType { BUBBLE = 0, DOUBLEFIRE = 1, RAPIDFIRE = 2, SPIKEWINGS = 3, HEIGHTBOOST = 4, MEGASHOT = 5, CANNON = 6, FASTMOVE = 7};

        static int LastPowerType = -1;

        private const float DefaultFallSpeed = Window.Height / 7142f; //0.07f  

        PowerUp NextP;
        PowerUp PrevP;
        int type;

        static Texture2D DoubleImage;
        static Texture2D RapidImage;
        static Texture2D FastImage;
        static Texture2D SpikeImage;
        static Texture2D HeightImage;
        static Texture2D MegaImage;
        static Texture2D CannonImage;
        static Texture2D BubbleImage;

        public static void LoadImages(ScreenManager screenManager)
        {
            DoubleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrdouble"), 6, screenManager.GraphicsDevice);
            RapidImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrrapid"), 6, screenManager.GraphicsDevice);
            FastImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrfast"), 6, screenManager.GraphicsDevice);
            SpikeImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrspike"), 1, screenManager.GraphicsDevice);
            HeightImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrheight"), 6, screenManager.GraphicsDevice);
            MegaImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrmega"), 6, screenManager.GraphicsDevice);
            CannonImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrcannon"), 6, screenManager.GraphicsDevice);
            BubbleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/pwrbubble"), 6, screenManager.GraphicsDevice);
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

        public PowerUp() : this(-1) { }

        public PowerUp(int type)
            : base(null) //must call base with null texture for now because we won't know our texture until later :/
        {
            while (type < 0 || type >= PowerUpTypesCount)
            {
                type = Core.Instance.Random.Next(PowerUpTypesCount);
                if (type == SPIKEWINGS || type == HEIGHTBOOST) type = -1; //hacky disable spikewings and height boost.
            }

            //Set our picture, which we didn't do in base()
            switch (type)
            {
                case DOUBLEFIRE: Picture = DoubleImage; break;
                case RAPIDFIRE: Picture = RapidImage; break;
                case SPIKEWINGS: Picture = SpikeImage; break;
                case HEIGHTBOOST: Picture = HeightImage; break;
                case MEGASHOT: Picture = MegaImage; break;
                case FASTMOVE: Picture = FastImage; break;
                case CANNON: Picture = CannonImage; break;
                case BUBBLE: Picture = BubbleImage; break;
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

        public void TestPickedUp(Player p)
        {
            if (NextP != null) NextP.TestPickedUp(p);
            if (!IsAlive) return;
            if (testCollision(p))
            {
                IsAlive = false;
                p.Upgrade(type);
            }
        }

        public override void Draw(int xOff, int yOff, ScreenManager screenManager)
        {
            if (NextP != null) NextP.Draw(xOff, yOff, screenManager);
            base.Draw(xOff, yOff, screenManager);
        }
    }

    public abstract class Drawable
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
        public Texture2D Picture;
        private Color[] ExplosionMap; //FIXME: Must be static or shared! at the moment there's one per enemy.
        private int expWidth; //dimensions of the explosion map.
        private int expHeight;

        public Drawable(Texture2D picture)
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
                screenManager.SpriteBatch.Draw(Picture, new Rectangle((int)Position.X + xOff, (int)Position.Y + yOff, (int)Width, (int)Height), color);
            }
            else
            {
                //Draw a rectangle in the specified color.
                screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle((int)Position.X + xOff, (int)Position.Y + yOff, (int)Width, (int)Height), color);
                //screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle((int)Position.X + xOff, (int)Position.Y + yOff, (int)Width, (int)Height), Color.White);
            }
        }

        public void ClearExplosionMap()
        {
            ExplosionMap = null;
            expWidth = 0;
            expHeight = 0;
        }

        public void GetExplosionMap(Texture2D texture)
        {
            if (ExplosionMap != null) return;
            ExplosionMap = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(ExplosionMap);
            expWidth = texture.Width;
            expHeight = texture.Height;
        }

        public void GetExplosionMap()
        {
            GetExplosionMap(Picture);
        }

        #region HelperMethods

        public bool isOnScreen()
        {
            if (Position.X + Width < 0 || Position.X > Window.Width) return false;
            if (Position.Y + Height < 0 || Position.Y > Window.Height) return false;
            return true;
        }

        public bool isFullyOnScreen()
        {
            if (Position.X < 0 || Position.X + Width > Window.Width) return false;
            if (Position.Y < 0 || Position.Y + Height > Window.Height) return false;
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

        public void MakeIntoParticles(ParticleType expType, int xOff, int yOff)
        {
            Debug.Assert(ExplosionMap != null);

            ParticleGroup pGroup = Core.Instance.CreateParticleGroup(expWidth, expHeight, expType);
            if (pGroup == null) return; //must have run out of particle groups.
            int pos = 0;
            for (int y = 0; y < expHeight; y++)
            {
                for (int x = 0; x < expWidth; x++)
                {
                    //Debug.WriteLine(ExplosionMap[pos].B);
                    Color color = ExplosionMap[pos];
                    if (color.A > 64 && y % Tweaking.particleSize == 0 && x % Tweaking.particleSize == 0)
                    {
                        pGroup.AddParticle((int)(Position.X + x + xOff), (int)(Position.Y + y + yOff), color, expType, (float)x / (float)expWidth, (float)y / (float)expHeight);
                    }
                    pos++;
                }
            }
        }

        public void MakeIntoParticles(ParticleType expType)
        {
            MakeIntoParticles(expType, 0, 0);
        }

    }
    public class Creature : Drawable
    {
        public float Speed;
        public int Direction; //-1 is lrft, 1 is right, 2 is down\special
        public int RefireTimer;
        public int ROF; //Rate Of Fire
        public SoundEffect ShootSound;
        public ArtSet Art;
        public Creature(Texture2D texture)
            : base(texture)
        {
        }

    }

    public abstract class Enemy : Creature
    {
        //EnemySet Set;
        public SoundEffect DieSound;
        public int Points;

        public Enemy(Texture2D texture, int defaultROF, float speed)
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
            GetExplosionMap();
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
                if (s != null)
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

        public void Die(ParticleType expType)
        {
            IsAlive = false;
            Core.Instance.TrySpawnPowerUp(this);
            Explode(expType);
        }

        public virtual void Explode(ParticleType expType)
        {
            DieSound.Play();
            // If coreType.getInstance().fpsAlertLevel<2 Then return 'no explosion effects if framerate is low  
            MakeIntoParticles(expType);
        }
    }

    #region enemies


    class Star : Enemy
    {
        float xV;
        float yV;
        float friction = 0.998f;
        int stillTime;
        static int defaultMaxStillTime = 120;
        public int MaxStillTime;
        static int maxJumpDist = Window.Width;
        static int defaultROF = 6000;
        static float defaultSpeed = 0.1f;

        public Star()
            : base(Core.Instance.Art.Star, defaultROF, defaultSpeed)
        {
            Art = Core.Instance.Art;
            //shootSound = n/a stars don't shoot
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
            xV *= (float)Math.Pow(friction, delta);
            yV *= (float)Math.Pow(friction, delta);
            Position.X += xV * delta;
            Position.Y += yV * delta;
            if (Math.Abs(xV) < 0.01) xV = 0;
            if (Math.Abs(yV) < 0.01) yV = 0;
            //Local fearDirection:Float = starsAvoidShots()
            if (xV == 0 && yV == 0)
            {
                stillTime++; //FIXME: should be framerate dependent.
            }

            if (stillTime > defaultMaxStillTime)
            { // if we can dodge, also move on (fearDirection != 0) but don't decrease MaxStillTime in that case.
                MaxStillTime = (int)(MaxStillTime * 0.9f);
                stillTime = 0;
                //choose X destination
                int destX = Core.Instance.Random.Next(0, Window.Width);
                /*'override random movement if we're fleeing.
                         'if (fearDirection > 0) destX = windowType.WIDTH - 30
                         'if (fearDirection < 0) destX = 0 + 30
                         'x = fearDirection + (windowType.width / 2)*/
                //make sure destination is in range.
                destX = (int)Math.Min(destX, Position.X + maxJumpDist / 2);
                destX = (int)Math.Max(destX, Position.X - maxJumpDist / 2);

                //calculate Y coordinate using trig, to get a destinationpoint that is maxJumpDist away.
                //c^2 = a^2 - b^2
                int c = (int)Math.Sqrt(maxJumpDist * maxJumpDist - Math.Abs(Position.X - (destX * destX)));
                int destY = (int)(c + Position.Y);
                xV = (destX - Position.X) * 10 / maxJumpDist / 18;
                yV = (destY - Position.Y) * 10 / maxJumpDist / 18;


            }
            DoCollisions();
        }

    }
    /*Rem Method starsAvoidShots:Float()
       'get nearest threat. Move to avoid it 
    Method starsAvoidShots:Float()
                Local core:coreType = coreType.getInstance()
                Local s:shotType
                Local danger:Int
                Local leftHelp:Int = 0
                Local rightHelp:Int = 0
                Local fearDirection:Float = 0
                For Local i:Int = 0 To core.shotFirstEmpty - 1 Step 1
                    s=core.shotArray[i]
                    If s.bLive = True And s.hurtsEnemy > 0 Then
                        If s.x + s.width + 30 > x And s.x < x + width + 30 Then  '30s are a 'safety margin'
                            If s.speed < 0 And s.y > y Then 'below us, coming up
                              Local timeLeft%
                                timeLeft= (y - (s.y + s.height)) / s.speed 'ticks until collision.
                                If timeLeft < 900 Then
                                    DebugLog("starfish: look out, a shot! - distance " + timeLeft)                           
                                  If timeLeft < 1 Then timeLeft = 1
                                    danger = 900.0 / timeLeft
                                    Local dist:Int = centreX() - s.centreX()
                                    'stuck in a corner?
                                    If s.x < width + 5 And dist < 0 Then dist = -dist '5 is a safety margin
                                    If s.x + s.width > windowType.width - (width + 5) And dist > 0 Then dist = -dist '5 is a safety margin
                                    fearDirection = fearDirection + danger/dist
                                EndIf                           
                            EndIf
                      EndIf
                    EndIf
                Next
                Return fearDirection
    EndMethod  
    EndRem  
    
    EndType*/


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

 //       public static Texture2D MonkPicture;
 //      public static Texture2D[] SpecialPicture = new Texture2D[3];
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
            //slight performance waste - we get the explosion map twice for monk, once in Enemy() but it's wrong, then once in Monk()
            ClearExplosionMap();
            GetExplosionMap(Art.MonkSpecialPicture[2]);
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
            //TODO: set these offsets correctly, make it flexible to image size changes.
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
                float distance = ((Window.Width / 2) - (Position.X + Width / 2)) / Window.Width;
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
    #endregion

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
        bool hasSpike;
        bool hasHeight;
        bool hasMega;
        bool hasSpeed;
        public bool hasCannon; //public due to haxx
        bool hasBubble;

        public int AnyBonus; //number of bonuses we have.

        static Texture2D DoubleImage;
        static Texture2D RapidImage;
        static Texture2D FastImage;
        static Texture2D SpikeImage;
        static Texture2D HeightImage;
        static Texture2D MegaImage;
        static Texture2D CannonImage;
        static Texture2D BubbleImage;

        public static SoundEffect DefaultBeamSound;
        private SoundEffectInstance BeamSoundInstance;


        public PlayerUpgrades(Player p)
        {
            Debug.Assert(DefaultBeamSound != null);
            this.p = p;
            this.BeamSoundInstance = DefaultBeamSound.CreateInstance();
            BeamSoundInstance.IsLooped = true;
        }

        #region serialization

        public void LoadFromPlayerData(PlayerData pd)
        {
            PlayerUpgrades u = p.Upgrades;
            u.SilentUpgrade(PowerUp.BUBBLE, pd.Bubbles);
            u.SilentUpgrade(PowerUp.CANNON, pd.Cannon);
            u.SilentUpgrade(PowerUp.DOUBLEFIRE, pd.DoubleFire);
            u.SilentUpgrade(PowerUp.FASTMOVE, pd.FastMove);
            u.SilentUpgrade(PowerUp.HEIGHTBOOST, pd.Height);
            u.SilentUpgrade(PowerUp.MEGASHOT, pd.Mega);
            u.SilentUpgrade(PowerUp.RAPIDFIRE, pd.Rapid);
            u.SilentUpgrade(PowerUp.SPIKEWINGS, pd.SpikeWings);
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
            pd.SpikeWings = hasSpike ? 1 : 0;

            //special handling for multi-part upgrades:
            //only doublefire (triplefire) is supported, others will be discarded.
            if (hasDouble && p.ShotCount == 3)
            {
                pd.DoubleFire = 2; 
            }
        }

        #endregion

        private void SilentUpgrade(int type, int amount)
        {
            if (amount <= 0) return;
            for (int i = 0; i < amount; i++)
            {
                Upgrade(type, true);
            }
        }

        public static void LoadImages(ScreenManager screenManager)
        {
            DoubleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partdouble"), Gfx.StandardScale, screenManager.GraphicsDevice);
            RapidImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partrapid"), Gfx.StandardScale, screenManager.GraphicsDevice);
            FastImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partfast"), Gfx.StandardScale, screenManager.GraphicsDevice);
            SpikeImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partspikes"), 1, screenManager.GraphicsDevice);
            HeightImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partheight"), Gfx.StandardScale, screenManager.GraphicsDevice);
            MegaImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partmega"), Gfx.StandardScale, screenManager.GraphicsDevice);
            CannonImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partcannon"), Gfx.StandardScale, screenManager.GraphicsDevice);
            BubbleImage = Gfx.Scale(screenManager.Game.Content.Load<Texture2D>("gfx/partbubble"), Gfx.StandardScale, screenManager.GraphicsDevice);
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

        private void drawImage(Texture2D image, int x, int y, ScreenManager screenManager)
        {
            int xBack = (image.Width - p.Picture.Width) / 2;
            int yBack = (image.Height - p.Picture.Height) / 2;
            screenManager.SpriteBatch.Draw(image, new Rectangle(x - xBack, y - yBack, (int)(image.Width), (int)(image.Height)), Color.White);
        }

        private Color beamColor = new Color(255, 255, 255, 0);
        private void drawCannon(int x, int y, ScreenManager screenManager)
        {
            beamColor.A = Math.Min((byte)255, (byte)(254 * 2 * cannonWarmth / cannonWarmthMax));
            screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle(x, 0, p.Width, y), beamColor);

        }


        //classic draworder: under: cannon, double, rapid, spike, height, mega, cannon
        // over: speed, bubble

        public void DrawUnder(int xOff, int yOff, ScreenManager screenManager)
        {
            if (hasCannon && cannonWarmth > 0)
            {
                drawCannon((int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasSpike)
            {
                //add spikexoff, spikeyoff
                drawImage(SpikeImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

        }


        public void DrawOver(int xOff, int yOff, ScreenManager screenManager)
        {
            if (hasDouble)
            {
                drawImage(DoubleImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasCannon)
            {
                drawImage(CannonImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasSpeed)
            {
                drawImage(FastImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasHeight)
            {
                drawImage(HeightImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasRapid)
            {
                drawImage(RapidImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasMega)
            {
                drawImage(MegaImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }

            if (hasBubble)
            {
                drawImage(BubbleImage, (int)p.Position.X + xOff, (int)p.Position.Y + yOff, screenManager);
            }
        }

        int[] pwrUpXOff = new int[PowerUp.PowerUpTypesCount];
        int[] pwrUpYOff = new int[PowerUp.PowerUpTypesCount];

        /*Const spikeXOff:Int = -19       Const spikeYOff:Int = 16*/

        const int bubbleXOff = -26;
        const int bubbleYOff = -20;

        public void Move(int delta)
        {
            if (hasSpike)
            {
                Debug.WriteLine("spike upgrade not supported");
                //movespikes
            }

            if (hasBubble && bubbleHealth < bubbleHealthMax - 1) //the minus one is for int to double rounding errors (in BlitzMax, may be obselete now)
            {
                bubbleTime -= delta; //damaged bubbles decay.
                if (bubbleTime <= 0)
                {
                    bubbleHealth = 0;
                }
                if (bubbleHealth <= 0)
                {
                    ExplodeComponent(true, BubbleImage);
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

        public void Upgrade(int type)
        {
            Upgrade(type, true);
        }

        private void Upgrade(int type, bool showMessage)
        {
            String text = "--";
            switch (type)
            {
                case PowerUp.DOUBLEFIRE:
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
                case PowerUp.RAPIDFIRE:
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
                case PowerUp.FASTMOVE:
                    float maxSpeed = (float)(1.5 * p.DefaultSpeed);
                    if (p.Speed < maxSpeed - 0.01)
                    {
                        p.Speed += boostSpeed;
                        hasSpeed = true;
                        AnyBonus++;
                        if (boostSpeed > maxSpeed)
                        {
                            p.Speed = maxSpeed;
                            text = "Maximum speed";
                        }
                        else
                        {
                            text = "Speed increased";
                        }
                    }
                    else
                    {
                        text = "Upgrade not needed";
                    }
                    break;
                case PowerUp.SPIKEWINGS:
                    text = "FAIL FAIL DON'T USE SPIKEWINGS";
                    break;
                case PowerUp.HEIGHTBOOST:
                    if (!hasHeight)
                    {
                        hasHeight = true;
                        AnyBonus++;
                        p.DesiredY += boostDesiredY;
                        if (p.DesiredY < Player.DefaultDesiredY + boostDesiredY)
                        {
                            p.DesiredY = Player.DefaultDesiredY + boostDesiredY;
                        }
                        text = "Height increase";
                    }
                    else
                    {
                        text = "Upgrade not needed";
                    }
                    break;
                case PowerUp.MEGASHOT:
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
                case PowerUp.CANNON:
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
                case PowerUp.BUBBLE:
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
                default:
                    text = "Unrecognised upgrade " + type;
                    Debug.WriteLine("Unrecognised upgrade " + type);
                    break;
            }
            if (showMessage) Core.Instance.CreateMessage(text, 2, 2);
        }

        /*Case powerUp.SPIKEWINGS
         If (bSpike = False) Then
         bSpike = True
             text = "Projectile Repellers"
         Else
             text = "Upgrade not needed"
         EndIf*/

        public void LoseUpgrades()
        {
            p.resetAbilities();
            //megaShots = 0
            cannonTime = 0;
            cannonWarmth = 0;
            bubbleHealth = 0; //unneccessary
            bubbleTime = 0; //unneccessary

            ExplodeComponent(hasDouble, DoubleImage);
            ExplodeComponent(hasRapid, RapidImage);
            //spike
            ExplodeComponent(hasHeight, HeightImage);
            ExplodeComponent(hasMega, MegaImage);
            ExplodeComponent(hasSpeed, FastImage);
            ExplodeComponent(hasCannon, CannonImage);
            ExplodeComponent(hasBubble, BubbleImage);

            hasDouble = false;
            hasRapid = false;
            //hasSpike = false;
            hasHeight = false;
            hasMega = false;
            hasSpeed = false;
            hasCannon = false;
            hasBubble = false;
            AnyBonus = 0;
        }

        private void ExplodeComponent(bool hasComponent, Texture2D texture)
        {
            if (hasComponent)
            {
                //Haxx, relies on the fact that the player never explodes so we can abuse its explosionmap.
                p.ClearExplosionMap();
                p.GetExplosionMap(texture);
                p.MakeIntoParticles(ParticleType.SCATTER, p.Width / 2 - texture.Width / 2, p.Height / 2 - texture.Height / 2);
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
        double YSpeed = (Window.Height / 50000d);
        public double DefaultSpeed;

        double Shield = 1;
        double SuperShield = 0.7;
        int SuperY;
        const int DeathY = Window.Height;
        bool isAI;
        float ShotSpeedMulti = 1;

        public bool left;
        public bool right;
        public bool up;
        public bool down;

        int Id; //for 2 player

        //  Field soundShoot:TSound
        // Field powerUpSound:TSound
        //   Field hitSound:TSound

        //powerup stuff
        const int DefaultShotCount = 1;
        public const int DefaultROF = 600;
        const float DefaultShotSpeedMulti = 1.0f;
        public const int DefaultDesiredY = DeathY - 200;

        public readonly PlayerUpgrades Upgrades;

        private const int BaseDamageDistanceFactor = (int)(((float)DeathY - (float)DefaultDesiredY) / 100f);

        public int DesiredY;

        public int ShotCount;
        int CurrentShot;

        public void Upgrade(int type)
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
            : base(Core.Instance.Art.Player1)
        {
            Art = Core.Instance.Art;
            Position.X = Window.Width / 2 - Width / 2;
            Position.Y = DefaultDesiredY;
            Id = id;
            //set keys.

            //Base creation (common to all players)

            Upgrades = new PlayerUpgrades(this);

            HitSound = DefaultHitSound;
            PowerUpSound = DefaultPowerUpSound;
            ShootSound = Snd.PlayerShotSound;

            IsAlive = true;
            SuperY = DeathY - (int)Height;

            if (Id == 0)
            {
                r = 255;
                g = 255;
                b = 255;
            }
            else
            {
                r = 200;
                g = 200;
                b = 200;
            }

            isAI = false;
            resetAbilities();
        }

        public void resetAbilities()
        {
            DefaultSpeed = 0.2f;
            Speed = (float)DefaultSpeed; //    'horizonal
            ROF = DefaultROF;
            ShotSpeedMulti = DefaultShotSpeedMulti;
            ShotCount = DefaultShotCount;
            Upgrades.MegaShots = 0;
            DesiredY = DefaultDesiredY;
        }

        /*Function create2:playerType()
                Local p:playerType = New playerType         
                p.image = LoadImage("resources\gfx\fighter2.png")           
                baseCreate(p)
                p.x = windowType.width / 2 - p.width / 2
                p.y = 400           
                p.leftKey = KEY_A
                p.rightKey = KEY_D
                p.fireKey = KEY_W
                p.id = 1            
                Return p
            EndFunction*/

        private void PushDown(float impact, double shakeImpact, bool loseUpgrades)
        {
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
            if (Shake > 200) Shake = 300;

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

            PowerUp powerUp = core.PowerUps;
            if (powerUp != null) powerUp.TestPickedUp(this);

        }

        /*collide with enemies, if we have spikes     
'       If bSpike Then
'       Local x:Int = x + spikeXOff          - 10 'extra wide enemy killing
'       Local width:Int = spikeImage.width   + 20
'       Local y:Int = y + spikeYOff
'       Local height:Int = spikeImage.height        
'         For Local a:enemyType = EachIn coreType.getInstance().alienList
'               If a.bLive = True And a.x + a.width > x And a.x < x + width And  a.y + a.height > y And a.y < y + height Then
'                   a.die(particleType.SMALLSCATTER)
'                 kills:+1
'               EndIf
'           Next
'       EndIf   */

        public override void Move(int delta)
        {
            //  if (!isOnScreen())
            //  {
            //      IsAlive = false;
            //  } no, this check is done in the Core. FIXME...

            if (!IsAlive) return;
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
                    if (down && Position.Y + Height < Window.Height)
                    {
                        Position.Y += Speed * delta;
                        if (Position.Y + Height > Window.Height) Position.Y = Window.Height - Height;
                    }
                    if (Position.Y < 0) Position.Y = 0;
                }

                if (Position.X < 0) Position.X = 0;
                if (Position.X + Width > Window.Width) Position.X = Window.Width - Width;
                if (Core.Instance.EnemyCount > 0 || Upgrades.hasCannon || Core.Instance.InTrialLimbo) //the player is firing
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

        /*'spikes powerup: pushes enemy shots away from player
        Method moveSpikes(delta:Int)
            For Local i:Int = 0 To coreType.getInstance().maxShots - 1
                Local s:shotType = coreType.getInstance().shotArray[i]
                If (s <> Null And s.bLive And s.y + s.height > 0 And s.hurtsPlayer > 0) Then
                 'NO that sucked. Previously -> only push if it's on one side of us - straight above will still come straight down.
                    If s.x + s.width/2 > x + width/2 Then
                        s.xSpeed:+0.0004
                    ElseIf s.x + s.width/2 < x + width/2 Then
                      s.xSpeed:-0.0004
                    EndIf
                EndIf
            Next 'i
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
    }

    public enum ParticleType { SCATTER, FAST_SCATTER, FLASH, STILL };

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
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isf.FileExists(filename))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(filename, FileMode.Open, isf))
                    {
                        using (StreamReader reader = new StreamReader(isfs))
                        {
                            try
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
                                    P2 =  PlayerData.Deserialize(reader);
                                    isAlive = true;
                                }
                            }
                            catch (Exception)
                            {
                                Debug.WriteLine("Error in saved game file.");
                                Debug.WriteLine(reader.ReadToEnd());
                                isAlive = false;
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                    }
                }
            }
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
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(filename, FileMode.Create, isf))
                    {
                        using (StreamWriter writer = new StreamWriter(isfs))
                        {
                            writer.WriteLine(version);
                            writer.WriteLine(level);
                            P.Serialize(writer);
                            P2.Serialize(writer);
                            writer.Flush();
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Error while saving game.");
            }
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
            Debug.WriteLine("Displaying file " + filename);
            Debug.WriteLine("--- file begins ---");
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isf.FileExists(filename))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(filename, FileMode.Open, isf))
                    {
                        using (StreamReader reader = new StreamReader(isfs))
                        {
                            try
                            {
                                Debug.WriteLine(reader.ReadToEnd());
                                Debug.WriteLine("--- file ends ---");
                            }
                            catch (Exception)
                            {
                                Debug.WriteLine("Error reading file.");
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                    }
                }
            }
        }
    }

    public static class IOUtil
    {
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
        bool verticalMotion = false;

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

        private void Save()
        {
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(filename, FileMode.Create, isf))
                    {
                        using (StreamWriter writer = new StreamWriter(isfs))
                        {
                            writer.WriteLine(version);
                            writer.WriteLine(enableSound);
                            writer.WriteLine(enableMusic);
                            writer.WriteLine(verticalMotion);
                            writer.Flush();
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception) //eww catch all
            {
                Debug.WriteLine("Error while saving options.");
            }
        }

        private Options()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isf.FileExists(filename))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(filename, FileMode.Open, isf))
                    {
                        using (StreamReader reader = new StreamReader(isfs))
                        {
                            try
                            {
                                int version = IOUtil.ReadInt(reader);
                                enableSound = IOUtil.ReadBool(reader);
                                enableMusic = IOUtil.ReadBool(reader);
                                verticalMotion = IOUtil.ReadBool(reader);
                            }
                            catch (FormatException)
                            {
                                Debug.WriteLine("Error in options file.");
                                Debug.WriteLine(reader.ReadToEnd());
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                    }
                }
            }
        }

    }

    //TODO: split out persistent options into the Options class
    public class Tweaking
    {
        #region Accelerometer controls
        public static double AccelDefaultY = -1.0;
        public static int AccelerationSmoothing = 3;
        public const int MaxAccelerationSmoothing = 10;
        public static bool RecalibrateAccel = true;
        #endregion

        public static bool DraggableAnchors = false;
        //Settings here
        public const bool isCheatsEnabled = false;
        
        // Options menu options     

        //other random stuff
        public static bool EnableSound = true; //true
        public static bool DrawTouchAnchors = true;
        //These must be set correctly before release

        public const bool DebugFileStuff = false; // set to false
        public const bool DrawUpsellButtons = false; // set to false

        public static bool ShowPerfStats = false; //false
        public const bool ParticleStressTest = false; //false makes particles last forever
        public const int particleSize = 4; //4 is the minimum OK value.
        public const bool AllowParticlePushing = true; //true - this drops the particle limit on my PC from 13,360 to 10,000
    }

    public class ParticleGroup : Drawable
    {
        float normalAlphaDrain;
        float alpha; //duplicates int a;
        Particle[] ptArray;
        int ptFirstEmpty;
        Core core;
        ParticleType expType;

        public ParticleGroup(int size, ParticleType expType)
            : base(null)
        {
            IsAlive = true;
            core = Core.Instance;
            alpha = 1.0f;
            ptArray = new Particle[size];
            a = (byte)(alpha * 255f);
            this.expType = expType;
            if (expType == ParticleType.SCATTER)
            {
                normalAlphaDrain = 1f;
            }
            else if (expType == ParticleType.FAST_SCATTER)
            {
                normalAlphaDrain = 1.75f;
            }
            else
            {
                normalAlphaDrain = 0.5f;
            }
            //normalAlphaDrain *= 0.5f;
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
            foreach (Particle pt in ptArray) //we could stop at ptFirstEmpty-1
            {
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
            List<Shot> shots = new List<Shot>(10); //lame performance hack - recycle a shots list instead of creating one per particle.
            Core core = Core.Instance; //another unverified performance tweak. Pass it in so it doesn't have to be looked up. Can't think it makes a difference.
            for (int i = 0; i < ptFirstEmpty; i++)
            {
                Particle pt = ptArray[i];
                if (pt != null)
                {
                    core.ActiveParticleCounter++;
                    pt.Move(delta, a, canBePushed, expType, core, shots);
                    if (shots.Count > 0) { shots.Clear(); }
                }
                //We don't delete dead particles. They all disappear when the ParticleGroup does.
            }
        }

        public void ReleaseParticles()
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
        }
    }

    //from http://blog.gallusgames.com/programming/lean-mean-object-pool-in-c
    public class Pool<T> where T : new()
    {
        private T[] pool;
        private int nextItem = -1;

        private bool hasPoolExausted; //limits debug messages.
        private bool hasPoolOverfilled; //limits debug messages.

        public Pool(int capacity)
        {
            pool = new T[capacity];
            for (int i = 0; i < capacity; i++)
            {
                pool[i] = new T();
            }
        }

        public override string ToString()
        {
            return base.ToString() + "( nextItem " + nextItem + ", capacity " + pool.Length + ")";
        }

        public T Fetch()
        {
            if (nextItem == pool.Length - 1)
            {
                T newT = new T();
                if (!hasPoolExausted)
                {
                    Debug.WriteLine("Pool exausted, creating an instance of " + newT.GetType().Name);
                    hasPoolExausted = true;
                }
                return newT;
            }
            return pool[++nextItem];
        }

        public void Release(T instance)
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

    public class Particle : Drawable
    {
        float xV;
        float yV;
        const float speed = 0.2f;

        public override void Move(int delta)
        {
            throw new InvalidOperationException("Particles don't actually support the normal move method, use the special one instead. Wow this is naughty.");
        }

        #region pooling
        private const int PoolSize = 8000; //performance degrades at 5000
        private static Pool<Particle> Pool;

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
            Pool = new Pool<Particle>(PoolSize);
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
            p.IsAlive = true;
            Core core = Core.Instance;

            switch (type)
            {
                case ParticleType.SCATTER:
                    p.xV = (float)(core.Random.NextDouble() * randomBetween(-0.7, 0.7));
                    p.yV = (float)(core.Random.NextDouble() * randomBetween(-0.7, 0.7));
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

        //Hacks here: shots should always be passed in empty. It should be an instance variable, it's only passed in to save allocating an object (does it matter?)
        public void Move(int delta, byte a, bool canBePushed, ParticleType expType, Core core, List<Shot> shots)
        {
            Debug.Assert(shots.Count == 0, "the shots array should be empty. It's only passed in as a lame untested performance tweak.");
            Position.X += xV;
            Position.Y += yV;

            this.a = a;

            //friction
            xV *= 0.98f;
            yV *= 0.98f;

            //collisions
            if (canBePushed)
            {
                //Find all shots that are vertically near us.
                int cell = Core.PositionToIndex(Position);

                List<Shot> shots1;
                if (core.shotLookup.TryGetValue(cell, out shots1)) { shots.AddRange(shots1); }
                //check the cell above as well, in case we are near the edge of a cell.
                List<Shot> shots2;
                if (core.shotLookup.TryGetValue(cell - 1, out shots2)) { shots.AddRange(shots2); };

                //Loop through the shots.
                if (shots != null && shots.Count > 0)
                {

                    for (int i = 0; i < shots.Count(); i++)
                    {
                        Shot s = shots[i];
                        if (s.IsAlive)
                        {
                            //Check if it is horizontally near us.
                            if (Math.Abs(s.centerX() - Position.X) < s.Width * 2.5)
                            {
                                //check that it vertically passed us this frame.
                                float heightDiff = s.Position.Y - Position.Y;
                                int dir = Math.Sign(s.Speed);
                                heightDiff *= dir;
                                int deltaDist = (int)(dir * s.Speed * delta * 1.5); //the distance the shot moves per frame ( plus half a frame to be safe)
                                if (heightDiff > 0 && heightDiff < deltaDist)
                                {
                                    float multi = 4;
                                    //if (expType == ParticleType.FAST_SCATTER) multi = 4;
                                    int direction = Math.Sign(Position.X - s.centerX());
                                    if (direction == 0) direction = 1; //we must be on one side of the shot or the other.
                                    xV = xV + 0.0009f * multi * delta * direction;
                                    //HAXX:
                                    //r = Core.Instance.Random.Next(0, 255);
                                    // g = Core.Instance.Random.Next(0, 255);
                                    // b = Core.Instance.Random.Next(0, 255);
                                    // }
                                }

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
    public class Shot : Drawable
    {
        #region pooling
        public const int PoolSize = 50;
        private static Pool<Shot> Pool;

        public static Shot Create(Texture2D texture)
        {
            Debug.Assert(Pool != null, "Pool not initialized");
            Shot s = Pool.Fetch();
            s.Initialize(texture);
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
            Pool = new Pool<Shot>(PoolSize);
        }
        #endregion

        public float Speed;
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

        public virtual void Initialize(Texture2D texture)
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
            Position.Y += Speed * delta;
            Position.X += XSpeed * delta;
            if (Position.Y + Height < 0 && Speed < 0) IsAlive = false;
            if (Position.Y > Window.Height && Speed > 0) IsAlive = false;
            if (Position.X + Width < 0) IsAlive = false;
            if (Position.X > Window.Width) IsAlive = false;
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
            Shot s =  Shot.Create(Core.Instance.Art.ShotHeartPicture);
            s.Speed = StandardShotSpeed;
            s.HurtsEnemy = 0;
            s.HurtsPlayer = 30;
            s.Pierce = 0;
            s.Owner = -1;
            s.expType = ParticleType.SCATTER;
            s.Position.X = s.offCenterX(x);
            s.Position.Y = y;
            s.Speed *= speedMulti;
            return s;
        }

        internal static Shot CreateHeartBig(int x, int y, float speedMulti)
        {
            Shot s = Shot.Create(Core.Instance.Art.ShotHeartBigPicture); //difference from CreateHeart
            s.Speed = StandardShotSpeed;
            s.HurtsEnemy = 0;
            s.HurtsPlayer = 30;
            s.Pierce = 1; //difference from CreateHeart
            s.Owner = -1;
            s.expType = ParticleType.SCATTER;
            s.Position.X = s.offCenterX(x);
            s.Position.Y = y;
            s.Speed *= speedMulti;
            return s;
        }

        internal static Shot CreateMonkShot(float x, float y, float speedMulti)
        {
            Shot s = Shot.Create(Core.Instance.Art.MonkShot);
            s.Speed = StandardShotSpeed;
            s.HurtsEnemy = 0;
            s.HurtsPlayer = 30;
            s.Pierce = 0;
            s.Owner = -1;
            s.expType = ParticleType.SCATTER;
            s.Position.X = x; //not off centered!
            s.Position.Y = y;
            s.Speed *= speedMulti;
            return s;
        }

        public enum ShotType { Normal, Mega }; //Only used by player shots

        internal static Shot CreatePlayerShot(ShotType type, int inX, int inY, float SpeedMultiplier, int playerId)
        {
            Shot s = Shot.Create(null); //we set the texture, height and width manually so we can pass null here.
            if (type == ShotType.Normal)
            {
                s.Picture = Core.Instance.Art.PlayerShotBase; //should be different for player 1 and player 2.
                s.Pierce = 0;
                s.expType = ParticleType.FAST_SCATTER;
                s.Speed = -StandardShotSpeed;
            }
            else
            {
                s.Speed = -StandardShotSpeed * .83f;
                s.expType = ParticleType.SCATTER;
                s.Picture = Core.Instance.Art.PlayerShotMega;
                s.Pierce = 1;
            }
            s.Width = s.Picture.Width;
            s.Height = s.Picture.Height;
            s.HurtsEnemy = 1;
            s.HurtsPlayer = 0;
            s.Owner = playerId;
            s.Position.X = s.offCenterX(inX);
            s.Position.Y = inY;
            s.Speed *= SpeedMultiplier;
            s.Owner = playerId;

            if (s.Owner == 0)
            {
                s.r = 255;
                s.g = 255;
                s.b = 255;
            }
            else
            {
                s.r = 100;
                s.g = 100;
                s.b = 100;
            }
            return s;
        }
    }

    public class Beam : Drawable
    {
        int Timer;
        Enemy Parent;
        const int LifeSpan = 1400;
        const int GrowSpan = 700;
        const float BeamSpeed = Window.Height * 0.67f / 500;
        const int Width2 = Window.Width * 15 / 300;
        const int InitialWidth = Window.Width * 2 / 300;
        const float Damage = 0.15f;

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
                    p.PushDown(delta * Damage, delta * Damage);
                    isHit = true;
                }
                if (p2 != null && p2.IsAlive && (p2.Position.X + p2.Width > Position.X) && (p2.Position.X < Position.X + Width) && (p2.Position.Y + p2.Height > Position.Y))
                {
                    p2.PushDown(delta * Damage, delta * Damage);
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

    public class Special
    {
        int duration;
        int time;
        int spawnPeriod;
        int spawned;
        int spawnStart;
        Core core;
        int stage;
        float speedMulti;

        bool makeSeekingBlocks;
        bool makeCentreBlocks;
        bool makeWallBlocks;

        public int Creatures;

        public Special(int level)
        {
            stage = level; //do'h
            duration = 15000;
            spawnStart = 200;
            int specialType = stage % 3;

            if (specialType == 0)
            {
                //special divided-screen stage
                makeCentreBlocks = true;

                makeSeekingBlocks = true;
                speedMulti = 0.75f; //slow
                spawnPeriod = 550;
                if (stage <= 3) spawnPeriod = (int)(spawnPeriod * 1.3f); //super slow the first time
                makeWallBlocks = false;
                if (stage > 6 && stage % 2 != 0)
                {
                    //this will be the third time you face this level.
                    if (stage <= 3) spawnPeriod = (int)(spawnPeriod * 1.2f); //slower because...
                    Creatures = 1; //monk wave
                }
            }
            else if (specialType == 2)
            {
                //special wall stage
                makeSeekingBlocks = false;
                speedMulti = 0.64f;
                spawnPeriod = (int)(1250 * 1f / 0.7f);
                makeWallBlocks = true;
                if (stage > 3) // from the second time, you face monks
                {
                    Creatures = 1; //monk wave
                }
            }
            else // specialType == 1
            {
                makeSeekingBlocks = true;
                speedMulti = (float)Math.Min(0.7 + stage * .12, 1.3);
                spawnPeriod = Math.Max(480 - stage * 40, 220);
                makeWallBlocks = false;
                if (stage > 9 && stage % 2 != 0)
                {
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
                if (makeSeekingBlocks)
                {
                    //TODO: if it's 2 player, randomly pick a player to target.
                    Player target;
                    if (core.P2 != null && core.P2.IsAlive && core.Random.Next(0, 2) == 0)
                    {
                        target = core.P2;
                    }
                    else
                    {
                        target = core.P;
                    }
                    spawnSeekingBlocks(target);
                }
                if (makeCentreBlocks)
                {
                    spawnCentreBlocks();
                }
                if (makeWallBlocks)
                {
                    spawnWallBlocks();
                }
            }
        }

        private void spawnSeekingBlocks(Player p)
        {
            int x = p.centerX() + core.Random.Next(-130, 130);
            if (x < 10) x = core.Random.Next(10, 60);
            if (x > Window.Width) x = Window.Width - core.Random.Next(10, 60);
            Shot s = Shot.CreateHeart(x, -200, speedMulti);
            core.AddShot(s);
        }

        private void spawnCentreBlocks()
        {
            int x = Window.Width / 2;
            Shot s = Shot.CreateHeartBig(x, -200, speedMulti);
            core.AddShot(s);
        }

        private void spawnWallBlocks()
        {
            int stepWidth = Window.Width * 40 / 300;
            int steps = Window.Width / stepWidth + 1;
            int safeStep = core.Random.Next(0, steps - 2);

            for (int x = 0; x < steps; x++)
            {
                if (x != safeStep && x != safeStep + 1)
                {
                    Shot s = Shot.CreateHeart(x * stepWidth + 10, -50, speedMulti);
                    core.AddShot(s);
                }
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
        const int maxParticleGroups = 50; // in testing, 20 was the max in single player
        ParticleGroup[] ptArray = new ParticleGroup[maxParticleGroups];

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
                P2.Position.X = Window.Width / 3 * 1 - P.Width / 2;
                P2.RefireTimer = P2.ROF / 2;
                P.Position.X = Window.Width / 3 * 2 - P2.Width / 2;
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
                    //Release my particles
                    pt.ReleaseParticles();
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
                    CreateMessage("Tap to restart", 7, 0);
                    lostMessage = true;
                }

                if (Tick > lostTick + resetDelay && Input.TappedThisFrame)
                {
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
                    CreateDeathMessage("You are both destroyed!");
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
                        CreateDeathMessage("You are destroyed!");
                        SetLost();
                    }
                }
            }

            if (P2 != null && !P2.isOnScreen())
            {
                if (P2.IsAlive)
                {
                    P2.IsAlive = false;
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
                        cheatTimer = 10;
                        PowerUpChance = 0.5f;
                    }
                }
            }

            //combine shake for 2 player
            if (P2 != null)
            {
                P.Shake += P2.Shake;
                P2.Shake = 0;
            }

            //Visual effects
            if (P.Shake > 0)
            {
                P.Shake -= 0.3 * delta;
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
                PowerUps.Move(delta);
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
                if (!hasLost && lowestEnemy.Position.Y > Window.Height)
                {
                    CreateDeathMessage("Aliens got past you!");
                    SetLost();
                }
            }
            else
            {
                lowestEnemyCentreX = Window.Width / 2;
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
            //int xCell = (int)(pos.X * 2 / Window.Width);
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
                    xOff = (Math.Min(Math.Abs(xOff), Window.Width / 4) + Random.Next(1, 2)) * -Math.Sign(xOff);
                }
            }
            else
            {
                xOff = (Math.Min(Math.Abs(xOff), Window.Width / 4) - Random.Next(1, 2)) * -Math.Sign(xOff);
            }


            screenManager.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            //screenManager.SpriteBatch.Begin();

            screenManager.SpriteBatch.Draw(Gfx.Pixel, new Rectangle(0, 0, Window.Width, Window.Height), Color.Gray);

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
                screenManager.SpriteBatch.DrawString(Message.GameFont, String.Format("Mem: {0:00000}KB", GC.GetTotalMemory(false) / 1024), new Vector2(10, 10), Color.Tomato);
                screenManager.SpriteBatch.DrawString(Message.GameFont, "particles: " + ActiveParticleCounter, new Vector2(10, 50), Color.Tomato);
            }

            if (scores != null)
            {
                scores.Draw(xOff, yOff, screenManager);
            }

            if (P2 != null)
            {
                //FIXME: uses memory every frame!
                string msg1 = "Score: " + P.Score;
                string msg2 = "Score: " + P2.Score;
                drawString(msg2, 30, Window.Height - 30, 0, 0, screenManager);
                drawString(msg1, Window.Width - 160, Window.Height - 30, 0, 0, screenManager);

                //display a cross when trying to move a dead player (if there is also an alive player)
                if (!hasLost && !P2.IsAlive && Input.P2HasFingerDown)
                {
                    screenManager.SpriteBatch.Draw(Gfx.Cross, new Rectangle(0, 0, Window.Width/2, Window.Height), CrossColor);
                }
                if (!hasLost && !P.IsAlive && Input.P1HasFingerDown)
                {
                    screenManager.SpriteBatch.Draw(Gfx.Cross, new Rectangle(Window.Width / 2, 0, Window.Width / 2, Window.Height), CrossColor);
                }
            }

            //draw touchanchors
            if (Tweaking.DrawTouchAnchors)
            {
                if (Input.touchAnchor1 != null)
                {
                    DrawAnchor(Gfx.JoystickBase, Input.touchAnchor1.Position, screenManager, false);
                    DrawAnchor(Gfx.Joystick, Input.touchAnchor1.MovePosition, screenManager, Input.touchAnchor1.MovePosition.X > Input.touchAnchor1.Position.X);
                }
                if (Input.touchAnchor2 != null)
                {
                    DrawAnchor(Gfx.JoystickBase, Input.touchAnchor2.Position, screenManager, false);
                    DrawAnchor(Gfx.Joystick, Input.touchAnchor2.MovePosition, screenManager, Input.touchAnchor2.MovePosition.X > Input.touchAnchor2.Position.X);
                }
            }

            if (Version.IsTrialMode)
            {
                if (InTrialLimbo)
                {
                    //In limbo means we tried to advance to a new level, but we were out of trial levels. Instead we advance to an upsell screen.
                    Input.UpsellBuyButton = DrawUpsell(UPSELL_GET_FULL_VERSION, Window.Height / 5 * 2, 1f, screenManager);
                    //DrawUpsell("MORE GREAT ADVENTURES", Window.Height / 4 + 60, 1f, screenManager);
                    //DrawUpsell("1 OR 2 PLAYERS", Window.Height / 4 + 120, 1f, screenManager);
                    Input.UpsellBackButton = DrawUpsell(UPSELL_BACK, Window.Height / 5 * 2 + 100, 0.5f, screenManager);
                }
                screenManager.SpriteBatch.DrawString(Message.GameFont, trialModeString, new Vector2(Window.Width / 2, 10), Color.White, 0f, new Vector2(trialModeStringWidth/2, 0), 0.5f, SpriteEffects.None, 0f);
            }

            screenManager.SpriteBatch.End();
        }

        private const string UPSELL_GET_FULL_VERSION = "GET THE FULL VERSION";
        private const string UPSELL_BACK = "BACK TO MAIN MENU";

        private Rectangle DrawUpsell(string message, int yPos, float scale, ScreenManager screenManager)
        {
            Vector2 textSize = Message.GameFont.MeasureString(message);
            int width = (int)(textSize.X);
            screenManager.SpriteBatch.DrawString(Message.GameFont, message, new Vector2(Window.Width / 2 + 1, yPos + 1), Color.Black, 0f, new Vector2(width / 2, 0), scale, SpriteEffects.None, 0f);
            screenManager.SpriteBatch.DrawString(Message.GameFont, message, new Vector2(Window.Width / 2, yPos), Color.White, 0f, new Vector2(width / 2, 0), scale, SpriteEffects.None, 0f);
            Rectangle button = new Rectangle((int)(Window.Width / 2 - (textSize.X * scale / 2)), yPos, (int)(textSize.X * scale), (int)(textSize.Y * scale));
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

        public Message CreateOffCenterMessage(String text, int row, int yPos, float alphaDrainMulti)
        {
            Debug.Assert(row <= 7);
            //Note, Off center messages do not 'fill up' a row.
            Message message = Message.CreateAtRowWithYOffset(text, row, yPos);
            messageList.Add(message);
            return message;
        }

        private Message CreateMessageImg(String path, int row)
        {
            return null;
            //FIXME
            /*  Method createMessageImg:messageType(path:String, row:Int=0)
        Local m:messageType = messageType.createImg(path,windowType.width/2, windowType.height/2 + 16*row)
        messageList.addLast(m)      
        Return m
    EndMethod*/
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
            P.Upgrade(Random.Next(0, PowerUp.PowerUpTypesCount));
        }

        public void CheatSad()
        {
            if (!Tweaking.isCheatsEnabled) return;
            Shot s = Shot.CreateMonkShot(P.centerX() - 5, 20, 1);
            AddShot(s);
        }

        public ParticleGroup CreateParticleGroup(int width, int height, ParticleType expType)
        {
            if (ptFirstEmpty == maxParticleGroups - 1)
            {
                DebugLog("Overload - too many particle groups"); //i don't recycle them yet so this always happens after 60 kills.
                return null;
            }
            else
            {
                ParticleGroup ptg = new ParticleGroup(width * height, expType);
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

            if (level == 1)
            {
                if (P2 != null)
                {
                    CreateOffCenterMessage("Player 1", 2, (int)(Window.Width / 4), 1f);
                    CreateOffCenterMessage("Player 2", 2, (int)(Window.Width * 3 / 4), 1f);
                    //CreateMessage("Touch for control", 3);
                }
                else
                {
                    //CreateMessage("slide or tilt to move", 3);
                    //CreateMessage("slide or tilt to move", 3);
                }
                /* for 2 player: createMessage("Player 1",-4)
                createMessage("Use arrow keys",-3)
                createMessage("Player 2",-2)
                createMessage("Use W, A and D keys",-1)     */
                //CreateMessageImg("keys.png", 5);
            }

            if (every[3] == 0)
            {
                CreateMessage("Promotion to " + GetRank(), 1);
                // P.Promotion();
                // if (P2 != null) P2.Promotion();
            }
            CreateMessage("Level " + level, 0);

            if (level >= 5 && every[5] == 0)
            {
                special = new Special(level / 5);
                DebugLog("Special Stage");
                if (level == 5 || level == 10)
                {
                    CreateMessage("Avoid", -2);
                }
                if (special.Creatures == 1)
                {
                    MakeMonkWave();
                }
                return;
            }

            //Act 1 - levels 1 to 8
            //'Octo or Monk       
            //1:O 2:O 3:O 4:M 5:--special-- 6:M 7:O 8:M 9:O  

            if (level < 9)
            {
                if (level > 3 && every[2] == 0)
                {
                    MakeMonkWave();
                }
                else
                {
                    MakeOctoWave();
                }
                return;
            }

            //Act 2: Levels 9 to 11
            //Octo, Star.
            if (level < 12)
            {
                if (level == 9) MakeStarWave();
                if (level == 10) MakeOctoWave();
                if (level == 11) MakeStarWave();
                return;
            }

            //Act 3: levels 12 to 19
            //Octo + Monk, Star + Monk
            if (level < 20)
            {
                if (every[2] == 0)
                {
                    MakeOctoWave();
                    MakeMonkWave();
                }
                else
                {
                    MakeMonkWave();
                    MakeStarWave();
                }
                return;
            }

            //Act 4: levels 20 to infinity
            //Specials override some of these, remember
            switch (every[7])
            {
                case 0:
                    MakeOctoWave();
                    break;
                case 1:
                    MakeMonkWave();  //this is meant to be monk alone, but that's too boring.
                    MakeOctoWave();
                    break;
                case 2:
                    MakeStarWave();
                    break;
                case 3:
                    MakeOctoWave();
                    MakeMonkWave();
                    break;
                case 4:
                    MakeOctoWave();
                    MakeStarWave();
                    break;
                case 5:
                    MakeMonkWave();
                    MakeStarWave();
                    break;
                case 6:
                    MakeMonkWave();
                    MakeStarWave();
                    MakeOctoWave();
                    break;
                default: Debug.WriteLine("Math error, science does not exist.");
                    throw new ArgumentException("Terrible Math Error: Unexpected value for every[7] which was " + every[7]);
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
                m.Position.X = (Window.Width / 5) * Random.Next(1, 5) - m.Width / 2;
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
            else if (level - 2 < 9)
            {
                maxBeams = ((level - 2) / 3) + 1;
            }
            else
            {
                maxBeams = 4;
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
                    e.Position.X = Window.Width + (i - rightSquad) * xSpacing; //right side
                    e.Position.Y = ySpacing;
                    e.Row = 1;
                    e.Direction = -1;
                }
                AlienList.Add(e);
            }

        }

        public void MakeStarWave()
        {

            //fixme: at high levels, stars should shoot (don't yet)
            //the starwave is split into sub waves once it gets too big, to stop it getting impossible to destroy in time.
            int numAliens;
            if (level < 10)
            {
                numAliens = 4 + level / 3;
            }
            else
            {
                numAliens = 4 + 10 / 3 + (level - 10) / 5;
            }

            float stillTimeMulti;
            if (level < 10)
            {
                stillTimeMulti = 1f;
            }
            else if (level < 15)
            {
                stillTimeMulti = 0.5f;
            }
            else
            {
                stillTimeMulti = 0.3f;
            }

            int ySpacing = (int)(Core.Instance.Art.Star.Height * 1.1);
            int addHeight = -ySpacing;
            while (numAliens > 0)
            {
                for (int i = 1; i <= 4 && numAliens > 0; i++) //weird for loop because of blitzmax old ways. TODO: make start from zero...
                {
                    Star a = new Star();
                    a.Position.X = (Window.Width / 5) * i - a.Width / 2;
                    int wave = numAliens / 8;
                    a.Position.Y = addHeight - wave * a.Height;
                    a.MaxStillTime = (int)(a.MaxStillTime * stillTimeMulti);
                    AlienList.Add(a);
                    numAliens--;
                }
                addHeight -= ySpacing;
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
        public bool TappedThisFrame;

        public bool P1HasFingerDown;
        public bool P2HasFingerDown;

        public TouchAnchor touchAnchor1;
        public TouchAnchor touchAnchor2;

        //Input Members
        TouchCollection touchState;

        //Hacks for the upsell screen
        public Rectangle UpsellBuyButton;
        public Rectangle UpsellBackButton;
        public UpsellAction UpsellAction;

        double[] accelX = new double[Tweaking.MaxAccelerationSmoothing];
        double[] accelY = new double[Tweaking.MaxAccelerationSmoothing];
        int accelCount = 0;
        double smoothedAccelX = 0;
        double smoothedAccelY = 0;

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

        public void HandleInputs(InputState input, AccelerometerReadingEventArgs accelState, Player player, Player player2)
        {
                //Vector2 touchPos1 = new Vector2(-1, -1);
                //Vector2 touchPos2 = new Vector2(-1, -1);

                touchState = TouchPanel.GetState();
                float touchXChange1 = 0;
                float touchXChange2 = 0;
                float touchYChange1 = 0;
                float touchYChange2 = 0;

                bool wasFingerDown = HasFingerDown;
                HasFingerDown = false;
                TappedThisFrame = false;
                P1HasFingerDown = false;
                P2HasFingerDown = false;

                UpsellAction = UpsellAction.NONE;

                //interpret touch screen presses
                foreach (TouchLocation location in touchState)
                {
                     switch (location.State)
                     {
                       case TouchLocationState.Pressed:
                           UpsellHacksPressed(location.Position);
                           HasFingerDown = true;
                           if (Core.Instance.P2 != null && location.Position.X < Window.Width / 2)
                           {
                               Debug.WriteLine("New touch LEFT");
                               touchAnchor2 = new TouchAnchor(location.Position, location.Id);
                               P2HasFingerDown = true;
                           }
                           else
                           {
                               Debug.WriteLine("New touch RIGHT");
                               touchAnchor1 = new TouchAnchor(location.Position, location.Id);
                               P1HasFingerDown = true;
                           }
                           break;
                        case TouchLocationState.Moved:
                            HasFingerDown = true;
                            if (touchAnchor2 != null && location.Id == touchAnchor2.Id)
                            {
                                touchAnchor2.MovePosition = location.Position;
                                touchXChange2 = location.Position.X - touchAnchor2.Position.X;
                                touchYChange2 = location.Position.Y - touchAnchor2.Position.Y;
                                P2HasFingerDown = true;
                                
                            } else if (touchAnchor1 != null && location.Id == touchAnchor1.Id)
                            {
                                touchAnchor1.MovePosition = location.Position;
                                touchXChange1 = location.Position.X - touchAnchor1.Position.X;
                                touchYChange1 = location.Position.Y - touchAnchor1.Position.Y;
                                P1HasFingerDown = true;
                            }
                            else
                            {
                                //Handling an edge case that I wish I could test for...
                                touchAnchor1 = new TouchAnchor(location.Position, location.Id);
                                P1HasFingerDown = true;
                            }
                            
                            break;
                        case TouchLocationState.Released:
                            break;

                     }
                    

                }

                if (HasFingerDown && !wasFingerDown)
                {
                    TappedThisFrame = true;
                }

                if (!P2HasFingerDown)
                {
                    touchAnchor2 = null;
                }

                if (!P1HasFingerDown)
                {
                    touchAnchor1 = null;
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

        /*        if (accelState != null)
                {
                    accelX[accelCount] = accelState.X;
                    accelY[accelCount] = accelState.Y;
                    smoothedAccelX = 0;
                    smoothedAccelY = 0;
                    for (int i = 0; i < Tweaking.AccelerationSmoothing; i++)
                    {
                        smoothedAccelX += accelX[i];
                        smoothedAccelY += accelY[i];
                    }
                    smoothedAccelX /= Tweaking.AccelerationSmoothing;
                    smoothedAccelY /= Tweaking.AccelerationSmoothing;

                    accelCount++;
                    if (accelCount >= Tweaking.AccelerationSmoothing)
                    {
                        accelCount = 0;

                        if (Tweaking.RecalibrateAccel)
                        {
                            Debug.WriteLine("Recalibrating up\\down accelerometer movement to " + smoothedAccelY);
                            Tweaking.RecalibrateAccel = false;
                            Tweaking.AccelDefaultY = smoothedAccelY;
                        }
                    }
                }

                //ignore accelerometer in two player games.
                //ignore acceleromter when the player is touching the screen.
                if (accelState != null && Core.Instance.P2 == null && !HasFingerDown) 
                {
                    //Dead zone: according to someone, a device sitting flat can get variance of up to .06
                    //I haven't done any smoothing here :/ and may need it.
                    if (Math.Abs(smoothedAccelX) > 0.10f) //dead zone
                    {
                        if (smoothedAccelX > 0.0f)
                            player.right = true;
                        else
                            player.left = true;
                    }

                    if (Tweaking.VerticleMotion && Math.Abs(smoothedAccelY - Tweaking.AccelDefaultY) > 0.08f) //dead zone
                    {
                        if (smoothedAccelY + Tweaking.AccelDefaultY < 0.0f)
                            player.down = true;
                        else
                            player.up = true;
                    }
                }*/

                ProcessTouchMovement(player, touchXChange1, touchYChange1, touchAnchor1);
                if (player2 != null) ProcessTouchMovement(player2, touchXChange2, touchYChange2, touchAnchor2);

                //use keyboard for cheats
                KeyboardState keyState = Keyboard.GetState();
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
                if (keyState.IsKeyDown(Keys.W)) Core.Instance.CheatSkipLevel(1);
                if (keyState.IsKeyDown(Keys.E)) Core.Instance.CheatPowerUp();
                if (keyState.IsKeyDown(Keys.R)) Core.Instance.CheatSad();
                if (keyState.IsKeyDown(Keys.T)) Core.Instance.CheatManyPowerUps(true);
                if (keyState.IsKeyDown(Keys.Y)) Core.Instance.CheatManyPowerUps(false);
                if (keyState.IsKeyDown(Keys.U)) Tweaking.ShowPerfStats = true;
                if (keyState.IsKeyDown(Keys.I)) Tweaking.ShowPerfStats = false;
                
                //keyboard control (overrides accelerometer and touch)
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

                //if (input.CurrentGamePadStates[0].DPad.Left == ButtonState.Pressed
                //else if (input.CurrentGamePadStates[0].DPad.Right == ButtonState.Pressed 
                //    player.Velocity.X = MathHelper.Min(input.CurrentGamePadStates[0].ThumbSticks.Left.X * 2.0f, 1.0f);
                // B button, or pressing on the upper half of the pad or space on keyboard or touching the touch panel fires the weapon.
                //               if (input.CurrentGamePadStates[0].IsButtonDown(Buttons.B) || input.CurrentGamePadStates[0].IsButtonDown(Buttons.A) || input.CurrentGamePadStates[0].ThumbSticks.Left.Y > 0.25f ||
                //        keyState.IsKeyDown(Keys.Space) || buttonTouched)
        }

        //TODO: calculate touchXChange and touchYChange in here instead of passing in.
        private void ProcessTouchMovement(Player p, float touchXChange, float touchYChange, TouchAnchor touchAnchor)
        {
            if (touchAnchor == null) return;
            Vector2 touchPos = touchAnchor.MovePosition;
            //touching overrides the accelerometer
            if (Math.Abs(touchXChange) > 15) //threshhold
            {
                bool tooFarAway = (Math.Abs(touchXChange) > 30); //drag the anchor after us if we move too far
                if (touchXChange > 0)
                {
                    p.right = true;
                    p.left = false;
                    if (tooFarAway && Tweaking.DraggableAnchors)
                    { 
                        touchAnchor.Position.X = touchPos.X - 30; 
                    }
                }
                else
                {
                    p.left = true;
                    p.right = false;
                    if (tooFarAway && Tweaking.DraggableAnchors) { touchAnchor.Position.X = touchPos.X + 30; }
                }

                if (tooFarAway && !Tweaking.DraggableAnchors)
                {
                    float x = touchAnchor.Position.X;
                    float moveX = touchAnchor.MovePosition.X;
                    touchAnchor.MovePosition.X = Math.Max(Math.Min(moveX, x + 30), x - 30);
                }
            }

            if (Options.Instance.VerticalMotion)
            {
                if (Math.Abs(touchYChange) > 15) //threshhold
                {
                    bool tooFarAway = (Math.Abs(touchYChange) > 30); //drag the anchor after us if we move too far
                    if (touchYChange > 0)
                    {
                        p.down = true;
                        p.up = false;
                        if (tooFarAway && Tweaking.DraggableAnchors) { touchAnchor.Position.Y = touchPos.Y - 30; }
                    }
                    else
                    {
                        p.up = true;
                        p.down = false;
                        if (tooFarAway && Tweaking.DraggableAnchors) { touchAnchor.Position.Y = touchPos.Y + 30; }
                    }
                    if (tooFarAway && !Tweaking.DraggableAnchors)
                    {
                        float y = touchAnchor.Position.Y;
                        float moveY = touchAnchor.MovePosition.Y;
                        touchAnchor.MovePosition.Y = Math.Max(Math.Min(moveY, y + 30), y - 30);
                    }
                }
            }
            else
            {
                touchAnchor.MovePosition.Y = touchAnchor.Position.Y;
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
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(set.ScoreFile, FileMode.Create, isf))
                    {
                        using (StreamWriter writer = new StreamWriter(isfs))
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
                            writer.Flush();
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Error while saving highscores.");
            }
            if (Tweaking.DebugFileStuff) DebugFileStuff.DisplayFileContents(set.ScoreFile);
        }

        /// <summary>
        /// Loads the high score from a text file.  The StorageDevice was selected during the loading screen.
        /// </summary>
        public static void Populate(ScoreSet set)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isf.FileExists(set.ScoreFile))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(set.ScoreFile, FileMode.Open, isf))
                    {
                        using (StreamReader reader = new StreamReader(isfs))
                        {
                            try
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
                            }
                            catch (FormatException)
                            {
                                Debug.WriteLine("Error in high scores");
                                Debug.WriteLine(reader.ReadToEnd());
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                    }
                }
            }
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
                grey.Player2 = Gfx.Scale(c.Load<Texture2D>("gfx/fighter"), Gfx.StandardScale, gd);
                //grey.Pixel = Gfx.Scale(c.Load<Texture2D>("gfx/px"), Gfx.StandardScale, gd);
                //grey.Cross = Gfx.Scale(c.Load<Texture2D>("gfx/cross"), Window.Width / 2, gd);
                //grey.Joystick = Gfx.Scale(c.Load<Texture2D>("gfx/joystick"), Gfx.StandardScale, gd);
                //grey.JoystickBase = Gfx.Scale(c.Load<Texture2D>("gfx/joystickbase"), Gfx.StandardScale, gd);
                grey.Star = Gfx.Scale(c.Load<Texture2D>("gfx/star"), Gfx.StandardScale, gd);

                grey.PlayerShotBase = Gfx.Scale(c.Load<Texture2D>("gfx/shot02"), Gfx.StandardScale, gd);
                grey.PlayerShotMega = Gfx.Scale(c.Load<Texture2D>("gfx/shotmega"), Gfx.StandardScale, gd);

                grey.ShotHeartPicture = Gfx.Scale(c.Load<Texture2D>("gfx/heart"), Gfx.StandardScale, gd);
                grey.ShotHeartBigPicture = Gfx.Scale(c.Load<Texture2D>("gfx/heartbig"), Gfx.StandardScale, gd);

                grey.MonkShot = Gfx.Scale(c.Load<Texture2D>("gfx/monkshot"), Gfx.StandardScale, gd);
                grey.Monk = Gfx.Scale(c.Load<Texture2D>("gfx/monk"), Gfx.StandardScale, gd);
                grey.MonkSpecialPicture[0] = Gfx.Scale(c.Load<Texture2D>("gfx/monk01"), Gfx.StandardScale, gd);
                grey.MonkSpecialPicture[1] = Gfx.Scale(c.Load<Texture2D>("gfx/monk10"), Gfx.StandardScale, gd);
                grey.MonkSpecialPicture[2] = Gfx.Scale(c.Load<Texture2D>("gfx/monk00"), Gfx.StandardScale, gd);
            }
        }

        #endregion

        //public Texture2D Pixel;
        //public Texture2D Cross;
        //public Texture2D Joystick;
       // public Texture2D JoystickBase;
        public Texture2D Star;
        public Texture2D PlayerShotBase;
        public Texture2D PlayerShotMega;
        public Texture2D ShotHeartPicture;
        public Texture2D ShotHeartBigPicture;
        public Texture2D MonkShot;
        public Texture2D Octo;
        public Texture2D Monk;
        public Texture2D[] MonkSpecialPicture = new Texture2D[3];
        public Texture2D Player1;
        public Texture2D Player2;

        private ArtSet()
        {
            //private constructor.
        }


    }

    public static class Gfx
    {
        public static Texture2D Pixel;
        public static Texture2D Cross;
        public static Texture2D Joystick;
        public static Texture2D JoystickBase;

        public const int StandardScale = 12;

        public static Texture2D Scale(Texture2D source, int scale, GraphicsDevice graphicsDevice)
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
            isTrialMode = Guide.IsTrialMode;
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
            //load limit from file
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isf.FileExists(filename))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(filename, FileMode.Open, isf))
                    {
                        using (StreamReader reader = new StreamReader(isfs))
                        {
                            try
                            {
                                int version = IOUtil.ReadInt(reader);
                                Debug.Assert(version == FILE_VERSION);
                                LevelsLeft = IOUtil.ReadInt(reader);
                            }
                            catch (FormatException)
                            {
                                Debug.WriteLine("Error in trial limits file.");
                                levelsLeft = 0;
                                Debug.WriteLine(reader.ReadToEnd());
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                    }
                }
                else
                {
                    levelsLeft = LEVEL_LIMIT;
                }
            }
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
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(filename, FileMode.Create, isf))
                    {
                        using (StreamWriter writer = new StreamWriter(isfs))
                        {
                            writer.WriteLine(FILE_VERSION);
                            writer.WriteLine(levelsLeft);
                            writer.Flush();
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception) //eww catch all
            {
                Debug.WriteLine("Error while saving trial limits.");
            }
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
            PlayerUpgrades.LoadImages(ScreenManager);

            Gfx.Pixel = Gfx.Scale(c.Load<Texture2D>("gfx/px"), Gfx.StandardScale, gd);
            Gfx.Cross = Gfx.Scale(c.Load<Texture2D>("gfx/cross"), Window.Width / 2, gd);
            Gfx.Joystick = Gfx.Scale(c.Load<Texture2D>("gfx/joystick"), Gfx.StandardScale, gd);
            Gfx.JoystickBase = Gfx.Scale(c.Load<Texture2D>("gfx/joystickbase"), Gfx.StandardScale, gd);

            Snd.PlayerShotSound = c.Load<SoundEffect>("snd/EugenSopot/menu_select22_edited");
            Snd.AlienDieSound = c.Load<SoundEffect>("snd/space sound jiggled");
            Snd.envSound = c.Load<SoundEffect>("snd/EugenSopot/new_radio9"); //was iSubmarineRunLoop 'looping damage sound
            Monk.DefaultShootSound = c.Load<SoundEffect>("snd/Gulping"); //was illegal\iBodyFall-002.wav
            Octo.DefaultShootSound = c.Load<SoundEffect>("snd/EugenSopot/new_radio13"); //iSpaceElevatorDown

            Player.DefaultPowerUpSound = c.Load<SoundEffect>("snd/EugenSopot/menu_select2"); //' was illegal\imodChainCatch-002.wav
            Player.DefaultHitSound = c.Load<SoundEffect>("snd/EugenSopot/explosion5"); //'was illegal\iBlowMusket-001.wav
            PlayerUpgrades.DefaultBeamSound = c.Load<SoundEffect>("snd/Carpassing");

            Message.GameFont = c.Load<SpriteFont>("fonts/leaguegothic");
            Particle.CreatePool();
            Shot.CreatePool();
        }
    }
}
