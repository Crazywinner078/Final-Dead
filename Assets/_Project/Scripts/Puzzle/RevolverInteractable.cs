using Dangaronpo.Data;
using Dangaronpo.Interaction;
using Dangaronpo.Player;
using Dangaronpo.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 最终左轮交互。手持子弹时优先装弹；没有可装子弹时尝试开枪并触发结局事件。
    /// </summary>
    public class RevolverInteractable : InteractableBase
    {
        [SerializeField] private ItemDataSO bulletItem;
        [SerializeField] private InvestigationUI investigationUI;
        [SerializeField] private int requiredLoadedBullets = 5;
        [SerializeField] private bool clearHeldBulletAfterEachLoad;
        [SerializeField] private string examinePromptText = "调查";
        [SerializeField] private string loadBulletPromptText = "装弹";
        [SerializeField] private string firePromptText = "开枪";
        [SerializeField] private string finishedPromptText = "调查";

        [SerializeField, TextArea(2, 4)] private string loadedOneBulletText = "你把一发子弹装进了左轮。";
        [SerializeField, TextArea(2, 4)] private string fullyLoadedText = "五发子弹都已经装进去了。";
        [SerializeField, TextArea(2, 4)] private string emptyGunText = "枪里没有子弹。";
        [SerializeField, TextArea(2, 4)] private string badEndingText = "枪声响起。";
        [SerializeField, TextArea(2, 4)] private string trueEndingText = "击锤落下，枪却没有响。";
        [SerializeField, TextArea(2, 4)] private string alreadyFiredText = "一切已经结束了。";
        [SerializeField] private bool showEndingInvestigationText;

        [SerializeField] private UnityEvent onBulletLoaded;
        [SerializeField] private UnityEvent onEmptyGun;
        [SerializeField] private UnityEvent onTrueEnding;
        [SerializeField] private UnityEvent onBadEnding;

        private int loadedBullets;
        private bool hasFired;

        public int LoadedBullets => loadedBullets;
        public bool IsFullyLoaded => loadedBullets >= requiredLoadedBullets;

        public override string GetPromptText(PlayerInteractor playerInteractor)
        {
            if (hasFired)
                return finishedPromptText;

            if (CanLoadBulletForPrompt(playerInteractor != null ? playerInteractor.Inventory : null))
                return loadBulletPromptText;

            if (loadedBullets <= 0)
                return examinePromptText;

            return firePromptText;
        }

        public override void Interact(PlayerInteractor playerInteractor)
        {
            if (hasFired)
            {
                ShowMessage(alreadyFiredText);
                return;
            }

            if (playerInteractor == null || playerInteractor.Inventory == null)
            {
                Debug.LogError($"{nameof(RevolverInteractable)} cannot find Player Inventory.", this);
                return;
            }

            if (CanLoadBullet(playerInteractor.Inventory))
            {
                // 同一个交互键 E：当手里拿着子弹时解释为“装一发”，否则解释为“扣扳机”。
                LoadOneBullet(playerInteractor.Inventory);
                return;
            }

            Fire();
        }

        private bool CanLoadBullet(PlayerInventory inventory)
        {
            if (bulletItem == null)
            {
                Debug.LogError($"{nameof(RevolverInteractable)} is missing Bullet Item.", this);
                return false;
            }

            return CanLoadBulletForPrompt(inventory);
        }

        private bool CanLoadBulletForPrompt(PlayerInventory inventory)
        {
            if (inventory == null || bulletItem == null)
                return false;

            if (loadedBullets >= requiredLoadedBullets)
                return false;

            // 装弹要求玩家把子弹从背包里“取出并手持”，不是只要背包里有子弹就行。
            return inventory.HeldItem == bulletItem && inventory.HasItem(bulletItem);
        }

        private void LoadOneBullet(PlayerInventory inventory)
        {
            if (!inventory.RemoveItem(bulletItem))
                return;

            loadedBullets++;

            if (clearHeldBulletAfterEachLoad && inventory.HeldItem == bulletItem)
                inventory.HoldItem(null);

            // 这里触发装弹音效/动画，结局判断仍留在 Fire。
            onBulletLoaded?.Invoke();

            if (loadedBullets >= requiredLoadedBullets)
                ShowMessage(fullyLoadedText);
            else
                ShowMessage(loadedOneBulletText);
        }

        private void Fire()
        {
            if (loadedBullets <= 0)
            {
                // 没装子弹时不能直接进入结局，只给玩家反馈。
                ShowMessage(emptyGunText);
                onEmptyGun?.Invoke();
                return;
            }

            hasFired = true;

            if (IsFullyLoaded)
            {
                // 本项目规则：五发全装后触发空枪真结局，其它有弹开枪都是坏结局。
                if (showEndingInvestigationText)
                    ShowMessage(trueEndingText);

                onTrueEnding?.Invoke();
                return;
            }

            if (showEndingInvestigationText)
                ShowMessage(badEndingText);

            onBadEnding?.Invoke();
        }

        private void ShowMessage(string message)
        {
            if (investigationUI != null)
            {
                investigationUI.Show(message);
                return;
            }

            Debug.Log(message, this);
        }
    }
}
