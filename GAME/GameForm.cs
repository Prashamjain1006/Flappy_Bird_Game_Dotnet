using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

namespace FlappyBird
{
    public class GameForm : Form
    {
        private Game game;
        private UIManager ui;
        private Timer loopTimer;
        private Stopwatch stopwatch;
        private float lastTime = 0f;

        // Current transition target
        private GameState transitionTargetState;
        private bool isTransitioning = false;

        public GameForm()
        {
            // Set form characteristics
            this.Text = "Neon Flap";
            this.ClientSize = new Size(480, 640);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Double buffering is essential for smooth 60 FPS drawing
            this.DoubleBuffered = true;

            // Initialize game entities
            game = new Game();
            ui = new UIManager();

            // Set up High Precision Stopwatch
            stopwatch = new Stopwatch();
            stopwatch.Start();

            // Event Hookups
            this.Paint += GameForm_Paint;
            this.KeyDown += GameForm_KeyDown;
            this.MouseDown += GameForm_MouseDown;
            this.MouseMove += GameForm_MouseMove;
            this.FormClosing += GameForm_FormClosing;
            this.Load += GameForm_Load;

            // Game main loop timer (~60 FPS)
            loopTimer = new Timer();
            loopTimer.Interval = 16; // 16ms = ~62.5 FPS
            loopTimer.Tick += GameLoop_Tick;
            loopTimer.Start();
        }

        private void GameForm_Load(object sender, EventArgs e)
        {
            // Start BGM immediately
            SoundManager.StartBgm();
            
            // Start fading in the menu
            ui.StartFade(0f, 400f, null);
        }

        private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop playing music to release audio resources
            SoundManager.StopBgm();
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            // High-precision Delta Time calculation
            float currentTime = stopwatch.ElapsedMilliseconds / 1000f;
            float deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            // Clamp delta time to avoid large physics steps when the window is dragged
            if (deltaTime > 0.1f) deltaTime = 0.1f;

            // Update transition fades
            ui.UpdateTransition(deltaTime);

            // Update game physics & animations
            game.Update(deltaTime);

            // Request frame repaint
            this.Invalidate();
        }

        private void GameForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draw game background, entities, and foreground ground
            game.Draw(g);

            // Render HUD and Menu Overlays based on state
            switch (game.State)
            {
                case GameState.Menu:
                    ui.DrawMainMenu(g, this.ClientSize.Width, this.ClientSize.Height, stopwatch.ElapsedMilliseconds * 0.05f, game.Player.SelectedSkin);
                    break;
                case GameState.Settings:
                    ui.DrawSettingsMenu(g, this.ClientSize.Width, this.ClientSize.Height, game.Difficulty);
                    break;
                case GameState.Playing:
                    float popProgress = game.GetAchievementProgress();
                    ui.DrawGameHUD(g, this.ClientSize.Width, this.ClientSize.Height, game.Scores.CurrentScore, game.Scores.HighScore, game.Scores.CoinsCollected, true, 0, game.ActiveAchievementText, popProgress);
                    break;
                case GameState.Paused:
                    // Keep HUD visible under pause blur
                    ui.DrawGameHUD(g, this.ClientSize.Width, this.ClientSize.Height, game.Scores.CurrentScore, game.Scores.HighScore, game.Scores.CoinsCollected, true, 0, "", 0f);
                    ui.DrawPauseScreen(g, this.ClientSize.Width, this.ClientSize.Height);
                    break;
                case GameState.GameOver:
                    // Keep HUD visible under game over blur
                    ui.DrawGameHUD(g, this.ClientSize.Width, this.ClientSize.Height, game.Scores.CurrentScore, game.Scores.HighScore, game.Scores.CoinsCollected, true, 0, "", 0f);
                    
                    bool isNewBest = game.Scores.CurrentScore == game.Scores.HighScore && game.Scores.CurrentScore > 0;
                    ui.DrawGameOverScreen(g, this.ClientSize.Width, this.ClientSize.Height, game.Scores.CurrentScore, game.Scores.HighScore, game.Scores.CoinsCollected, isNewBest, game.GetUnlockedBadges());
                    break;
            }

            // Draw screen transition overlay (black fade)
            ui.DrawTransitionOverlay(g, this.ClientSize.Width, this.ClientSize.Height);
        }

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (ui.IsFading) return;

            if (e.KeyCode == Keys.Space)
            {
                if (game.State == GameState.Playing)
                {
                    game.Player.Jump();
                }
                else if (game.State == GameState.Menu)
                {
                    FadeToState(GameState.Playing);
                }
                else if (game.State == GameState.GameOver)
                {
                    FadeToState(GameState.Playing);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (game.State == GameState.Playing)
                {
                    game.State = GameState.Paused;
                }
                else if (game.State == GameState.Paused)
                {
                    game.State = GameState.Playing;
                }
            }
            else if (e.KeyCode == Keys.R && game.State == GameState.Paused)
            {
                FadeToState(GameState.Playing);
            }
            else if (e.KeyCode == Keys.M && game.State == GameState.Paused)
            {
                FadeToState(GameState.Menu);
            }
        }

        private void GameForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (ui.IsFading) return;

            // In playing state, left click flaps the wings
            if (game.State == GameState.Playing)
            {
                if (e.Button == MouseButtons.Left)
                {
                    game.Player.Jump();
                }
                return;
            }

            // Hit test buttons in other screens
            string hit = ui.HitTest(e.Location, game.State.ToString());
            if (string.IsNullOrEmpty(hit)) return;

            // Perform actions based on buttons
            switch (hit)
            {
                case "Play":
                    FadeToState(GameState.Playing);
                    break;
                case "Settings":
                    FadeToState(GameState.Settings);
                    break;
                case "Back":
                    FadeToState(GameState.Menu);
                    break;
                case "Restart":
                    FadeToState(GameState.Playing);
                    break;
                case "Menu":
                    FadeToState(GameState.Menu);
                    break;
                case "SkinPrev":
                    CycleSkin(-1);
                    break;
                case "SkinNext":
                    CycleSkin(1);
                    break;
                case "SoundMinus":
                    SoundManager.SoundVolume = Math.Max(0, SoundManager.SoundVolume - 1);
                    SoundManager.RegenerateSounds();
                    SoundManager.PlayScore(); // play test sound
                    break;
                case "SoundPlus":
                    SoundManager.SoundVolume = Math.Min(10, SoundManager.SoundVolume + 1);
                    SoundManager.RegenerateSounds();
                    SoundManager.PlayScore(); // play test sound
                    break;
                case "MusicMinus":
                    SoundManager.MusicVolume = Math.Max(0, SoundManager.MusicVolume - 1);
                    SoundManager.RegenerateSounds();
                    break;
                case "MusicPlus":
                    SoundManager.MusicVolume = Math.Min(10, SoundManager.MusicVolume + 1);
                    SoundManager.RegenerateSounds();
                    break;
                case "Easy":
                    game.Difficulty = 0;
                    SoundManager.PlayJump();
                    break;
                case "Medium":
                    game.Difficulty = 1;
                    SoundManager.PlayJump();
                    break;
                case "Hard":
                    game.Difficulty = 2;
                    SoundManager.PlayJump();
                    break;
            }
        }

        private void GameForm_MouseMove(object sender, MouseEventArgs e)
        {
            // Hover check
            string hit = ui.HitTest(e.Location, game.State.ToString());
            
            if (ui.HoveredButton != hit)
            {
                ui.HoveredButton = hit;
                
                // Repaint to update button hover states
                this.Invalidate();
            }
        }

        private void FadeToState(GameState targetState)
        {
            transitionTargetState = targetState;
            ui.StartFade(255f, 600f, OnFadeOutComplete);
        }

        private void OnFadeOutComplete()
        {
            // Transition game state
            game.State = transitionTargetState;

            if (transitionTargetState == GameState.Playing)
            {
                // Reset entities for a fresh restart
                game.StartNewGame();
            }

            // Fade back in
            ui.StartFade(0f, 600f, null);
        }

        private void CycleSkin(int dir)
        {
            int maxSkins = Enum.GetValues(typeof(BirdSkin)).Length;
            int nextSkin = (int)game.Player.SelectedSkin + dir;
            
            if (nextSkin < 0) nextSkin = maxSkins - 1;
            if (nextSkin >= maxSkins) nextSkin = 0;

            game.Player.SelectedSkin = (BirdSkin)nextSkin;
            SoundManager.PlayJump();
        }
    }
}
