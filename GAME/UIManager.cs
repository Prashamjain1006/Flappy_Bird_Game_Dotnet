using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FlappyBird
{
    public class UIManager
    {
        // Interactive Button Rectangles (scaled for standard window dimensions 480x640)
        public RectangleF PlayBtn { get; private set; }
        public RectangleF SettingsBtn { get; private set; }
        public RectangleF BackBtn { get; private set; }
        
        // Settings page buttons
        public RectangleF SoundMinusBtn { get; private set; }
        public RectangleF SoundPlusBtn { get; private set; }
        public RectangleF MusicMinusBtn { get; private set; }
        public RectangleF MusicPlusBtn { get; private set; }
        public RectangleF EasyBtn { get; private set; }
        public RectangleF MediumBtn { get; private set; }
        public RectangleF HardBtn { get; private set; }

        // Skins page buttons
        public RectangleF SkinPrevBtn { get; private set; }
        public RectangleF SkinNextBtn { get; private set; }

        // Game Over buttons
        public RectangleF RestartBtn { get; private set; }
        public RectangleF MenuBtn { get; private set; }

        // Current hovered button identifier
        public string HoveredButton { get; set; }

        // Transition fading
        public float FadeAlpha { get; set; }
        public bool IsFading { get; set; }
        private float fadeTarget;
        private float fadeSpeed; // Alpha units per second
        private Action onFadeComplete;

        public UIManager()
        {
            // Initialize defaults (C# 5 compat)
            HoveredButton = "";
            FadeAlpha = 255f;
            IsFading = false;
            fadeTarget = 0f;
            fadeSpeed = 500f;
            onFadeComplete = null;

            // Initialize button layout bounds
            float cx = 480f / 2f;

            PlayBtn = new RectangleF(cx - 90, 320, 180, 50);
            SettingsBtn = new RectangleF(cx - 90, 390, 180, 50);
            BackBtn = new RectangleF(20, 20, 80, 35);

            // Settings buttons
            SoundMinusBtn = new RectangleF(cx + 10, 190, 35, 30);
            SoundPlusBtn = new RectangleF(cx + 105, 190, 35, 30);
            MusicMinusBtn = new RectangleF(cx + 10, 240, 35, 30);
            MusicPlusBtn = new RectangleF(cx + 105, 240, 35, 30);

            EasyBtn = new RectangleF(cx - 150, 340, 90, 35);
            MediumBtn = new RectangleF(cx - 45, 340, 90, 35);
            HardBtn = new RectangleF(cx + 60, 340, 90, 35);

            // Skins buttons
            SkinPrevBtn = new RectangleF(cx - 140, 210, 40, 40);
            SkinNextBtn = new RectangleF(cx + 100, 210, 40, 40);

            // Game over buttons
            RestartBtn = new RectangleF(cx - 130, 460, 110, 40);
            MenuBtn = new RectangleF(cx + 20, 460, 110, 40);
        }

        public void UpdateTransition(float deltaTime)
        {
            if (!IsFading) return;

            if (FadeAlpha < fadeTarget)
            {
                FadeAlpha = Math.Min(255f, FadeAlpha + fadeSpeed * deltaTime);
                if (FadeAlpha >= fadeTarget)
                {
                    IsFading = false;
                    if (onFadeComplete != null) onFadeComplete();
                }
            }
            else if (FadeAlpha > fadeTarget)
            {
                FadeAlpha = Math.Max(0f, FadeAlpha - fadeSpeed * deltaTime);
                if (FadeAlpha <= fadeTarget)
                {
                    IsFading = false;
                    if (onFadeComplete != null) onFadeComplete();
                }
            }
        }

        public void StartFade(float target, float speed, Action onComplete)
        {
            fadeTarget = target;
            fadeSpeed = speed;
            onFadeComplete = onComplete;
            IsFading = true;
        }

        #region Render Methods

        public void DrawMainMenu(Graphics g, int width, int height, float pulseTick, BirdSkin selectedSkin)
        {
            // Title text (Pulsing neon shadow)
            float scale = 1.0f + (float)Math.Sin(pulseTick * 0.1f) * 0.04f;
            using (var titleFont = new Font("Arial Black", 32f * scale, FontStyle.Bold))
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, width, 120), Color.Gold, Color.OrangeRed, 90f))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                // Draw neon cyan glow behind
                using (var glowBrush = new SolidBrush(Color.FromArgb(80, 0, 191, 255)))
                {
                    g.DrawString("NEON FLAP", titleFont, glowBrush, new RectangleF(2, 90 + 2, width, 80), format);
                }
                g.DrawString("NEON FLAP", titleFont, brush, new RectangleF(0, 90, width, 80), format);
            }

            // Draw skin showcase
            DrawSkinShowcase(g, width / 2f, 230, selectedSkin);

            // Draw Skins Navigation arrows
            DrawArrowButton(g, SkinPrevBtn, "<", HoveredButton == "SkinPrev");
            DrawArrowButton(g, SkinNextBtn, ">", HoveredButton == "SkinNext");

            // Draw Buttons
            DrawModernButton(g, PlayBtn, "PLAY GAME", Color.LimeGreen, Color.DarkGreen, HoveredButton == "Play");
            DrawModernButton(g, SettingsBtn, "SETTINGS", Color.DeepSkyBlue, Color.RoyalBlue, HoveredButton == "Settings");

            // Instruction subtitle
            using (var subFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                StringFormat subFormat = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("CHOOSE SKIN WITH ARROWS • PRESS PLAY TO FLAP", subFont, Brushes.LightCyan, width / 2f, 480, subFormat);
            }
        }

        private void DrawSkinShowcase(Graphics g, float cx, float cy, BirdSkin skin)
        {
            // Draw glass card background for showcase
            RectangleF card = new RectangleF(cx - 70, cy - 45, 140, 90);
            DrawGlassCard(g, card, Color.FromArgb(40, 255, 255, 255), Color.Cyan);

            // Draw bird representation
            Bird dummy = new Bird(cx, cy - 5);
            dummy.SelectedSkin = skin;
            dummy.Draw(g);

            // Skin Name Label
            using (var nameFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                string label = "";
                Color labelColor = Color.White;
                switch (skin)
                {
                    case BirdSkin.ClassicYellow: label = "CLASSIC BIRD"; labelColor = Color.Gold; break;
                    case BirdSkin.CyberBlue: label = "CYBER BIRD"; labelColor = Color.Cyan; break;
                    case BirdSkin.RubyRed: label = "RUBY RAVEN"; labelColor = Color.Crimson; break;
                    case BirdSkin.GoldenPhoenix: label = "PHOENIX"; labelColor = Color.Yellow; break;
                }

                StringFormat f = new StringFormat { Alignment = StringAlignment.Center };
                using (var brush = new SolidBrush(labelColor))
                {
                    g.DrawString(label, nameFont, brush, cx, cy + 26, f);
                }
            }
        }

        public void DrawSettingsMenu(Graphics g, int width, int height, int difficulty)
        {
            float cx = width / 2f;

            // Draw Page Title
            using (var font = new Font("Arial Black", 24f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("SETTINGS", font, Brushes.White, cx, 80, f);
            }

            // Draw Glass Background Card for settings panel
            RectangleF panelRect = new RectangleF(cx - 180, 140, 360, 270);
            DrawGlassCard(g, panelRect, Color.FromArgb(30, 255, 255, 255), Color.DeepSkyBlue);

            // Volume Settings UI
            using (var labelFont = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var valueFont = new Font("Segoe UI Semibold", 12f))
            {
                // Sound Volume
                g.DrawString("SOUND SFX:", labelFont, Brushes.LightCyan, cx - 150, 192);
                g.DrawString(SoundManager.SoundVolume.ToString(), valueFont, Brushes.White, cx + 65, 192);
                DrawMiniButton(g, SoundMinusBtn, "-", HoveredButton == "SoundMinus");
                DrawMiniButton(g, SoundPlusBtn, "+", HoveredButton == "SoundPlus");

                // Music Volume
                g.DrawString("MUSIC BGM:", labelFont, Brushes.LightCyan, cx - 150, 242);
                g.DrawString(SoundManager.MusicVolume.ToString(), valueFont, Brushes.White, cx + 65, 242);
                DrawMiniButton(g, MusicMinusBtn, "-", HoveredButton == "MusicMinus");
                DrawMiniButton(g, MusicPlusBtn, "+", HoveredButton == "MusicPlus");
            }

            // Draw Divider Line
            using (var pen = new Pen(Color.FromArgb(100, 0, 191, 255), 1.5f))
            {
                g.DrawLine(pen, cx - 150, 300, cx + 150, 300);
            }

            // Difficulty Settings UI
            using (var sectionFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                g.DrawString("DIFFICULTY SPEED:", sectionFont, Brushes.LightCyan, cx - 150, 312);
            }

            DrawDifficultyButton(g, EasyBtn, "EASY", difficulty == 0, HoveredButton == "Easy");
            DrawDifficultyButton(g, MediumBtn, "MEDIUM", difficulty == 1, HoveredButton == "Medium");
            DrawDifficultyButton(g, HardBtn, "HARD", difficulty == 2, HoveredButton == "Hard");

            // Back button
            DrawBackButton(g, BackBtn, HoveredButton == "Back");
        }

        public void DrawGameHUD(Graphics g, int width, int height, int score, int highScore, int coins, bool isDay, float timeRemaining, string popText, float popProgress)
        {
            // Draw active score at the top center (Big and beautiful)
            using (var font = new Font("Arial Black", 36f, FontStyle.Bold))
            using (var outlinePath = new GraphicsPath())
            {
                StringFormat f = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near
                };

                outlinePath.AddString(score.ToString(), font.FontFamily, (int)font.Style, font.Size, new RectangleF(0, 30, width, 80), f);
                
                // Neon glow fill
                g.FillPath(Brushes.White, outlinePath);
                g.DrawPath(new Pen(Color.Black, 3f), outlinePath);
            }

            // High Score (top right) & Coin counter (top left)
            using (var hudFont = new Font("Segoe UI Black", 12f, FontStyle.Bold))
            {
                // Coins
                string coinStr = "COINS: " + coins;
                g.DrawString(coinStr, hudFont, Brushes.Gold, 15, 15);

                // High Score
                string hsStr = "BEST: " + highScore;
                SizeF size = g.MeasureString(hsStr, hudFont);
                g.DrawString(hsStr, hudFont, Brushes.OrangeRed, width - size.Width - 15, 15);
            }

            // Draw Achievement Unlock Popup overlay (slides down from top, then fades)
            if (!string.IsNullOrEmpty(popText) && popProgress > 0)
            {
                DrawAchievementPopup(g, width, popText, popProgress);
            }
        }

        public void DrawPauseScreen(Graphics g, int width, int height)
        {
            // Darken background slightly
            using (var dimBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(dimBrush, 0, 0, width, height);
            }

            float cx = width / 2f;
            float cy = height / 2f;

            // Draw Paused Card
            RectangleF pauseCard = new RectangleF(cx - 150, cy - 80, 300, 160);
            DrawGlassCard(g, pauseCard, Color.FromArgb(60, 255, 255, 255), Color.Orange);

            // Title
            using (var titleFont = new Font("Arial Black", 24f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("GAME PAUSED", titleFont, Brushes.Orange, cx, cy - 60, f);
            }

            // Subtitle instructions
            using (var subFont = new Font("Segoe UI Semibold", 10f))
            {
                StringFormat f = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("Press ESC to Resume\nPress R to Restart Game\nPress M for Main Menu", subFont, Brushes.White, cx, cy - 10, f);
            }
        }

        public void DrawGameOverScreen(Graphics g, int width, int height, int score, int highScore, int coinsCollected, bool isNewBest, List<string> badges)
        {
            // Darken background
            using (var dimBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            {
                g.FillRectangle(dimBrush, 0, 0, width, height);
            }

            float cx = width / 2f;

            // Title "GAME OVER" (sliding glowing pulse)
            using (var titleFont = new Font("Arial Black", 32f, FontStyle.Bold))
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, width, 120), Color.Red, Color.DarkRed, 90f))
            {
                StringFormat f = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                // Shadow glow
                using (var glowBrush = new SolidBrush(Color.FromArgb(80, 255, 0, 0)))
                {
                    g.DrawString("GAME OVER", titleFont, glowBrush, new RectangleF(2, 60 + 2, width, 85), f);
                }
                g.DrawString("GAME OVER", titleFont, brush, new RectangleF(0, 60, width, 85), f);
            }

            // Stats Board (Glass Card)
            RectangleF statsCard = new RectangleF(cx - 160, 160, 320, 190);
            DrawGlassCard(g, statsCard, Color.FromArgb(50, 255, 255, 255), isNewBest ? Color.Gold : Color.Red);

            using (var labelFont = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var valFont = new Font("Segoe UI Black", 16f, FontStyle.Bold))
            {
                // Score
                g.DrawString("FINAL SCORE:", labelFont, Brushes.LightCyan, cx - 130, 185);
                g.DrawString(score.ToString(), valFont, Brushes.White, cx + 50, 180);

                // High score
                g.DrawString("BEST SCORE:", labelFont, Brushes.LightCyan, cx - 130, 235);
                g.DrawString(highScore.ToString(), valFont, isNewBest ? Brushes.Gold : Brushes.White, cx + 50, 230);

                // Coins
                g.DrawString("COINS PILED:", labelFont, Brushes.LightCyan, cx - 130, 285);
                g.DrawString("+" + coinsCollected, valFont, Brushes.Gold, cx + 50, 280);

                if (isNewBest)
                {
                    // Draw a miniature "NEW BEST" star label
                    using (var alertFont = new Font("Segoe UI Black", 8f, FontStyle.Italic))
                    {
                        g.DrawString("NEW BEST!", alertFont, Brushes.Gold, cx + 105, 220);
                    }
                }
            }

            // Badges Section
            DrawBadgesRow(g, cx, 370, badges);

            // Replay Buttons
            DrawModernButton(g, RestartBtn, "RESTART", Color.LimeGreen, Color.DarkGreen, HoveredButton == "Restart");
            DrawModernButton(g, MenuBtn, "MENU", Color.Orange, Color.DarkGoldenrod, HoveredButton == "Menu");

            // Subtitle
            using (var restartFont = new Font("Segoe UI Semibold", 9f))
            {
                StringFormat f = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("OR PRESS SPACEBAR TO QUICK RESTART", restartFont, Brushes.Silver, cx, 520, f);
            }
        }

        private void DrawBadgesRow(Graphics g, float cx, float cy, List<string> badges)
        {
            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("BADGES UNLOCKED:", font, Brushes.LightSkyBlue, cx, cy, f);

                if (badges == null || badges.Count == 0)
                {
                    using (var emptyFont = new Font("Segoe UI", 9f, FontStyle.Italic))
                    {
                        g.DrawString("No badges earned yet.", emptyFont, Brushes.Gray, cx, cy + 18, f);
                    }
                    return;
                }

                // Center the badges horizontally
                float spacing = 65f;
                float startX = cx - ((badges.Count - 1) * spacing) / 2f;

                for (int i = 0; i < badges.Count; i++)
                {
                    float bx = startX + i * spacing;
                    float by = cy + 30;

                    // Draw Badge shape
                    using (var auraBrush = new SolidBrush(Color.FromArgb(80, 255, 215, 0)))
                    {
                        g.FillEllipse(auraBrush, bx - 16, by - 16, 32, 32);
                    }
                    g.FillEllipse(Brushes.Gold, bx - 12, by - 12, 24, 24);
                    g.DrawEllipse(Pens.DarkGoldenrod, bx - 12, by - 12, 24, 24);

                    // Tiny design inside
                    g.DrawEllipse(Pens.White, bx - 8, by - 8, 16, 16);

                    // Badge Text underneath
                    string shortName = badges[i].Replace(" Badge", "");
                    using (var txtFont = new Font("Segoe UI Black", 6.5f))
                    {
                        g.DrawString(shortName, txtFont, Brushes.Yellow, bx, by + 18, f);
                    }
                }
            }
        }

        private void DrawAchievementPopup(Graphics g, int width, string text, float progress)
        {
            // progress runs from 0.0 (hidden) to 1.0 (fully displayed slide-down) to 0.0 (fade-out)
            float popupY = -60f + 90f * progress; // Slide down 30 pixels from top
            float cx = width / 2f;

            RectangleF popupRect = new RectangleF(cx - 150, popupY, 300, 45);
            
            // Draw glowing orange glass popup card
            int alpha = (int)(255 * progress);
            DrawGlassCard(g, popupRect, Color.FromArgb(Math.Min(200, alpha), 0, 0, 0), Color.FromArgb(alpha, 255, 215, 0));

            // Icon
            g.FillEllipse(Brushes.Gold, cx - 130, popupY + 12, 22, 22);
            g.DrawEllipse(Pens.Black, cx - 130, popupY + 12, 22, 22);

            // Chime text
            using (var titleFont = new Font("Segoe UI Black", 8f, FontStyle.Bold))
            using (var descFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.FromArgb(alpha, Color.White)))
            using (var goldBrush = new SolidBrush(Color.FromArgb(alpha, Color.Gold)))
            {
                g.DrawString("ACHIEVEMENT UNLOCKED!", titleFont, goldBrush, cx - 100, popupY + 6);
                g.DrawString(text, descFont, textBrush, cx - 100, popupY + 20);
            }
        }

        public void DrawTransitionOverlay(Graphics g, int width, int height)
        {
            if (FadeAlpha <= 0) return;
            using (var brush = new SolidBrush(Color.FromArgb((int)FadeAlpha, 0, 0, 0)))
            {
                g.FillRectangle(brush, 0, 0, width, height);
            }
        }

        #endregion

        #region Custom GDI+ Button Drawers

        private void DrawModernButton(Graphics g, RectangleF rect, string text, Color topColor, Color bottomColor, bool isHovered)
        {
            float drawScale = isHovered ? 1.04f : 1.0f;
            
            // Apply hover scale from center of the button
            GraphicsState state = g.Save();
            g.TranslateTransform(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            g.ScaleTransform(drawScale, drawScale);
            g.TranslateTransform(-(rect.Width / 2f), -(rect.Height / 2f));

            RectangleF localRect = new RectangleF(0, 0, rect.Width, rect.Height);

            // Draw drop shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, CreateRoundedRectPath(new RectangleF(localRect.X + 2, localRect.Y + 3, localRect.Width, localRect.Height), 8));
            }

            // Fill gradient
            Color tc = isHovered ? ControlPaint.Light(topColor, 0.2f) : topColor;
            Color bc = isHovered ? ControlPaint.Light(bottomColor, 0.2f) : bottomColor;

            using (var brush = new LinearGradientBrush(localRect, tc, bc, 90f))
            {
                var path = CreateRoundedRectPath(localRect, 8);
                g.FillPath(brush, path);
                
                // Border glow
                using (var pen = new Pen(isHovered ? Color.White : Color.FromArgb(180, tc), 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Text
            using (var font = new Font("Arial Black", 12f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(text, font, Brushes.White, localRect, f);
            }

            g.Restore(state);
        }

        private void DrawArrowButton(Graphics g, RectangleF rect, string text, bool isHovered)
        {
            // A rounded square card with a clean vector chevron
            using (var path = CreateRoundedRectPath(rect, 8))
            {
                Color border = isHovered ? Color.Cyan : Color.FromArgb(120, 0, 191, 255);
                using (var brush = new SolidBrush(isHovered ? Color.FromArgb(60, 0, 191, 255) : Color.FromArgb(30, 255, 255, 255)))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(border, 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            using (var font = new Font("Segoe UI Black", 14f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(text, font, Brushes.White, rect, f);
            }
        }

        private void DrawMiniButton(Graphics g, RectangleF rect, string text, bool isHovered)
        {
            using (var path = CreateRoundedRectPath(rect, 6))
            {
                Color fill = isHovered ? Color.FromArgb(100, 0, 191, 255) : Color.FromArgb(50, 0, 0, 50);
                g.FillPath(new SolidBrush(fill), path);
                g.DrawPath(new Pen(Color.FromArgb(150, 0, 191, 255), 1.5f), path);
            }

            using (var font = new Font("Segoe UI Black", 12f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(text, font, Brushes.White, rect, f);
            }
        }

        private void DrawDifficultyButton(Graphics g, RectangleF rect, string text, bool isSelected, bool isHovered)
        {
            using (var path = CreateRoundedRectPath(rect, 6))
            {
                Color fill = isSelected ? Color.Gold : (isHovered ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(20, 255, 255, 255));
                Color border = isSelected ? Color.OrangeRed : (isHovered ? Color.Cyan : Color.Gray);
                
                using (var brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(border, 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            using (var font = new Font("Segoe UI Black", 9f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                Brush tBrush = isSelected ? Brushes.Black : Brushes.White;
                g.DrawString(text, font, tBrush, rect, f);
            }
        }

        private void DrawBackButton(Graphics g, RectangleF rect, bool isHovered)
        {
            using (var path = CreateRoundedRectPath(rect, 6))
            {
                Color fill = isHovered ? Color.Crimson : Color.FromArgb(30, 255, 255, 255);
                g.FillPath(new SolidBrush(fill), path);
                g.DrawPath(new Pen(isHovered ? Color.White : Color.Gray, 1.5f), path);
            }

            using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                StringFormat f = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("BACK", font, Brushes.White, rect, f);
            }
        }

        private void DrawGlassCard(Graphics g, RectangleF rect, Color fill, Color borderGlow)
        {
            // Glassmorphism card drawing helper
            using (var path = CreateRoundedRectPath(rect, 12))
            {
                using (var brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(borderGlow, 1.8f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        #endregion

        #region Mouse Collisions Hit Test

        public string HitTest(Point location, string screenState)
        {
            if (screenState == "Menu")
            {
                if (PlayBtn.Contains(location)) return "Play";
                if (SettingsBtn.Contains(location)) return "Settings";
                if (SkinPrevBtn.Contains(location)) return "SkinPrev";
                if (SkinNextBtn.Contains(location)) return "SkinNext";
            }
            else if (screenState == "Settings")
            {
                if (BackBtn.Contains(location)) return "Back";
                if (SoundMinusBtn.Contains(location)) return "SoundMinus";
                if (SoundPlusBtn.Contains(location)) return "SoundPlus";
                if (MusicMinusBtn.Contains(location)) return "MusicMinus";
                if (MusicPlusBtn.Contains(location)) return "MusicPlus";
                if (EasyBtn.Contains(location)) return "Easy";
                if (MediumBtn.Contains(location)) return "Medium";
                if (HardBtn.Contains(location)) return "Hard";
            }
            else if (screenState == "GameOver")
            {
                if (RestartBtn.Contains(location)) return "Restart";
                if (MenuBtn.Contains(location)) return "Menu";
            }

            return "";
        }

        #endregion
    }
}
