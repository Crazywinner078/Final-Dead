using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Dangaronpo.Audio
{
    /// <summary>
    /// 背景音乐播放器。支持开场自动播放、切歌淡入淡出和设置界面音量控制。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField] private AudioClip startingTrack;
        [SerializeField, Range(0f, 1f)] private float volume = 0.55f;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private bool restartSameTrack;

        private Coroutine fadeRoutine;

        public float Volume => volume;
        public bool IsPlaying => musicSource != null && musicSource.isPlaying;

        private void Awake()
        {
            RestoreListenerState();

            if (musicSource == null)
                musicSource = GetComponent<AudioSource>();

            if (musicSource != null)
            {
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
                musicSource.outputAudioMixerGroup = outputMixerGroup;
            }
        }

        private void Start()
        {
            if (playOnAwake && startingTrack != null)
                PlayTrack(startingTrack);
        }

        public void PlayStartingTrack()
        {
            PlayTrack(startingTrack);
        }

        public void PlayTrack(AudioClip track)
        {
            if (musicSource == null || track == null)
                return;

            // 默认不重复重启同一首 BGM，避免 UI 或事件重复调用时音乐从头开始。
            if (musicSource.clip == track && musicSource.isPlaying && !restartSameTrack)
                return;

            EnsureAudioDataLoaded(track);
            StartFadeRoutine(FadeToTrackRoutine(track));
        }

        public void StopMusic()
        {
            StopMusic(fadeDuration);
        }

        public void StopMusic(float duration)
        {
            if (musicSource == null)
                return;

            StartFadeRoutine(StopRoutine(duration));
        }

        public void SetVolume(float targetVolume)
        {
            volume = Mathf.Clamp01(targetVolume);

            if (musicSource != null)
                musicSource.volume = volume;
        }

        private void StartFadeRoutine(IEnumerator routine)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            // 同一时间只允许一个淡入淡出流程，防止多个协程同时抢音量。
            fadeRoutine = StartCoroutine(routine);
        }

        private IEnumerator FadeToTrackRoutine(AudioClip track)
        {
            if (musicSource.isPlaying)
                yield return FadeVolumeRoutine(musicSource.volume, 0f, fadeDuration);

            musicSource.clip = track;
            musicSource.volume = 0f;
            musicSource.Play();

            yield return FadeVolumeRoutine(0f, volume, fadeDuration);
            fadeRoutine = null;
        }

        private IEnumerator StopRoutine(float duration)
        {
            if (musicSource.isPlaying)
                yield return FadeVolumeRoutine(musicSource.volume, 0f, duration);

            musicSource.Stop();
            fadeRoutine = null;
        }

        private IEnumerator FadeVolumeRoutine(float fromVolume, float toVolume, float duration)
        {
            duration = Mathf.Max(0f, duration);

            if (duration <= 0f)
            {
                musicSource.volume = toVolume;
                yield break;
            }

            float elapsed = 0f;

            // 使用 unscaledDeltaTime，这样即使以后暂停游戏，菜单淡入淡出也能继续。
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                musicSource.volume = Mathf.Lerp(fromVolume, toVolume, Mathf.SmoothStep(0f, 1f, t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            musicSource.volume = toVolume;
        }

        private static void EnsureAudioDataLoaded(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

        private static void RestoreListenerState()
        {
            // 直接从 SampleScene 进入 Play Mode 时，避免沿用上一次运行留下的全局暂停/静音状态。
            AudioListener.pause = false;

            if (float.IsNaN(AudioListener.volume) || AudioListener.volume <= 0f)
                AudioListener.volume = 1f;
        }
    }
}
