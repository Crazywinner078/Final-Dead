using UnityEngine;

namespace Dangaronpo.Audio
{
    /// <summary>
    /// 单个音效配置资产。支持多个随机 clip、音量、音高范围和 2D/3D 空间混合。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCue", menuName = "Dangaronpo/Audio/Audio Cue")]
    public class AudioCueSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 12f;

        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;

        public AudioClip GetClip()
        {
            if (clips == null || clips.Length == 0)
                return null;

            // 先统计有效 clip，避免数组里有空引用时随机到 null。
            int validClipCount = 0;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    validClipCount++;
            }

            if (validClipCount <= 0)
                return null;

            int selectedIndex = Random.Range(0, validClipCount);
            int currentIndex = 0;

            // 在有效 clip 中按 selectedIndex 取一个，实现“忽略空位”的随机。
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                if (currentIndex == selectedIndex)
                    return clips[i];

                currentIndex++;
            }

            return null;
        }

        public float GetPitch()
        {
            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);

            return Random.Range(minPitch, maxPitch);
        }

        private void OnValidate()
        {
            // Inspector 改值时做基本保护，避免负距离和 0 音高导致奇怪播放结果。
            minDistance = Mathf.Max(0f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);

            if (pitchRange.x <= 0f)
                pitchRange.x = 0.01f;

            if (pitchRange.y <= 0f)
                pitchRange.y = 0.01f;
        }
    }
}
