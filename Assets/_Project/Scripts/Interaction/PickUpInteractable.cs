using Dangaronpo.Data;
using Dangaronpo.Player;
using UnityEngine;

namespace Dangaronpo.Interaction
{
    /// <summary>
    /// 场景中的可拾取道具。成功加入背包后隐藏场景物体。
    /// </summary>
    public class PickUpInteractable : InteractableBase
    {
        [SerializeField] private ItemDataSO itemData;
        [SerializeField] private GameObject objectToHide;

        private void Reset()
        {
            objectToHide = gameObject;
        }

        public override void Interact(PlayerInteractor playerInteractor)
        {
            if (itemData == null)
            {
                Debug.LogError($"{nameof(PickUpInteractable)} is missing Item Data.", this);
                return;
            }

            if (playerInteractor == null || playerInteractor.Inventory == null)
            {
                Debug.LogError($"{nameof(PickUpInteractable)} cannot find Player Inventory.", this);
                return;
            }

            // AddItem 会触发 PlayerInventory.ItemAdded，拾取确认 UI 和音效都可以监听这个事件。
            bool added = playerInteractor.Inventory.AddItem(itemData);

            if (!added)
                return;

            // wrapper 物体和真实模型可能不是同一个对象，所以允许手动指定要隐藏的目标。
            GameObject target = objectToHide != null ? objectToHide : gameObject;
            target.SetActive(false);
        }
    }
}
