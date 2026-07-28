using UnityEngine;

namespace Dangaronpo.Audio
{
    /// <summary>
    /// 放在按钮、机关或动画事件上的音效发射器，用 UnityEvent 直接调用 Play。
    /// </summary>
    public class AudioCueEmitter : MonoBehaviour
    {
        [SerializeField] private AudioCuePlayer cuePlayer;
        [SerializeField] private AudioCueSO cue;
        [SerializeField] private bool playAtEmitterPosition;

        public void Play()
        {
            Play(cue);
        }

        public void Play(AudioCueSO overrideCue)
        {
            if (cuePlayer == null || overrideCue == null)
                return;

            // 机关/物体音效可以选择从自身位置播放，UI 音效通常走 2D 播放。
            if (playAtEmitterPosition)
                cuePlayer.PlayAtPosition(overrideCue, transform.position);
            else
                cuePlayer.Play(overrideCue);
        }
    }
}
