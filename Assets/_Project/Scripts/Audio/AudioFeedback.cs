using UnityEngine;

namespace ZeroGCargo.Audio
{
    /// <summary>Original synthesized chime and boing clips, built once and reused.</summary>
    public sealed class AudioFeedback
    {
        private readonly AudioSource source;
        private readonly AudioClip chime;
        private readonly AudioClip boing;
        /// <summary>Creates two short original placeholder sounds.</summary>
        public AudioFeedback(GameObject owner)
        {
            source = owner.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = 0.13f;
            chime = Tone("Original dock chime", false); boing = Tone("Original collision boing", true);
        }
        private static AudioClip Tone(string name, bool falling)
        {
            const int rate = 22050; const float duration = 0.4f;
            var samples = new float[(int)(rate * duration)]; float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float frequency = falling ? Mathf.Lerp(260, 85, t / duration) : 1046;
                phase += frequency / rate * Mathf.PI * 2;
                samples[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 10) * Mathf.Min(1, t * 100);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        /// <summary>Plays the docking chime when enabled.</summary>
        public void Dock(bool enabled) { if (enabled) source.PlayOneShot(chime); }
        /// <summary>Plays the collision boing when enabled.</summary>
        public void Collision(bool enabled) { if (enabled) source.PlayOneShot(boing); }
        /// <summary>Releases generated clips.</summary>
        public void Dispose() { Object.Destroy(chime); Object.Destroy(boing); }
    }
}
