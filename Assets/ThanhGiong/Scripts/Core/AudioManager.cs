using UnityEngine;

namespace ThanhGiong
{
    public enum Sfx { Swing, Hit, Fire, EnemyDie, Hurt, Gong, Pickup, Arrow, Slam, Dash, Click, Block, Victory, Defeat, Warn }

    /// <summary>
    /// Âm thanh tổng hợp bằng code (procedural audio): mọi hiệu ứng và nhạc nền đều được tạo
    /// từ sóng sin, nhiễu trắng… khi khởi động, nên không cần file âm thanh bên ngoài.
    /// Nhạc nền dùng thang âm ngũ cung kết hợp tiếng trống trầm gợi không khí trống đồng.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SR = 22050;
        AudioClip[] clips;
        AudioClip music;
        AudioSource[] pool;
        AudioSource musicSource;
        int next;
        readonly System.Random rng = new(7);

        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        void Awake()
        {
            pool = new AudioSource[20];
            for (int i = 0; i < pool.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool[i] = s;
            }
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.22f;
            Generate();
        }

        public void Play(Sfx sfx, float volume = 1f, float pitchVariation = 0.08f)
        {
            var clip = clips[(int)sfx];
            if (clip == null) return;
            var s = pool[next];
            next = (next + 1) % pool.Length;
            s.clip = clip;
            s.volume = volume;
            s.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            s.Play();
        }

        public void PlayMusic()
        {
            if (musicSource.isPlaying) return;
            musicSource.clip = music;
            musicSource.Play();
        }

        // ------------------------------------------------------------------ Tổng hợp âm thanh
        static AudioClip Make(string name, float duration, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(SR * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Generate()
        {
            clips = new AudioClip[System.Enum.GetValues(typeof(Sfx)).Length];
            const float TwoPi = Mathf.PI * 2f;
            float lp = 0f, ph = 0f;

            clips[(int)Sfx.Swing] = Make("swing", 0.2f, t =>
            {
                float k = Mathf.Lerp(0.05f, 0.35f, t / 0.2f);
                lp += (Noise() - lp) * k;
                return lp * Mathf.Sin(Mathf.PI * t / 0.2f) * 1.8f;
            });

            ph = 0f;
            clips[(int)Sfx.Hit] = Make("hit", 0.22f, t =>
            {
                ph += TwoPi * Mathf.Lerp(180f, 60f, t / 0.22f) / SR;
                return Mathf.Sin(ph) * Mathf.Exp(-t * 18f) * 0.9f + Noise() * Mathf.Exp(-t * 45f) * 0.5f;
            });

            lp = 0f;
            clips[(int)Sfx.Fire] = Make("fire", 0.8f, t =>
            {
                lp += (Noise() - lp) * 0.08f;
                float crackle = rng.NextDouble() < 0.003 ? Noise() * 0.7f : 0f;
                return (lp * 2.4f + crackle) * Mathf.Min(1f, t / 0.05f) * Mathf.Exp(-t * 3f);
            });

            ph = 0f;
            clips[(int)Sfx.EnemyDie] = Make("die", 0.4f, t =>
            {
                ph += TwoPi * Mathf.Lerp(420f, 110f, t / 0.4f) / SR;
                float s = Mathf.Sin(ph);
                return (s + 0.3f * Mathf.Sign(s)) * 0.45f * Mathf.Exp(-t * 6f);
            });

            ph = 0f;
            clips[(int)Sfx.Hurt] = Make("hurt", 0.3f, t =>
            {
                ph += TwoPi * Mathf.Lerp(150f, 90f, t / 0.3f) / SR;
                return (Mathf.Sign(Mathf.Sin(ph)) * 0.35f + Noise() * 0.2f) * Mathf.Exp(-t * 10f);
            });

            float[] gp = { 1f, 1.47f, 2.09f, 2.56f, 3.42f };
            float[] ga = { 1f, 0.6f, 0.45f, 0.3f, 0.2f };
            float[] gd = { 1.2f, 1.8f, 2.4f, 3f, 4f };
            clips[(int)Sfx.Gong] = Make("gong", 3f, t =>
            {
                float v = 0f;
                for (int i = 0; i < gp.Length; i++) v += Mathf.Sin(TwoPi * 98f * gp[i] * t) * ga[i] * Mathf.Exp(-t * gd[i]);
                return v * 0.45f * Mathf.Min(1f, t / 0.01f);
            });

            float[] up = { 523.25f, 587.33f, 698.46f, 783.99f, 880f };
            clips[(int)Sfx.Pickup] = Make("pickup", 0.42f, t =>
            {
                int idx = Mathf.Min(4, (int)(t / 0.07f));
                float lt = t - idx * 0.07f;
                return Mathf.Sin(TwoPi * up[idx] * t) * Mathf.Exp(-lt * 12f) * 0.5f;
            });

            lp = 0f;
            clips[(int)Sfx.Arrow] = Make("arrow", 0.15f, t =>
            {
                float n = Noise();
                lp += (n - lp) * 0.3f;
                return (n - lp) * Mathf.Exp(-t * 25f) * 0.8f + Mathf.Sin(TwoPi * 300f * t) * Mathf.Exp(-t * 30f) * 0.3f;
            });

            lp = 0f; ph = 0f;
            clips[(int)Sfx.Slam] = Make("slam", 0.9f, t =>
            {
                ph += TwoPi * Mathf.Lerp(70f, 35f, t / 0.9f) / SR;
                lp += (Noise() - lp) * 0.05f;
                return Mathf.Sin(ph) * Mathf.Exp(-t * 4f) + lp * Mathf.Exp(-t * 6f) * 2.5f;
            });

            lp = 0f;
            clips[(int)Sfx.Dash] = Make("dash", 0.18f, t =>
            {
                lp += (Noise() - lp) * Mathf.Lerp(0.1f, 0.5f, t / 0.18f);
                return lp * Mathf.Sin(Mathf.PI * t / 0.18f) * 1.4f;
            });

            clips[(int)Sfx.Click] = Make("click", 0.06f, t => Mathf.Sin(TwoPi * 900f * t) * Mathf.Exp(-t * 40f) * 0.5f);

            clips[(int)Sfx.Block] = Make("block", 0.2f, t =>
                (Mathf.Sin(TwoPi * 1200f * t) + Mathf.Sin(TwoPi * 1730f * t) + Noise() * 0.5f) * Mathf.Exp(-t * 30f) * 0.35f);

            clips[(int)Sfx.Warn] = Make("warn", 0.35f, t =>
                Mathf.Sin(TwoPi * 220f * t + 4f * Mathf.Sin(TwoPi * 6f * t)) * Mathf.Exp(-t * 6f) * 0.5f);

            clips[(int)Sfx.Victory] = Melody("victory", new[] { 523.25f, 659.25f, 783.99f, 880f, 1046.5f }, new[] { 0.18f, 0.18f, 0.18f, 0.18f, 0.9f });
            clips[(int)Sfx.Defeat] = Melody("defeat", new[] { 440f, 392f, 329.63f, 261.63f }, new[] { 0.35f, 0.35f, 0.35f, 1.2f });
            music = MakeMusic();
        }

        static AudioClip Melody(string name, float[] notes, float[] durations)
        {
            float total = 0f;
            foreach (float d in durations) total += d;
            return Make(name, total + 0.3f, t =>
            {
                float start = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    if (t < start + durations[i] || i == notes.Length - 1)
                    {
                        float lt = t - start;
                        float tri = Mathf.PingPong(t * notes[i] * 2f, 1f) * 2f - 1f;
                        return (tri * 0.4f + Mathf.Sin(Mathf.PI * 2f * notes[i] * 2f * t) * 0.15f)
                               * Mathf.Min(1f, lt / 0.01f) * Mathf.Exp(-lt * 2.5f);
                    }
                    start += durations[i];
                }
                return 0f;
            });
        }

        /// <summary>Nhạc nền lặp: trống trầm + giai điệu ngũ cung kiểu gảy dây (gợi đàn bầu/đàn tranh).</summary>
        static AudioClip MakeMusic()
        {
            const float bpm = 104f;
            float beat = 60f / bpm;
            int beats = 16;
            int n = Mathf.CeilToInt(beat * beats * SR);
            var data = new float[n];

            void Drum(float time, float freq, float decay, float amp)
            {
                int start = (int)(time * SR);
                float phase = 0f;
                int len = (int)(0.6f * SR);
                for (int i = 0; i < len; i++)
                {
                    float lt = i / (float)SR;
                    phase += Mathf.PI * 2f * freq * (1f + 1.5f * Mathf.Exp(-lt * 25f)) / SR;
                    data[(start + i) % n] += Mathf.Sin(phase) * Mathf.Exp(-lt * decay) * amp;
                }
            }

            void Pluck(float time, float freq, float length, float amp)
            {
                int start = (int)(time * SR);
                int len = (int)(length * SR);
                float phase = 0f;
                for (int i = 0; i < len; i++)
                {
                    float lt = i / (float)SR;
                    float f = freq * (1f + 0.012f * Mathf.Sin(Mathf.PI * 2f * 5f * lt) * Mathf.Min(1f, lt * 3f));
                    phase += Mathf.PI * 2f * f / SR;
                    float v = Mathf.Sin(phase) + 0.4f * Mathf.Sin(phase * 2f) + 0.15f * Mathf.Sin(phase * 3f);
                    data[(start + i) % n] += v * Mathf.Exp(-lt * 3.5f) * Mathf.Min(1f, lt / 0.005f) * amp;
                }
            }

            for (int b = 0; b < beats; b++)
            {
                float t = b * beat;
                if (b % 4 == 0 || b % 4 == 2) Drum(t, 62f, 7f, 0.9f);
                if (b % 8 == 7) Drum(t + beat * 0.5f, 62f, 7f, 0.6f);
                Drum(t + beat * 0.5f, 190f, 22f, 0.22f);
                if (b % 4 == 0) Pluck(t, 73.42f, beat * 3.5f, 0.35f); // bè trầm
            }

            // Thang ngũ cung: Rê – Fa – Sol – La – Đô
            float D4 = 293.66f, F4 = 349.23f, G4 = 392f, A4 = 440f, C5 = 523.25f, D5 = 587.33f;
            (float beatPos, float freq, float len)[] melody =
            {
                (0f, D4, 1f), (1f, F4, 0.5f), (1.5f, G4, 0.5f), (2f, A4, 1.5f), (3.5f, G4, 0.5f),
                (4f, F4, 1f), (5f, G4, 0.5f), (5.5f, F4, 0.5f), (6f, D4, 2f),
                (8f, A4, 1f), (9f, C5, 0.5f), (9.5f, D5, 0.5f), (10f, C5, 1f), (11f, A4, 1f),
                (12f, G4, 1f), (13f, A4, 0.5f), (13.5f, G4, 0.5f), (14f, F4, 0.5f), (14.5f, G4, 0.5f), (15f, D4, 1f)
            };
            foreach (var m in melody) Pluck(m.beatPos * beat, m.freq, Mathf.Max(0.6f, m.len * beat * 1.6f), 0.28f);

            float max = 0.0001f;
            for (int i = 0; i < n; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            float norm = 0.9f / max;
            for (int i = 0; i < n; i++) data[i] *= norm;

            var clip = AudioClip.Create("music", n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
