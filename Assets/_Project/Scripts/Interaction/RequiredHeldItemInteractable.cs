using Dangaronpo.Data;
using Dangaronpo.Player;
using Dangaronpo.UI;
using UnityEngine;

namespace Dangaronpo.Interaction
{
    /// <summary>
    /// 需要手持指定道具才能触发的交互点，例如用钩子够出物品、用剪刀剪开机关。
    /// </summary>
    public class RequiredHeldItemInteractable : InteractableBase
    {
        [SerializeField] private ItemDataSO requiredHeldItem;
        [SerializeField] private ItemDataSO rewardItem;
        [SerializeField] private InvestigationUI investigationUI;

        [SerializeField, TextArea(2, 4)] private string failText = "That does not seem to work.";
        [SerializeField, TextArea(2, 4)] private string successText = "You found something.";

        [SerializeField] private GameObject objectToHide;
        [SerializeField] private bool hideTargetOnSuccess = true;
        [SerializeField] private bool clearHeldItemOnSuccess = true;
        [SerializeField] private bool consumeHeldItemOnSuccess;

        private void Reset()
        {
            objectToHide = gameObject;
        }

        private void Awake()
        {
            if (objectToHide == null)
                objectToHide = gameObject;

            if (requiredHeldItem == null)
                Debug.LogError($"{nameof(RequiredHeldItemInteractable)} is missing Required Held Item.", this);
        }

        public override void Interact(PlayerInteractor playerInteractor)
        {
            if (playerInteractor == null || playerInteractor.Inventory == null)
            {
                Debug.LogError($"{nameof(RequiredHeldItemInteractable)} cannot find Player Inventory.", this);
                return;
            }

            if (requiredHeldItem == null)
            {
                Debug.LogError($"{nameof(RequiredHeldItemInteractable)} is missing Required Held Item.", this);
                return;
            }

            if (playerInteractor.Inventory.HeldItem != requiredHeldItem)
            {
                // 这里故意不检查“背包是否拥有”，因为设计上要求玩家主动取出正确道具。
                ShowMessage(failText);
                return;
            }

            if (consumeHeldItemOnSuccess)
                playerInteractor.Inventory.RemoveItem(requiredHeldItem);
            else if (clearHeldItemOnSuccess)
                playerInteractor.Inventory.HoldItem(null);

            bool addedReward = false;

            if (rewardItem != null)
                // 奖励道具进入背包后，会统一触发拾取确认 UI。
                addedReward = playerInteractor.Inventory.AddItem(rewardItem);

            if (hideTargetOnSuccess)
            {
                // 成功后隐藏交互点，防止玩家重复获得同一个奖励。
                GameObject target = objectToHide != null ? objectToHide : gameObject;
                target.SetActive(false);
            }

            if (!addedReward)
                ShowMessage(successText);
        }

        private void ShowMessage(string message)
        {
            if (investigationUI != null)
                investigationUI.Show(message);
            else
                Debug.Log(message, this);
        }
    }
}
