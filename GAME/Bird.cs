using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlappyBird
{
    public enum BirdSkin
    {
        ClassicYellow,
        CyberBlue,
        RubyRed,
        GoldenPhoenix
    }

    public class Bird
    {
        public float X { get; set; }
        public float Y { get; set; }
        
        private float radius;
        public float Radius { get { return radius; } }
        
        public float Velocity { get; set; }
        public float Gravity { get; set; }
        public float JumpStrength { get; set; }
        public float Rotation { get; private set; } // Radians

        public BirdSkin SelectedSkin { get; set; }
        
        // Wing Flap animation variables
        private int flapState; // 0 = up, 1 = mid, 2 = down, 3 = mid
        private float animationTimer;
        private const float FlapSpeed = 0.08f; // Seconds per frame

        public Bird(float startX, float startY)
        {
            X = startX;
            Y = startY;
            radius = 15f;
            Velocity = 0f;
            Rotation = 0f;
            
            // Default physics (Medium)
            Gravity = 1300f;
            JumpStrength = -420f;
            SelectedSkin = BirdSkin.ClassicYellow;
            
            flapState = 0;
            animationTimer = 0f;
        }

        public void Reset(float startX, float startY)
        {
            X = startX;
            Y = startY;
            Velocity = 0;
            Rotation = 0;
            flapState = 0;
            animationTimer = 0f;
        }

        public void Jump()
        {
            Velocity = JumpStrength;
            SoundManager.PlayJump();
        }

        public void Update(float deltaTime)
        {
            // Apply gravity
            Velocity += Gravity * deltaTime;
            Y += Velocity * deltaTime;

            // Smooth rotation based on velocity
            float targetRotation = (float)Math.Max(-0.5, Math.Min(Velocity * 0.002f, 1.2));
            Rotation = Rotation * 0.8f + targetRotation * 0.2f;

            // Flap animation
            animationTimer += deltaTime;
            if (animationTimer >= FlapSpeed)
            {
                flapState = (flapState + 1) % 4;
                animationTimer = 0f;
            }
        }

        public void Draw(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Save graphics state
            GraphicsState state = g.Save();

            // Move origin to bird center and rotate
            g.TranslateTransform(X, Y);
            g.RotateTransform(Rotation * (180f / (float)Math.PI));

            // Draw based on selected skin
            switch (SelectedSkin)
            {
                case BirdSkin.ClassicYellow:
                    DrawClassicYellow(g);
                    break;
                case BirdSkin.CyberBlue:
                    DrawCyberBlue(g);
                    break;
                case BirdSkin.RubyRed:
                    DrawRubyRed(g);
                    break;
                case BirdSkin.GoldenPhoenix:
                    DrawGoldenPhoenix(g);
                    break;
            }

            // Restore graphics state
            g.Restore(state);
        }

        #region Skin Drawers

        private void DrawClassicYellow(Graphics g)
        {
            // Body
            using (var bodyBrush = new LinearGradientBrush(
                new RectangleF(-Radius, -Radius, Radius * 2, Radius * 2),
                Color.Gold, Color.Orange, 45f))
            {
                g.FillEllipse(bodyBrush, -Radius, -Radius, Radius * 2, Radius * 2);
                g.DrawEllipse(Pens.DarkOrange, -Radius, -Radius, Radius * 2, Radius * 2);
            }

            // Eye
            float eyeRadius = 5f;
            g.FillEllipse(Brushes.White, 3, -6, eyeRadius * 2, eyeRadius * 2);
            g.DrawEllipse(Pens.Black, 3, -6, eyeRadius * 2, eyeRadius * 2);
            // Pupil
            g.FillEllipse(Brushes.Black, 6, -4, 3, 3);

            // Beak
            PointF[] beakPoints = new PointF[]
            {
                new PointF(Radius - 3, -3),
                new PointF(Radius + 8, 2),
                new PointF(Radius - 3, 5)
            };
            g.FillPolygon(Brushes.OrangeRed, beakPoints);
            g.DrawPolygon(Pens.DarkRed, beakPoints);

            // Wing (based on flap state)
            DrawWing(g, Color.Yellow, Color.Goldenrod);
        }

        private void DrawCyberBlue(Graphics g)
        {
            // Draw radial glow via concentric semi-transparent circles
            for (int r = 1; r <= 6; r++)
            {
                int alpha = (int)(25 * (1f - (r / 6f)));
                using (var glowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 255, 255)))
                {
                    float size = (Radius + r) * 2f;
                    g.FillEllipse(glowBrush, -Radius - r, -Radius - r, size, size);
                }
            }

            // Body
            using (var bodyBrush = new LinearGradientBrush(
                new RectangleF(-Radius, -Radius, Radius * 2, Radius * 2),
                Color.FromArgb(10, 30, 80), Color.FromArgb(0, 191, 255), 45f))
            {
                g.FillEllipse(bodyBrush, -Radius, -Radius, Radius * 2, Radius * 2);
                using (var glowPen = new Pen(Color.Cyan, 2f))
                {
                    g.DrawEllipse(glowPen, -Radius, -Radius, Radius * 2, Radius * 2);
                }
            }

            // Eye (cybernetic glowing visor/eye)
            g.FillEllipse(Brushes.Cyan, 4, -5, 8, 6);
            g.FillEllipse(Brushes.White, 7, -3, 3, 2); // Glint

            // Beak
            PointF[] beakPoints = new PointF[]
            {
                new PointF(Radius - 3, -2),
                new PointF(Radius + 7, 1),
                new PointF(Radius - 3, 3)
            };
            g.FillPolygon(Brushes.Magenta, beakPoints);
            g.DrawPolygon(Pens.DeepPink, beakPoints);

            // Wing
            DrawWing(g, Color.Cyan, Color.FromArgb(0, 100, 200));
        }

        private void DrawRubyRed(Graphics g)
        {
            // Body
            using (var bodyBrush = new LinearGradientBrush(
                new RectangleF(-Radius, -Radius, Radius * 2, Radius * 2),
                Color.Crimson, Color.Maroon, 45f))
            {
                g.FillEllipse(bodyBrush, -Radius, -Radius, Radius * 2, Radius * 2);
                g.DrawEllipse(Pens.Black, -Radius, -Radius, Radius * 2, Radius * 2);
            }

            // Eye
            float eyeRadius = 5f;
            g.FillEllipse(Brushes.White, 2, -6, eyeRadius * 2, eyeRadius * 2);
            g.DrawEllipse(Pens.Black, 2, -6, eyeRadius * 2, eyeRadius * 2);
            g.FillEllipse(Brushes.Black, 4, -4, 3, 3);

            // Fierce Angry Eyebrow
            using (var eyebrowPen = new Pen(Color.Black, 2f))
            {
                g.DrawLine(eyebrowPen, 0, -8, 8, -5);
            }

            // Beak
            PointF[] beakPoints = new PointF[]
            {
                new PointF(Radius - 3, -3),
                new PointF(Radius + 8, 2),
                new PointF(Radius - 3, 5)
            };
            g.FillPolygon(Brushes.Yellow, beakPoints);
            g.DrawPolygon(Pens.DarkGoldenrod, beakPoints);

            // Wing
            DrawWing(g, Color.Red, Color.DarkRed);
        }

        private void DrawGoldenPhoenix(Graphics g)
        {
            // Golden Aura glow via concentric semi-transparent circles
            for (int r = 1; r <= 8; r++)
            {
                int alpha = (int)(30 * (1f - (r / 8f)));
                using (var glowBrush = new SolidBrush(Color.FromArgb(alpha, 255, 215, 0)))
                {
                    float size = (Radius + r) * 2f;
                    g.FillEllipse(glowBrush, -Radius - r, -Radius - r, size, size);
                }
            }

            // Body
            using (var bodyBrush = new LinearGradientBrush(
                new RectangleF(-Radius, -Radius, Radius * 2, Radius * 2),
                Color.Orange, Color.Yellow, 90f))
            {
                g.FillEllipse(bodyBrush, -Radius, -Radius, Radius * 2, Radius * 2);
                using (var goldPen = new Pen(Color.Gold, 2.5f))
                {
                    g.DrawEllipse(goldPen, -Radius, -Radius, Radius * 2, Radius * 2);
                }
            }

            // Eye
            float eyeRadius = 4.5f;
            g.FillEllipse(Brushes.White, 3, -5, eyeRadius * 2, eyeRadius * 2);
            g.DrawEllipse(Pens.OrangeRed, 3, -5, eyeRadius * 2, eyeRadius * 2);
            g.FillEllipse(Brushes.DarkRed, 5, -3.5f, 2.5f, 2.5f);

            // Beak
            PointF[] beakPoints = new PointF[]
            {
                new PointF(Radius - 3, -3),
                new PointF(Radius + 8, 1),
                new PointF(Radius - 3, 4)
            };
            g.FillPolygon(Brushes.Gold, beakPoints);
            g.DrawPolygon(Pens.DarkGoldenrod, beakPoints);

            // Wing (Fancy layered wing for phoenix)
            DrawWing(g, Color.Gold, Color.OrangeRed);

            // Tiny golden crown on top of head
            PointF[] crownPoints = new PointF[]
            {
                new PointF(-7, -Radius),
                new PointF(-9, -Radius - 6),
                new PointF(-3, -Radius - 3),
                new PointF(0, -Radius - 8),
                new PointF(3, -Radius - 3),
                new PointF(9, -Radius - 6),
                new PointF(7, -Radius)
            };
            g.FillPolygon(Brushes.Gold, crownPoints);
            g.DrawPolygon(Pens.OrangeRed, crownPoints);
        }

        private void DrawWing(Graphics g, Color primary, Color secondary)
        {
            // Position of wing relative to body shifts up and down based on flap state
            float wingYOffset = 0f;
            float wingHeightScale = 1f;

            switch (flapState)
            {
                case 0: // Wing Up
                    wingYOffset = -4f;
                    wingHeightScale = 0.5f;
                    break;
                case 1: // Wing Mid-Up
                case 3: // Wing Mid-Down
                    wingYOffset = 0f;
                    wingHeightScale = 0.9f;
                    break;
                case 2: // Wing Down
                    wingYOffset = 4f;
                    wingHeightScale = 0.5f;
                    break;
            }

            using (var wingBrush = new LinearGradientBrush(
                new RectangleF(-10, -8 + wingYOffset, 12, 12 * wingHeightScale),
                primary, secondary, 90f))
            {
                g.FillEllipse(wingBrush, -11, -6 + wingYOffset, 12, 12 * wingHeightScale);
                g.DrawEllipse(Pens.Black, -11, -6 + wingYOffset, 12, 12 * wingHeightScale);
            }
        }

        #endregion
    }
}
