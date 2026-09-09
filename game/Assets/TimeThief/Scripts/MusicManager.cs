using System.Collections;
using UnityEngine;

namespace TimeThief
{
    public sealed class MusicManager : MonoBehaviour
    {
        public AudioSource musicSourceA, musicSourceB;
        public AudioClip normalBattleMusic;
        public float crossfadeDuration = 1.5f, musicVolume = .7f;
        AudioSource active, sfx, magic;
        Coroutine fading;
        GameConfig config;
        public bool Muted;
        public void Init(GameConfig c)
        {
            config = c;
            normalBattleMusic = c.normalBattleMusic;
            crossfadeDuration = c.crossfadeDuration;
            musicVolume = c.musicVolume;
            musicSourceA = gameObject.AddComponent<AudioSource>();
            musicSourceB = gameObject.AddComponent<AudioSource>();
            sfx = gameObject.AddComponent<AudioSource>();
            magic = gameObject.AddComponent<AudioSource>();
            musicSourceA.loop = musicSourceB.loop = magic.loop = true;
            musicSourceA.playOnAwake = musicSourceB.playOnAwake = sfx.playOnAwake = magic.playOnAwake = false;
            active = musicSourceA;
            Muted = PlayerPrefs.GetInt("TTMuted", 0) == 1;
            ApplyMute();
        }

        public void PlayNormalMusic() => CrossfadeTo(normalBattleMusic);
        public void PlayMenuMusic() => CrossfadeTo(config.menuMusic ? config.menuMusic : normalBattleMusic);
        public void PlayBossMusic(AudioClip clip) => CrossfadeTo(clip ? clip : normalBattleMusic);
        public void CrossfadeTo(AudioClip clip)
        {
            if (active.clip == clip)
                return;
            if (fading != null)
                StopCoroutine(fading);
            var next = active == musicSourceA ? musicSourceB : musicSourceA;
            next.Stop();
            next.clip = clip;
            next.volume = 0;
            if (clip)
                next.Play();
            fading = StartCoroutine(Fade(active, next));
            active = next;
        }

        IEnumerator Fade(AudioSource previous, AudioSource next)
        {
            float from = previous.volume;
            for (float t = 0; t < crossfadeDuration; t += Time.unscaledDeltaTime)
            {
                float f = t / Mathf.Max(.01f, crossfadeDuration);
                previous.volume = from * (1 - f);
                next.volume = musicVolume * f;
                yield return null;
            }

            previous.Stop();
            next.volume = musicVolume;
            fading = null;
        }

        public void FadeToSilence() => CrossfadeTo(null);
        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            active.volume = musicVolume;
        }

        public void Sfx(AudioClip clip)
        {
            if (clip && !Muted)
                sfx.PlayOneShot(clip, .65f);
        }

        public void MagicLoop(bool on)
        {
            if (on && config.magicLoopSound && !magic.isPlaying)
            {
                magic.clip = config.magicLoopSound;
                magic.volume = .3f;
                magic.Play();
            }
            else if (!on)
                magic.Stop();
        }

        public void Toggle()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt("TTMuted", Muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute();
        }

        void ApplyMute()
        {
            musicSourceA.mute = musicSourceB.mute = sfx.mute = magic.mute = Muted;
        }
    }
}
