using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;

namespace FlappyBird
{
    public enum GameState
    {
        Menu,
        Settings,
        Playing,
        Paused,
        GameOver
    }

    public class Game
    {
        private int width;
        public int Width { get { return width; } }
        
        private int height;
        public int Height { get { return height; } }
        
        private int groundHeight;
        public int GroundHeight { get { return groundHeight; } }

        public GameState State { get; set; }
        public Bird Player { get; private set; }
        public List<Pipe> Pipes { get; private set; }
        public List<Particle> Particles { get; private set; }
        public List<Cloud> Clouds { get; private set; }
        public List<PointF> Stars { get; private set; }
        public ScoreManager Scores { get; private set; }

        // Difficulty Settings: 0 = Easy, 1 = Medium, 2 = Hard
        private int difficulty;
        public int Difficulty
        {
            get { return difficulty; }
            set
            {
                difficulty = value;
                ApplyDifficultySettings();
            }
        }

        // Difficulty parameters
        public float ScrollSpeed { get; private set; }
        public float PipeGapHeight { get; private set; }
        public float PipeSpawnRate { get; private set; } // Seconds between pipes

        private float pipeSpawnTimer;

        // Day/Night cycle
        private bool isNight;
        private float dayNightTimer;
        private float transitionProgress; // 0.0 = Day, 1.0 = Night
        private const float CycleDuration = 30f; // 30 seconds day, 30 seconds night
        private const float TransitionDuration = 4f; // 4 seconds to transition

        // Achievement popups
        public string ActiveAchievementText { get; private set; }
        private float achievementTimer;
        private const float AchievementDuration = 3.0f; // Seconds to show popup

        // Ground offset for scrolling effect
        private float groundOffset;

        private Random rand;

        public Game()
        {
            width = 480;
            height = 640;
            groundHeight = 80;

            State = GameState.Menu;
            Player = new Bird(100f, Height / 2f);
            Pipes = new List<Pipe>();
            Particles = new List<Particle>();
            Clouds = new List<Cloud>();
            Stars = new List<PointF>();
            Scores = new ScoreManager();
            
            // Subscribe to achievements
            Scores.OnAchievementUnlocked += TriggerAchievementPopup;

            difficulty = 1; // Medium
            pipeSpawnTimer = 0f;
            isNight = false;
            dayNightTimer = 0f;
            transitionProgress = 0f;
            ActiveAchievementText = "";
            achievementTimer = 0f;
            groundOffset = 0f;
            rand = new Random();

            // Generate initial clouds
            for (int i = 0; i < 4; i++)
            {
                Clouds.Add(new Cloud(
                    rand.Next(0, Width),
                    rand.Next(40, 180),
                    15f + (float)rand.NextDouble() * 20f,
                    0.6f + (float)rand.NextDouble() * 0.8f
                ));
            }

            // Generate starry sky coordinates
            for (int i = 0; i < 30; i++)
            {
                Stars.Add(new PointF(rand.Next(0, Width), rand.Next(0, Height - GroundHeight - 100)));
            }

            ApplyDifficultySettings();
        }

        private void ApplyDifficultySettings()
        {
            switch (difficulty)
            {
                case 0: // Easy
                    ScrollSpeed = 150f;
                    PipeGapHeight = 170f;
                    PipeSpawnRate = 2.4f;
                    Player.Gravity = 1000f;
                    Player.JumpStrength = -380f;
                    break;
                case 1: // Medium
                    ScrollSpeed = 200f;
                    PipeGapHeight = 140f;
                    PipeSpawnRate = 1.9f;
                    Player.Gravity = 1300f;
                    Player.JumpStrength = -420f;
                    break;
                case 2: // Hard
                    ScrollSpeed = 260f;
                    PipeGapHeight = 115f;
                    PipeSpawnRate = 1.4f;
                    Player.Gravity = 1600f;
                    Player.JumpStrength = -460f;
                    break;
            }
        }

        public void StartNewGame()
        {
            Player.Reset(100f, Height / 2f);
            Pipes.Clear();
            Particles.Clear();
            Scores.ResetScore();
            pipeSpawnTimer = PipeSpawnRate; // Spawn first pipe quickly
            State = GameState.Playing;
            dayNightTimer = 0f;
            isNight = false;
            transitionProgress = 0f;
            ActiveAchievementText = "";
            achievementTimer = 0f;
        }

        private void TriggerAchievementPopup(string name)
        {
            ActiveAchievementText = name;
            achievementTimer = AchievementDuration;
            SoundManager.PlayAchievement();
        }

        public void Update(float deltaTime)
        {
            // Update day/night cycle
            UpdateDayNight(deltaTime);

            // Update achievement timer
            if (achievementTimer > 0)
            {
                achievementTimer -= deltaTime;
                if (achievementTimer <= 0)
                {
                    ActiveAchievementText = "";
                }
            }

            // Update background clouds
            foreach (var cloud in Clouds)
            {
                cloud.X -= cloud.Speed * deltaTime;
                if (cloud.X < -150f * cloud.Scale)
                {
                    cloud.X = Width + 50f;
                    cloud.Y = rand.Next(40, 180);
                    cloud.Speed = 15f + (float)rand.NextDouble() * 20f;
                    cloud.Scale = 0.6f + (float)rand.NextDouble() * 0.8f;
                }
            }

            // If not playing, scroll screen ground and return
            if (State != GameState.Playing)
            {
                if (State == GameState.Menu || State == GameState.Settings)
                {
                    ScrollGround(deltaTime);
                }
                
                // Update particles in game over/menu
                UpdateParticles(deltaTime);
                return;
            }

            // Update Player physics
            Player.Update(deltaTime);

            // Boundary collision checks (ceiling & ground)
            if (Player.Y - Player.Radius <= 0)
            {
                Player.Y = Player.Radius;
                Player.Velocity = 0;
            }

            if (Player.Y + Player.Radius >= Height - GroundHeight)
            {
                HandlePlayerDeath();
                return;
            }

            // Scroll ground top line
            ScrollGround(deltaTime);

            // Update obstacles (pipes & coins)
            UpdatePipes(deltaTime);

            // Update particles
            UpdateParticles(deltaTime);
        }

        private void ScrollGround(float deltaTime)
        {
            groundOffset -= ScrollSpeed * deltaTime;
            if (groundOffset <= -24f) // Loop ground patterns every 24 pixels
            {
                groundOffset += 24f;
            }
        }

        private void UpdatePipes(float deltaTime)
        {
            pipeSpawnTimer += deltaTime;
            if (pipeSpawnTimer >= PipeSpawnRate)
            {
                // Decide whether to spawn a coin in the gap (40% chance)
                bool spawnCoin = rand.NextDouble() < 0.4;
                Pipes.Add(new Pipe(Width + 50f, Height, PipeGapHeight, spawnCoin));
                pipeSpawnTimer = 0f;
            }

            for (int i = Pipes.Count - 1; i >= 0; i--)
            {
                var pipe = Pipes[i];
                pipe.Update(ScrollSpeed, deltaTime);

                // Check collision with bird
                RectangleF topRect = new RectangleF(pipe.X, 0, pipe.Width, pipe.GapY);
                RectangleF bottomRect = new RectangleF(pipe.X, pipe.GapY + pipe.GapHeight, pipe.Width, Height - (pipe.GapY + pipe.GapHeight));

                if (Collision.CheckCircleVsRect(Player.X, Player.Y, Player.Radius - 1f, topRect) ||
                    Collision.CheckCircleVsRect(Player.X, Player.Y, Player.Radius - 1f, bottomRect))
                {
                    HandlePlayerDeath();
                    return;
                }

                // Coin collection check
                if (pipe.HasCoin && !pipe.CoinCollected)
                {
                    if (Collision.CheckCircleVsCircle(Player.X, Player.Y, Player.Radius, pipe.CoinX, pipe.CoinY, pipe.CoinRadius))
                    {
                        pipe.CoinCollected = true;
                        Scores.CollectCoin();
                        SoundManager.PlayCoin();
                        TriggerParticleBurst(pipe.CoinX, pipe.CoinY, Color.Gold, 12);
                    }
                }

                // Check scoring pass
                if (!pipe.Passed && pipe.X + pipe.Width < Player.X)
                {
                    pipe.Passed = true;
                    Scores.AddScore(1);
                    SoundManager.PlayScore();
                    // Gentle score particle splash at the pipe exit
                    TriggerParticleBurst(Player.X, Player.Y + 20, Color.FromArgb(200, 255, 255, 255), 6);
                }

                // Remove off-screen pipes
                if (pipe.X < -100f)
                {
                    Pipes.RemoveAt(i);
                }
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                p.Update(deltaTime);
                if (p.IsDead)
                {
                    Particles.RemoveAt(i);
                }
            }
        }

        private void UpdateDayNight(float deltaTime)
        {
            dayNightTimer += deltaTime;
            if (dayNightTimer >= CycleDuration)
            {
                isNight = !isNight;
                dayNightTimer = 0f;
            }

            // Smooth transition math
            if (isNight && transitionProgress < 1.0f)
            {
                transitionProgress = Math.Min(1.0f, transitionProgress + (deltaTime / TransitionDuration));
            }
            else if (!isNight && transitionProgress > 0.0f)
            {
                transitionProgress = Math.Max(0.0f, transitionProgress - (deltaTime / TransitionDuration));
            }
        }

        private void HandlePlayerDeath()
        {
            State = GameState.GameOver;
            SoundManager.PlayCollision();
            
            // Wait briefly then play game over tune
            Task.Run(async () =>
            {
                await Task.Delay(400);
                SoundManager.PlayGameOver();
            });

            TriggerParticleBurst(Player.X, Player.Y, Color.OrangeRed, 25);
        }

        private void TriggerParticleBurst(float x, float y, Color col, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Random velocities in circular spread
                double angle = rand.NextDouble() * Math.PI * 2;
                float speed = 50f + (float)rand.NextDouble() * 120f;
                float vx = (float)Math.Cos(angle) * speed;
                float vy = (float)Math.Sin(angle) * speed - 20f; // slight upwards push

                Particles.Add(new Particle(x, y, vx, vy, col, 0.4f + (float)rand.NextDouble() * 0.5f));
            }
        }

        public List<string> GetUnlockedBadges()
        {
            List<string> badges = new List<string>();
            if (Scores.FirstFlightUnlocked) badges.Add("First Flight");
            if (Scores.Score10Unlocked) badges.Add("Score 10 Badge");
            if (Scores.Score25Unlocked) badges.Add("Score 25 Badge");
            if (Scores.Score50Unlocked) badges.Add("Score 50 Badge");
            return badges;
        }

        #region Render System

        public void Draw(Graphics g)
        {
            // 1. Draw Day/Night sky vertical gradient background
            DrawSky(g);

            // 2. Draw stars if transition state is showing night
            if (transitionProgress > 0.05f)
            {
                DrawStars(g);
            }

            // 3. Draw parallax clouds
            DrawClouds(g);

            // 4. Draw game entities when active
            if (State == GameState.Playing || State == GameState.Paused || State == GameState.GameOver)
            {
                foreach (var pipe in Pipes)
                {
                    pipe.Draw(g, Height);
                }

                if (State != GameState.GameOver)
                {
                    Player.Draw(g);
                }
            }

            // 5. Draw particles
            foreach (var p in Particles)
            {
                p.Draw(g);
            }

            // 6. Draw scrollable ground
            DrawGround(g);
        }

        private void DrawSky(Graphics g)
        {
            // Define sky colors
            Color dayTop = Color.FromArgb(102, 178, 255);   // Soft Sky Blue
            Color dayBottom = Color.FromArgb(255, 204, 153); // Warm horizon peach

            Color nightTop = Color.FromArgb(12, 15, 36);      // Deep navy indigo
            Color nightBottom = Color.FromArgb(51, 0, 76);    // Dark twilight purple

            // Interpolate colors based on transition progress
            Color currentTop = BlendColors(dayTop, nightTop, transitionProgress);
            Color currentBottom = BlendColors(dayBottom, nightBottom, transitionProgress);

            using (var skyBrush = new LinearGradientBrush(
                new Rectangle(0, 0, Width, Height),
                currentTop, currentBottom, 90f))
            {
                g.FillRectangle(skyBrush, 0, 0, Width, Height);
            }
        }

        private void DrawStars(Graphics g)
        {
            // Fade stars in proportional to night cycle progress
            int alpha = (int)(255 * transitionProgress);
            if (alpha <= 0) return;

            // Twinkle rate based on system time ticks
            double twinkle = Math.Sin(DateTime.Now.Ticks * 0.000002) * 0.5 + 0.5;
            int starAlpha = (int)(alpha * (0.3 + 0.7 * twinkle));

            using (var starBrush = new SolidBrush(Color.FromArgb(starAlpha, Color.White)))
            {
                foreach (var star in Stars)
                {
                    g.FillRectangle(starBrush, star.X, star.Y, 2, 2);
                }
            }
        }

        private void DrawClouds(Graphics g)
        {
            foreach (var cloud in Clouds)
            {
                using (var path = new GraphicsPath())
                {
                    // Draw a cloud shape using overlapping ellipses
                    float cw = 60 * cloud.Scale;
                    float ch = 40 * cloud.Scale;
                    path.AddEllipse(cloud.X, cloud.Y, cw, ch);
                    path.AddEllipse(cloud.X + 20 * cloud.Scale, cloud.Y - 15 * cloud.Scale, cw * 1.1f, ch * 1.1f);
                    path.AddEllipse(cloud.X + 45 * cloud.Scale, cloud.Y, cw * 0.9f, ch * 0.9f);

                    // Soft semi-transparent white/gray cloud
                    using (var fill = new SolidBrush(Color.FromArgb(120, 245, 248, 255)))
                    {
                        g.FillPath(fill, path);
                    }
                }
            }
        }

        private void DrawGround(Graphics g)
        {
            float groundY = Height - GroundHeight;

            // Draw Ground background body (darker soil)
            using (var soilBrush = new LinearGradientBrush(
                new RectangleF(0, groundY + 10, Width, GroundHeight - 10),
                Color.FromArgb(100, 60, 20),
                Color.FromArgb(50, 30, 10),
                90f))
            {
                g.FillRectangle(soilBrush, 0, groundY + 10, Width, GroundHeight - 10);
            }

            // Draw grass border strip at top of ground
            using (var grassBrush = new LinearGradientBrush(
                new RectangleF(0, groundY, Width, 10),
                Color.FromArgb(120, 230, 50),
                Color.FromArgb(70, 150, 20),
                90f))
            {
                g.FillRectangle(grassBrush, 0, groundY, Width, 10);
            }
            g.DrawLine(Pens.Black, 0, groundY, Width, groundY);

            // Draw scrolling vertical texture indicators on the ground to show movement speed
            using (var linePen = new Pen(Color.FromArgb(50, 0, 0, 0), 2f))
            {
                for (float x = groundOffset; x < Width + 50; x += 24)
                {
                    g.DrawLine(linePen, x, groundY + 10, x - 10, Height);
                }
            }
        }

        private Color BlendColors(Color c1, Color c2, float ratio)
        {
            int r = (int)(c1.R + (c2.R - c1.R) * ratio);
            int g = (int)(c1.G + (c2.G - c1.G) * ratio);
            int b = (int)(c1.B + (c2.B - c1.B) * ratio);
            return Color.FromArgb(r, g, b);
        }

        #endregion

        public float GetAchievementProgress()
        {
            if (achievementTimer <= 0) return 0f;
            
            // Peak curve at center of display time
            if (achievementTimer > AchievementDuration - 0.5f)
            {
                return (AchievementDuration - achievementTimer) / 0.5f; // slide down
            }
            else if (achievementTimer < 0.5f)
            {
                return achievementTimer / 0.5f; // fade out
            }
            return 1.0f; // remain fully open
        }
    }

    #region Helper Classes (Particle, Cloud)

    public class Particle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }
        public Color BaseColor { get; private set; }
        public float MaxLifetime { get; private set; }
        public float Lifetime { get; private set; }
        public bool IsDead { get { return Lifetime <= 0f; } }

        private float rotationSpeed;
        private float angle;
        private Random rand;

        public Particle(float x, float y, float vx, float vy, Color col, float maxLife)
        {
            X = x;
            Y = y;
            Vx = vx;
            Vy = vy;
            BaseColor = col;
            MaxLifetime = maxLife;
            Lifetime = maxLife;
            
            angle = 0f;
            rand = new Random();
            rotationSpeed = (float)(rand.NextDouble() * 10 - 5);
        }

        public void Update(float deltaTime)
        {
            Lifetime -= deltaTime;

            // Gravity on particles
            Vy += 200f * deltaTime;

            X += Vx * deltaTime;
            Y += Vy * deltaTime;

            angle += rotationSpeed * deltaTime;
        }

        public void Draw(Graphics g)
        {
            if (IsDead) return;

            float progress = Lifetime / MaxLifetime;
            int alpha = (int)(255 * progress);
            if (alpha < 0) alpha = 0;

            float size = 4f + 6f * progress;

            using (var brush = new SolidBrush(Color.FromArgb(alpha, BaseColor)))
            {
                GraphicsState state = g.Save();
                g.TranslateTransform(X, Y);
                g.RotateTransform(angle * (180f / (float)Math.PI));
                
                g.FillRectangle(brush, -size / 2f, -size / 2f, size, size);

                g.Restore(state);
            }
        }
    }

    public class Cloud
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; set; }
        public float Scale { get; set; }

        public Cloud(float x, float y, float speed, float scale)
        {
            X = x;
            Y = y;
            Speed = speed;
            Scale = scale;
        }
    }

    #endregion
}
