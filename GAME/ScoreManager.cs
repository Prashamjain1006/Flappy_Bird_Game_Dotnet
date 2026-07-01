using System;
using System.IO;

namespace FlappyBird
{
    public class ScoreManager
    {
        private const string HighScoreFile = "highscore.txt";
        
        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public int CoinsCollected { get; private set; }

        // Achievements
        public bool FirstFlightUnlocked { get; private set; }
        public bool Score10Unlocked { get; private set; }
        public bool Score25Unlocked { get; private set; }
        public bool Score50Unlocked { get; private set; }

        // Events for UI display
        public event Action<string> OnAchievementUnlocked;

        public ScoreManager()
        {
            CurrentScore = 0;
            CoinsCollected = 0;
            LoadHighScore();
            ResetAchievements();
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            CoinsCollected = 0;
            ResetAchievements();
        }

        private void ResetAchievements()
        {
            FirstFlightUnlocked = false;
            Score10Unlocked = false;
            Score25Unlocked = false;
            Score50Unlocked = false;
        }

        public void AddScore(int amount)
        {
            CurrentScore += amount;
            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                SaveHighScore();
            }
            CheckAchievements();
        }

        public void CollectCoin()
        {
            CoinsCollected++;
            AddScore(2); // Bonus 2 points per coin
        }

        private void CheckAchievements()
        {
            var handler = OnAchievementUnlocked;

            if (!FirstFlightUnlocked && CurrentScore >= 1)
            {
                FirstFlightUnlocked = true;
                if (handler != null) handler("First Flight");
            }
            if (!Score10Unlocked && CurrentScore >= 10)
            {
                Score10Unlocked = true;
                if (handler != null) handler("Score 10 Badge");
            }
            if (!Score25Unlocked && CurrentScore >= 25)
            {
                Score25Unlocked = true;
                if (handler != null) handler("Score 25 Badge");
            }
            if (!Score50Unlocked && CurrentScore >= 50)
            {
                Score50Unlocked = true;
                if (handler != null) handler("Score 50 Badge");
            }
        }

        private void LoadHighScore()
        {
            try
            {
                if (File.Exists(HighScoreFile))
                {
                    string content = File.ReadAllText(HighScoreFile);
                    int score;
                    if (int.TryParse(content, out score))
                    {
                        HighScore = score;
                    }
                }
                else
                {
                    HighScore = 0;
                }
            }
            catch
            {
                HighScore = 0; // Fallback
            }
        }

        private void SaveHighScore()
        {
            try
            {
                File.WriteAllText(HighScoreFile, HighScore.ToString());
            }
            catch
            {
                // Fallback for file locks/permissions
            }
        }
    }
}
