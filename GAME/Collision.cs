using System;
using System.Drawing;

namespace FlappyBird
{
    public static class Collision
    {
        /// <summary>
        /// Check if a circular bird intersects with a rectangular obstacle.
        /// </summary>
        public static bool CheckCircleVsRect(float cx, float cy, float radius, RectangleF rect)
        {
            // Find the closest point on the rectangle to the circle's center
            float closestX = Math.Max(rect.X, Math.Min(cx, rect.X + rect.Width));
            float closestY = Math.Max(rect.Y, Math.Min(cy, rect.Y + rect.Height));

            // Calculate distance between the circle's center and this closest point
            float dx = cx - closestX;
            float dy = cy - closestY;
            float distanceSquared = (dx * dx) + (dy * dy);

            // If the distance is less than the circle's radius, they intersect
            return distanceSquared < (radius * radius);
        }

        /// <summary>
        /// Check if two circles intersect (bird vs coin).
        /// </summary>
        public static bool CheckCircleVsCircle(float c1x, float c1y, float r1, float c2x, float c2y, float r2)
        {
            float dx = c1x - c2x;
            float dy = c1y - c2y;
            float distanceSquared = (dx * dx) + (dy * dy);
            float radiusSum = r1 + r2;

            return distanceSquared < (radiusSum * radiusSum);
        }
    }
}
