using UnityEngine;
using UnityEngine.Audio;

namespace Dangaronpo.Audio
{
    /// <summary>
    /// 通用音效播放器。UI、交互物体和谜题都可以调用它播放 AudioCueSO。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField] private AudioCueSO defaultCue;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        public float Volume => volume;

        private void Awake()
        {
            RestoreListenerState();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.outputAudioMixerGroup = outputMixerGroup;
            }
        }

        public void PlayDefault()
        {
            Play(defaultCue);
        }

        public void Play(AudioCueSO cue)
        {
            if (audioSource == null || cue == null)
                return;

            // AudioCueSO 决定随机哪个 clip 和音高，播放器只负责真正播放。
            AudioClip clip = cue.GetClip();

            if (clip == null)
                return;

            EnsureAudioDataLoaded(clip);
            ConfigureAudioSource(audioSource, cue);
            audioSource.pitch = cue.GetPitch();
            audioSource.PlayOneShot(clip, cue.Volume * volume);
        }

        public void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            EnsureAudioDataLoaded(clip);
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(clip, volume);
        }

        public void SetVolume(float targetVolume)
        {
            volume = Mathf.Clamp01(targetVolume);
        }

        public void PlayAtPosition(AudioCueSO cue, Vector3 position)
        {
            if (cue == null)
                return;

            AudioClip clip = cue.GetClip();

            if (clip == null)
                return;

            EnsureAudioDataLoaded(clip);

            // 3D 音效用临时物体播放，避免移动主 SFXPlayer 的位置影响其它音效。
            GameObject soundObject = new GameObject($"SFX_{clip.name}");
            soundObject.transform.position = position;

            AudioSource source = soundObject.AddComponent<AudioSource>();
            ConfigureAudioSource(source, cue);
            source.outputAudioMixerGroup = outputMixerGroup;
            source.clip = clip;
            source.volume = cue.Volume * volume;
            source.pitch = cue.GetPitch();
            source.Play();

            // 播放完成后自动销毁临时 AudioSource，防止场景里堆积音效物体。
            float destroyDelay = Mathf.Max(0.1f, clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch)) + 0.1f);
            Destroy(soundObject, destroyDelay);
        }

        private void ConfigureAudioSource(AudioSource source, AudioCueSO cue)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = cue.SpatialBlend;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.outputAudioMixerGroup = outputMixerGroup;
        }

        private static void EnsureAudioDataLoaded(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

        private static void RestoreListenerState()
        {
            // 直接从某个场景进入 Play Mode 时，静态 AudioListener 状态可能沿用上一次运行。
            AudioListener.pause = false;

            if (float.IsNaN(AudioListener.volume) || AudioListener.volume <= 0f)
                AudioListener.volume = 1f;
        }
    }
}
