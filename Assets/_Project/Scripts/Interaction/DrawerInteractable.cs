using System.Collections;
using Dangaronpo.Data;
using Dangaronpo.Player;
using Dangaronpo.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;

namespace Dangaronpo.Interaction
{
    /// <summary>
    /// 抽屉开关逻辑。可以配置是否需要手持钥匙，以及打开后钥匙是收回还是消耗。
    /// </summary>
    public class DrawerInteractable : InteractableBase
    {
        [SerializeField] private Transform drawerTransform;
        [SerializeField] private ItemDataSO requiredKey;
        [SerializeField] private InvestigationUI investigationUI;

        [FormerlySerializedAs("openLocaloffset")]
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 0f, -0.45f);
        [SerializeField] private float openDuration = 0.5f;
        [SerializeField, TextArea(2, 4)] private string lockedText = "The drawer is locked.";

        [SerializeField] private bool clearHeldItemOnOpen = true;
        [SerializeField] private bool consumeRequiredKeyOnOpen;
        [SerializeField] private UnityEvent onLocked;
        [SerializeField] private UnityEvent onOpened;
        [SerializeField] private UnityEvent onClosed;

        private Vector3 closedLocalPosition;
        private bool isOpen;
        private bool isMoving;

        private void Awake()
        {
            if (drawerTransform == null)
                drawerTransform = transform;

            closedLocalPosition = drawerTransform.localPosition;
        }

        public override void Interact(PlayerInteractor playerInteractor)
        {
            if (isMoving)
                return;

            // 上锁抽屉只认“当前手持物品”，不是背包里是否拥有钥匙。
            if (!isOpen && requiredKey != null && !IsHoldingRequiredKey(playerInteractor))
            {
                if (investigationUI != null)
                    investigationUI.Show(lockedText);
                else
                    Debug.Log(lockedText, this);

                onLocked?.Invoke();
                return;
            }

            StartCoroutine(MoveDrawer(!isOpen, playerInteractor));
        }

        private bool IsHoldingRequiredKey(PlayerInteractor playerInteractor)
        {
            if (playerInteractor == null || playerInteractor.Inventory == null)
                return false;

            return playerInteractor.Inventory.HeldItem == requiredKey;
        }

        private IEnumerator MoveDrawer(bool open, PlayerInteractor playerInteractor)
        {
            isMoving = true;

            Vector3 startPosition = drawerTransform.localPosition;
            Vector3 targetPosition = open ? closedLocalPosition + openLocalOffset : closedLocalPosition;

            float elapsed = 0f;

            // 用协程平滑移动抽屉，比录制一个简单开合动画更容易微调距离和速度。
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / openDuration;
                t = Mathf.SmoothStep(0f, 1f, t);

                drawerTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            drawerTransform.localPosition = targetPosition;
            isOpen = open;

            if (isOpen)
                onOpened?.Invoke();
            else
                onClosed?.Invoke();

            HandleRequiredKeyAfterOpen(open, playerInteractor);

            isMoving = false;
        }

        private void HandleRequiredKeyAfterOpen(bool open, PlayerInteractor playerInteractor)
        {
            if (!open)
                return;

            if (requiredKey == null)
                return;

            if (playerInteractor == null || playerInteractor.Inventory == null)
                return;

            if (consumeRequiredKeyOnOpen)
            {
                // 一次性钥匙：打开后直接从背包移除。
                playerInteractor.Inventory.RemoveItem(requiredKey);
                return;
            }

            if (clearHeldItemOnOpen)
                // 可复用钥匙：保留在背包里，但从手上收回，避免一直拿着钥匙。
                playerInteractor.Inventory.HoldItem(null);
        }
    }
}
