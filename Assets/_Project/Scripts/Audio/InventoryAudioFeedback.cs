using Dangaronpo.Player;
using UnityEngine;

namespace Dangaronpo.Audio
{
    /// <summary>
    /// 背包音效桥接器。监听 ItemAdded，让所有“获得道具”的入口都能统一播放拾取音。
    /// </summary>
    public class InventoryAudioFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private AudioCuePlayer cuePlayer;
        [SerializeField] private AudioCueSO itemAddedCue;

        private void OnEnable()
        {
            if (playerInventory != null)
                playerInventory.ItemAdded += HandleItemAdded;
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.ItemAdded -= HandleItemAdded;
        }

        private void HandleItemAdded(Dangaronpo.Data.ItemDataSO item)
        {
            // item 参数当前不用，但保留它可以以后按道具类型播放不同音效。
            if (itemAddedCue != null && cuePlayer != null)
                cuePlayer.Play(itemAddedCue);
        }
    }
}
