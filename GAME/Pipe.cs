using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlappyBird
{
    public class Pipe
    {
        public float X { get; set; }
        public float GapY { get; private set; } // Vertical starting Y of the gap
        
        public float GapHeight { get; set; }
        
        private float width;
        public float Width { get { return width; } }
        
        public bool Passed { get; set; }

        // Coin properties
        public bool HasCoin { get; private set; }
        public bool CoinCollected { get; set; }
        
        public float CoinX { get { return X + Width / 2f; } }
        public float CoinY { get { return GapY + GapHeight / 2f; } }
        
        private float coinRadius;
        public float CoinRadius { get { return coinRadius; } }
        
        private float coinSpinAngle;

        private static Random rand = new Random();

        public Pipe(float startX, float screenHeight, float gapHeight, bool spawnCoin)
        {
            X = startX;
            GapHeight = gapHeight;
            HasCoin = spawnCoin;
            
            width = 70f;
            coinRadius = 10f;
            coinSpinAngle = 0f;
            Passed = false;
            CoinCollected = false;

            // Randomly position the gap
            // Ensure the gap is not too close to the top or bottom of the screen
            float minGapY = 60f;
            float maxGapY = screenHeight - gapHeight - 150f; // Leaves room for ground
            GapY = (float)(minGapY + rand.NextDouble() * (maxGapY - minGapY));
        }

        public void Update(float speed, float deltaTime)
        {
            // Move pipe left
            X -= speed * deltaTime;

            // Spin coin
            if (HasCoin && !CoinCollected)
            {
                coinSpinAngle += 5f * deltaTime; // Speed of spin
                if (coinSpinAngle > (float)Math.PI * 2)
                {
                    coinSpinAngle -= (float)Math.PI * 2;
                }
            }
        }

        public void Draw(Graphics g, float screenHeight)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Top Pipe
            DrawPipeColumn(g, X, 0, Width, GapY, true);

            // Draw Bottom Pipe
            float bottomY = GapY + GapHeight;
            DrawPipeColumn(g, X, bottomY, Width, screenHeight - bottomY, false);

            // Draw Coin if it has one and hasn't been collected
            if (HasCoin && !CoinCollected)
            {
                DrawSpinningCoin(g, CoinX, CoinY);
            }
        }

        private void DrawPipeColumn(Graphics g, float x, float y, float w, float h, bool isTop)
        {
            if (h <= 0) return;

            // Pipe Body gradient (glossy green cylinder look)
            using (var pipeBrush = new LinearGradientBrush(
                new RectangleF(x, y, w, h),
                Color.FromArgb(50, 205, 50), // LimeGreen
                Color.FromArgb(0, 100, 0),    // DarkGreen
                0f))
            {
                // Custom color blend for a shiny cylindrical highlights
                ColorBlend blend = new ColorBlend();
                blend.Colors = new Color[] { 
                    Color.FromArgb(34, 139, 34),  // ForestGreen
                    Color.FromArgb(144, 238, 144), // LightGreen (shine)
                    Color.FromArgb(50, 205, 50),   // LimeGreen
                    Color.FromArgb(0, 100, 0)      // DarkGreen (shadow)
                };
                blend.Positions = new float[] { 0.0f, 0.2f, 0.5f, 1.0f };
                pipeBrush.InterpolationColors = blend;

                g.FillRectangle(pipeBrush, x, y, w, h);
                g.DrawRectangle(Pens.Black, x, y, w, h);
            }

            // Draw Pipe Cap
            float capHeight = 24f;
            float capWidth = w + 8f;
            float capX = x - 4f;
            float capY = isTop ? (y + h - capHeight) : y;

            using (var capBrush = new LinearGradientBrush(
                new RectangleF(capX, capY, capWidth, capHeight),
                Color.FromArgb(50, 205, 50),
                Color.FromArgb(0, 80, 0),
                0f))
            {
                ColorBlend blend = new ColorBlend();
                blend.Colors = new Color[] { 
                    Color.FromArgb(50, 220, 50),
                    Color.FromArgb(170, 255, 170),
                    Color.FromArgb(50, 205, 50),
                    Color.FromArgb(0, 80, 0)
                };
                blend.Positions = new float[] { 0.0f, 0.25f, 0.6f, 1.0f };
                capBrush.InterpolationColors = blend;

                g.FillRectangle(capBrush, capX, capY, capWidth, capHeight);
                g.DrawRectangle(Pens.Black, capX, capY, capWidth, capHeight);
            }
        }

        private void DrawSpinningCoin(Graphics g, float cx, float cy)
        {
            // Simulate 3D rotation by scaling width using Sine
            float scaleX = (float)Math.Abs(Math.Sin(coinSpinAngle));
            if (scaleX < 0.05f) scaleX = 0.05f; // Prevent 0 width glitches

            float drawWidth = CoinRadius * 2f * scaleX;
            float drawHeight = CoinRadius * 2f;

            // Draw glowing gold aura
            using (var auraBrush = new SolidBrush(Color.FromArgb(60, 255, 215, 0)))
            {
                g.FillEllipse(auraBrush, cx - drawWidth / 2f - 4, cy - drawHeight / 2f - 4, drawWidth + 8, drawHeight + 8);
            }

            // Coin Body
            using (var coinBrush = new LinearGradientBrush(
                new RectangleF(cx - drawWidth / 2f, cy - drawHeight / 2f, drawWidth, drawHeight),
                Color.Yellow, Color.Gold, 45f))
            {
                g.FillEllipse(coinBrush, cx - drawWidth / 2f, cy - drawHeight / 2f, drawWidth, drawHeight);
                using (var coinPen = new Pen(Color.DarkGoldenrod, 1.5f))
                {
                    g.DrawEllipse(coinPen, cx - drawWidth / 2f, cy - drawHeight / 2f, drawWidth, drawHeight);
                }
            }

            // Inner circle highlight (spinning)
            float innerW = drawWidth * 0.6f;
            float innerH = drawHeight * 0.6f;
            if (innerW > 1)
            {
                g.DrawEllipse(Pens.Goldenrod, cx - innerW / 2f, cy - innerH / 2f, innerW, innerH);
                
                // Write 'C' for Coin
                using (var font = new Font("Arial", 8f * scaleX, FontStyle.Bold))
                {
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("C", font, Brushes.DarkGoldenrod, new RectangleF(cx - drawWidth/2, cy - drawHeight/2, drawWidth, drawHeight), format);
                }
            }
        }
    }
}
