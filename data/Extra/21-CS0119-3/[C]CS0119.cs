using System;
using System.Collections.Generic;
using System.Numerics;
using System.Drawing;
using System.Windows.Forms;

namespace MyFirstGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;              // All default to private
        KeyboardState oldKeyState;
        SpriteBatch spriteBatch;
        bool gameOn = true;
        SpriteObject ballSprite, starSprite;         // A couple of sprites...
        Texture2D texStar, texBall;                  // This is a texture we can render.

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 900 - 32; // Force window size or it won't work...
            graphics.PreferredBackBufferWidth = 1600;    // Force window size
            Content.RootDirectory = "Content";
            graphics.IsFullScreen = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            oldKeyState = Keyboard.GetState();
            base.Initialize();

        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        // Set the coordinates to draw the sprite at.
        Vector2 starPosition = Vector2.Zero;
        Vector2 ballPosition = new Vector2(200, 0);

        // Store some information about the sprite's motion.
        Vector2 starSpeed = new Vector2(10.0f, 10.0f);
        Vector2 ballSpeed = new Vector2(10.0f, 0.0f);

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            texStar = Content.Load<Texture2D>("star");
            texBall = Content.Load<Texture2D>("ball");
            ballSprite = new SpriteObject(texBall, new Vector2(200, 0), new Vector2(10.0f, 0.0f));  // create two sprites
            starSprite = new SpriteObject(texStar, Vector2.Zero, new Vector2(10.0f, 10.0f));
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Move the sprite by speed.

            KeyboardState newKeyState = Keyboard.GetState();
            if (newKeyState.IsKeyDown(Keys.Escape))
                Exit();

            if (collisionDetected()) gameOn = false; // If there's a collision then no movement.

            //Could do with putting this in a seperate method or class, if there isn't one already...   
            if (gameOn)
            {
                if (newKeyState.IsKeyDown(Keys.PageUp) && oldKeyState.IsKeyUp(Keys.PageUp))
                {
                    starSpeed.X *= 1.1f;
                    starSpeed.Y *= 1.1f;
                }
                else if (newKeyState.IsKeyDown(Keys.PageDown) && oldKeyState.IsKeyUp(Keys.PageDown))
                {
                    starSpeed.X *= 0.9f;
                    starSpeed.Y *= 0.9f;
                }
                else if (newKeyState.IsKeyDown(Keys.Up) && oldKeyState.IsKeyUp(Keys.Up))
                {
                    starSpeed.Y += 1.0f;
                }
                else if (newKeyState.IsKeyDown(Keys.Down) && oldKeyState.IsKeyUp(Keys.Down))
                {
                    starSpeed.Y -= 1.0f;
                }
                else if (newKeyState.IsKeyDown(Keys.Left) && oldKeyState.IsKeyUp(Keys.Left))
                {
                    starSpeed.X -= 1.0f;
                }
                else if (newKeyState.IsKeyDown(Keys.Right) && oldKeyState.IsKeyUp(Keys.Right))
                {
                    starSpeed.X += 1.0f;
                }
                oldKeyState = newKeyState;

                starPosition += starSpeed;
                ballPosition += ballSpeed;

                int MaxX =
                  graphics.GraphicsDevice.Viewport.Width - texStar.Width;
                int MinX = 0;
                int MaxY =
                  graphics.GraphicsDevice.Viewport.Height - texStar.Height;
                int MinY = 0;

                // Check for ball bounce
                if (ballPosition.X > MaxX)
                {
                    ballPosition.X = MaxX;
                    ballSpeed.X *= -1;
                }
                if (ballPosition.X < MinX)
                {
                    ballPosition.X = MinX;
                    ballSpeed.X *= -1;
                }
                if (ballPosition.Y > MaxY)
                {
                    ballSpeed.Y *= -1;
                    ballPosition.Y = MaxY;
                }

                ballSpeed.Y += 1;

                // Check for bounce.
                if (starPosition.X > MaxX)
                {
                    starSpeed.X *= -1;
                    starPosition.X = MaxX;
                }

                else if (starPosition.X < MinX)
                {
                    starSpeed.X *= -1;
                    starPosition.X = MinX;
                }

                if (starPosition.Y > MaxY)
                {
                    starSpeed.Y *= -1;
                    starPosition.Y = MaxY;
                }

                else if (starPosition.Y < MinY)
                {
                    starSpeed.Y *= -1;
                    starPosition.Y = MinY;
                }
            }
            else
            {
                starSpeed = Vector2.Zero;
                ballSpeed = Vector2.Zero;
            }
        }

        private bool collisionDetected()
        {
            if (((Math.Abs(starPosition.X - ballPosition.X) < texStar.Width) && (Math.Abs(starPosition.Y - ballPosition.Y) < texBall.Height)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw the sprite.
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            spriteBatch.Draw(texStar, starPosition, Color.White);
            spriteBatch.Draw(texBall, ballPosition, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void Exit() {}

        protected Device GraphicsDevice = new Device();
    }

    public abstract class Game
    {
        protected virtual void Initialize()
        {

        }

        protected virtual void LoadContent()
        {

        }

        protected virtual void Update(GameTime t)
        {

        }

        protected virtual void Draw(GameTime t)
        {

        }
    }

    public class GameTime
    {

    }

    public struct Vector2
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator+(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector2 Zero = new Vector2(0, 0);
    }

    public class GraphicsDeviceManager
    {
        public GraphicsDeviceManager(Game game)
        {

        }

        public int PreferredBackBufferHeight { get; set; }
        public int PreferredBackBufferWidth { get; set; }
        public bool IsFullScreen { get; set; }

        public Device GraphicsDevice { get; set; }
    }

    public class Device
    {
        public void Clear(Color color)
        {

        }

        public Texture2D Viewport { get; set; }
    }

    public class KeyboardState
    {
        public bool IsKeyUp(Keys k)
        {
            return false;
        }

        public bool IsKeyDown(Keys k)
        {
            return true;
        }
    }

    public class SpriteBatch
    {
        public SpriteBatch(Device device)
        {

        } 

        public void Begin(SpriteSortMode mode, BlendState state)
        {

        }

        public void Draw(Texture2D texture, Vector2 pos, Color color)
        {

        }

        public void End()
        {

        }
    }

    public class SpriteObject
    {
        public SpriteObject(Texture2D obj, Vector2 pos1, Vector2 pos2)
        {

        }
    }

    public class Texture2D
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class Keyboard
    {
        public static KeyboardState GetState()
        {
            return new KeyboardState();
        }
    }

    public class Content
    {
        public static string RootDirectory = "/";

        public static Texture2D Load<T>(string obj)
        {
            return new Texture2D();
        }
    }

    public enum SpriteSortMode { BackToFront }

    public enum BlendState { AlphaBlend }

    public class Program
    {
        public static void Main()
        {

        }
    }
}
