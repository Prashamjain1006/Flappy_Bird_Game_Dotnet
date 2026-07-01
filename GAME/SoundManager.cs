using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace FlappyBird
{
    public static class SoundManager
    {
        // Sound settings properties
        public static int SoundVolume { get; set; }
        public static int MusicVolume { get; set; }
        public static bool SoundEnabled { get; set; }
        public static bool MusicEnabled { get; set; }

        private static SoundPlayer bgmPlayer;
        private static bool isBgmPlaying;
        private static byte[] cachedBgmData;

        // Sound effect bytes
        private static byte[] jumpWav;
        private static byte[] scoreWav;
        private static byte[] collisionWav;
        private static byte[] gameOverWav;
        private static byte[] coinWav;
        private static byte[] achievementWav;

        static SoundManager()
        {
            // Initialize settings defaults
            SoundVolume = 5;
            MusicVolume = 5;
            SoundEnabled = true;
            MusicEnabled = true;

            isBgmPlaying = false;
            cachedBgmData = null;
            bgmPlayer = null;

            RegenerateSounds();
        }

        // Re-generate all audio streams when volume/settings change
        public static void RegenerateSounds()
        {
            double sVol = (SoundVolume / 10.0) * 0.5; // Scale max volume to prevent clipping
            double mVol = (MusicVolume / 10.0) * 0.15; // Lower BGM default to avoid overpowering SFX

            // Generate SFX
            jumpWav = GenerateJumpSound(sVol);
            scoreWav = GenerateScoreSound(sVol);
            collisionWav = GenerateCollisionSound(sVol);
            gameOverWav = GenerateGameOverSound(sVol);
            coinWav = GenerateCoinSound(sVol);
            achievementWav = GenerateAchievementSound(sVol);

            // Generate BGM Loop
            cachedBgmData = GenerateBgmLoop(mVol);

            // Update background music player if active
            if (isBgmPlaying)
            {
                StopBgm();
                if (MusicEnabled && MusicVolume > 0)
                {
                    StartBgm();
                }
            }
        }

        public static void PlayJump() { PlaySoundBytes(jumpWav); }
        public static void PlayScore() { PlaySoundBytes(scoreWav); }
        public static void PlayCollision() { PlaySoundBytes(collisionWav); }
        public static void PlayGameOver() { PlaySoundBytes(gameOverWav); }
        public static void PlayCoin() { PlaySoundBytes(coinWav); }
        public static void PlayAchievement() { PlaySoundBytes(achievementWav); }

        private static void PlaySoundBytes(byte[] wavBytes)
        {
            if (!SoundEnabled || SoundVolume == 0 || wavBytes == null) return;
            
            Task.Run(() =>
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(wavBytes))
                    {
                        using (SoundPlayer player = new SoundPlayer(ms))
                        {
                            player.Play();
                        }
                    }
                }
                catch
                {
                    // Fallback to prevent crash if audio drivers are busy/missing
                }
            });
        }

        public static void StartBgm()
        {
            isBgmPlaying = true;
            if (!MusicEnabled || MusicVolume == 0 || cachedBgmData == null) return;

            Task.Run(() =>
            {
                try
                {
                    if (bgmPlayer != null)
                    {
                        bgmPlayer.Stop();
                        bgmPlayer.Dispose();
                    }
                    MemoryStream ms = new MemoryStream(cachedBgmData);
                    bgmPlayer = new SoundPlayer(ms);
                    bgmPlayer.PlayLooping();
                }
                catch
                {
                    // Fail silently if audio devices are unavailable
                }
            });
        }

        public static void StopBgm()
        {
            isBgmPlaying = false;
            try
            {
                if (bgmPlayer != null)
                {
                    bgmPlayer.Stop();
                    bgmPlayer.Dispose();
                    bgmPlayer = null;
                }
            }
            catch { }
        }

        #region Sound Effect Generators

        private static byte[] GenerateJumpSound(double volume)
        {
            int sampleRate = 11025;
            double duration = 0.08; // 80ms
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                // Linear frequency sweep from 350Hz up to 600Hz
                double freq = 350.0 + (600.0 - 350.0) * (t / duration);
                double angle = 2.0 * Math.PI * freq * t;
                samples[i] = (short)(Math.Sin(angle) * short.MaxValue * volume);
            }

            return CreateWavBytes(samples, sampleRate);
        }

        private static byte[] GenerateScoreSound(double volume)
        {
            int sampleRate = 11025;
            double duration = 0.25; // 250ms
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            int note1Samples = numSamples / 2;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double freq = i < note1Samples ? 880.0 : 1100.0; // Two notes: A5 then C#6
                double localT = i < note1Samples ? t : t - (duration / 2);
                double angle = 2.0 * Math.PI * freq * localT;
                // Exponential decay envelope
                double envelope = Math.Exp(-5.0 * localT);
                samples[i] = (short)(Math.Sin(angle) * envelope * short.MaxValue * volume);
            }

            return CreateWavBytes(samples, sampleRate);
        }

        private static byte[] GenerateCoinSound(double volume)
        {
            int sampleRate = 11025;
            double duration = 0.18;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            int split = numSamples / 3;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double freq = i < split ? 987.77 : 1318.51; // B5 to E6
                double localT = i < split ? t : t - ((double)split / sampleRate);
                double angle = 2.0 * Math.PI * freq * localT;
                double envelope = Math.Exp(-8.0 * localT);
                samples[i] = (short)(Math.Sin(angle) * envelope * short.MaxValue * volume);
            }

            return CreateWavBytes(samples, sampleRate);
        }

        private static byte[] GenerateCollisionSound(double volume)
        {
            int sampleRate = 11025;
            double duration = 0.25;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            Random rand = new Random();

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                // Slide down frequency sweep
                double freq = 220.0 - (180.0 * (t / duration));
                double angle = 2.0 * Math.PI * freq * t;
                // Add some white noise for a thud effect
                double noise = rand.NextDouble() * 2.0 - 1.0;
                double synth = Math.Sin(angle) * 0.7 + noise * 0.3;
                double envelope = Math.Exp(-4.0 * t);
                samples[i] = (short)(synth * envelope * short.MaxValue * volume);
            }

            return CreateWavBytes(samples, sampleRate);
        }

        private static byte[] GenerateGameOverSound(double volume)
        {
            int sampleRate = 11025;
            double duration = 0.8;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            int noteLength = numSamples / 4;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                int noteIndex = i / noteLength;
                // Minor key downward arpeggio: E4 -> C4 -> A3 -> E3
                double freq = 329.63; // E4
                if (noteIndex == 1) freq = 261.63; // C4
                else if (noteIndex == 2) freq = 220.00; // A3
                else if (noteIndex == 3) freq = 164.81; // E3

                double localT = (double)(i % noteLength) / sampleRate;
                double angle = 2.0 * Math.PI * freq * localT;
                double envelope = Math.Exp(-3.0 * localT);
                samples[i] = (short)(Math.Sin(angle) * envelope * short.MaxValue * volume);
            }

            return CreateWavBytes(samples, sampleRate);
        }

        private static byte[] GenerateAchievementSound(double volume)
        {
            int sampleRate = 11025;
            double duration = 0.5;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            int noteLength = numSamples / 4;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                int noteIndex = i / noteLength;
                // Major key upward chime: C5 -> E5 -> G5 -> C6
                double freq = 523.25; // C5
                if (noteIndex == 1) freq = 659.25; // E5
                else if (noteIndex == 2) freq = 783.99; // G5
                else if (noteIndex == 3) freq = 1046.50; // C6

                double localT = (double)(i % noteLength) / sampleRate;
                double angle = 2.0 * Math.PI * freq * localT;
                double envelope = Math.Exp(-4.0 * localT);
                samples[i] = (short)(Math.Sin(angle) * envelope * short.MaxValue * volume);
            }

            return CreateWavBytes(samples, sampleRate);
        }

        private static byte[] GenerateBgmLoop(double volume)
        {
            int sampleRate = 11025;
            double duration = 4.0; // 4 second chiptune loop
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            // A simple chiptune progression
            // Notes: Am, F, C, G (8 steps of 0.5s each)
            double[] notes = new double[] {
                220.00, 261.63, 329.63, 440.00, // Am (A3, C4, E4, A4)
                174.61, 220.00, 261.63, 349.23, // F  (F3, A3, C4, F4)
                261.63, 329.63, 392.00, 523.25, // C  (C4, E4, G4, C5)
                196.00, 246.94, 293.66, 392.00  // G  (G3, B3, D4, G4)
            };

            int stepCount = 8;
            double stepDuration = duration / stepCount; // 0.5s per step
            int samplesPerStep = (int)(sampleRate * stepDuration);

            for (int i = 0; i < numSamples; i++)
            {
                int step = i / samplesPerStep;
                double stepTime = (double)(i % samplesPerStep) / sampleRate;

                // Alternate chords
                int chordIndex = (step / 2) % 4; // 0=Am, 1=F, 2=C, 3=G
                
                // Arpeggio index (0, 1, 2, 3 notes cycling rapidly at 8Hz)
                int arpSpeed = 8; // notes per second
                int arpIndex = (int)(stepTime * arpSpeed) % 4;
                double freq = notes[chordIndex * 4 + arpIndex];

                double angle = 2.0 * Math.PI * freq * stepTime;
                
                // Triangle wave shape for a chiptune feel
                double triangleVal = Math.Abs((angle % (2.0 * Math.PI)) / Math.PI - 1.0) * 2.0 - 1.0;

                // Add a simple bass line on beat
                double bassFreq = notes[chordIndex * 4]; // Root note
                double bassAngle = 2.0 * Math.PI * bassFreq * stepTime;
                double bassVal = Math.Sin(bassAngle);
                
                double mix = triangleVal * 0.6 + bassVal * 0.4;
                samples[i] = (short)(mix * short.MaxValue * volume);
            }

            return CreateWavBytes(samples, sampleRate);
        }

        #endregion

        #region WAV Compilation Helper

        private static byte[] CreateWavBytes(short[] samples, int sampleRate)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    int subChunkSize = samples.Length * 2; // 16-bit = 2 bytes per sample
                    int chunkSize = 36 + subChunkSize;

                    // Header
                    bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                    bw.Write(chunkSize);
                    bw.Write(new char[] { 'W', 'A', 'V', 'E' });

                    // fmt subchunk
                    bw.Write(new char[] { 'f', 'm', 't', ' ' });
                    bw.Write(16); // Subchunk1Size (16 for PCM)
                    bw.Write((short)1); // AudioFormat (1 for PCM)
                    bw.Write((short)1); // NumChannels (1 mono)
                    bw.Write(sampleRate); // SampleRate
                    bw.Write(sampleRate * 2); // ByteRate (SampleRate * 1 channel * 2 bytes/sample)
                    bw.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8 = 1 * 2 = 2)
                    bw.Write((short)16); // BitsPerSample (16 bits)

                    // data subchunk
                    bw.Write(new char[] { 'd', 'a', 't', 'a' });
                    bw.Write(subChunkSize);

                    // Samples
                    for (int i = 0; i < samples.Length; i++)
                    {
                        bw.Write(samples[i]);
                    }

                    bw.Flush();
                    return ms.ToArray();
                }
            }
        }

        #endregion
    }
}
