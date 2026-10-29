using UnityEngine;

namespace ZeroGCargo.Audio
{
    /// <summary>Original synthesized ambience, flight tones and event sounds, allocated once.</summary>
    public sealed class AudioFeedback
    {
        private readonly AudioSource effects;
        private readonly AudioSource flight;
        private readonly AudioSource music;
        private readonly AudioClip chime;
        private readonly AudioClip boing;
        private readonly AudioClip tick;
        private readonly AudioClip whoosh;
        private readonly AudioClip ambience;
        private bool paused = true;
        /// <summary>Creates original reusable clips and playback channels.</summary>
        public AudioFeedback(GameObject owner)
        {
            effects = owner.AddComponent<AudioSource>(); effects.playOnAwake = false; effects.volume = .13f;
            flight = owner.AddComponent<AudioSource>(); flight.playOnAwake = false; flight.loop = true; flight.volume = 0;
            music = owner.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true; music.volume = .045f;
            chime = Tone("Dock C6",0); boing = Tone("Comic boing",1); tick = Tone("Near miss tick",2); whoosh = Tone("Flight harmonic",3); ambience = Tone("Life support ambience",4);
            flight.clip = whoosh; flight.Play(); music.clip = ambience;
        }
        private static AudioClip Tone(string name,int kind)
        {
            const int rate = 22050; float duration = kind >= 3 ? 2 : kind == 2 ? .08f : .4f;
            var samples = new float[(int)(rate*duration)]; float phase = 0;
            for(int i=0;i<samples.Length;i++)
            {
                float t = (float)i/rate;
                float frequency = kind == 0 ? 1046 : kind == 1 ? Mathf.Lerp(260,85,t/duration) : kind == 2 ? 1500 : kind == 3 ? 220 : 55;
                phase += frequency/rate*Mathf.PI*2;
                float envelope = kind >= 3 ? 1 : Mathf.Exp(-t*(kind == 2 ? 60 : 10))*Mathf.Min(1,t*100);
                samples[i] = Mathf.Sin(phase)*envelope*(kind == 4 ? .6f : 1);
                if(kind == 4) samples[i] += Mathf.Sin(t*Mathf.PI*2*110)*.12f;
            }
            var clip = AudioClip.Create(name,samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
        }
        /// <summary>Plays the successful-delivery chime.</summary>
        public void Dock(bool enabled) { if(enabled) effects.PlayOneShot(chime); }
        /// <summary>Plays collision feedback.</summary>
        public void Collision(bool enabled) { if(enabled) effects.PlayOneShot(boing); }
        /// <summary>Plays a soft near-miss tick.</summary>
        public void NearMiss(bool enabled) { if(enabled) effects.PlayOneShot(tick,.4f); }
        /// <summary>Sets the persistent ambient preference immediately.</summary>
        public void SetMusic(bool enabled) { if(enabled) music.Play(); else music.Stop(); }
        /// <summary>Ducks ambience and stops the flight tone while paused.</summary>
        public void SetPaused(bool value) { paused = value; music.volume = value ? .018f : .045f; if(value) flight.volume = 0; }
        /// <summary>Maps flight speed to a gentle 220–440 Hz harmonic.</summary>
        public void SetFlight(float speed,bool enabled)
        { flight.pitch = Mathf.Lerp(1,2,Mathf.Clamp01(speed/6)); flight.volume = enabled && !paused ? Mathf.Clamp01(speed/6)*.018f : 0; }
        /// <summary>Releases owned clips.</summary>
        public void Dispose() { Object.Destroy(chime); Object.Destroy(boing); Object.Destroy(tick); Object.Destroy(whoosh); Object.Destroy(ambience); }
    }
}
