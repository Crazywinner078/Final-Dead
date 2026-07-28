using Dangaronpo.Interaction;
using Dangaronpo.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.Player
{
    /// <summary>
    /// 玩家中心射线交互。负责找当前准星指向的 Interactable，并把 E 键交互请求转发给目标。
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private InteractionPromptUI promptUI;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private UnityEvent onInteracted;

        public PlayerInventory Inventory => playerInventory;

        private InteractableBase currentInteractable;

        private void Awake()
        {
            if (playerInventory == null)
                playerInventory = GetComponent<PlayerInventory>();

        }

        private void Update()
        {
            UpdateTarget();
            HandleInteractInput();
        }

        private void UpdateTarget()
        {
            InteractableBase nextInteractable = null;

            if (cameraTransform != null)
            {
                // 从相机中心向前射线检测，目标对象可以是子碰撞体，脚本挂在父物体上也能找到。
                Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
                {
                    nextInteractable = hit.collider.GetComponentInParent<InteractableBase>();
                }
            }

            if (nextInteractable == currentInteractable)
            {
                // 有些交互物的提示会跟随玩家状态变化，例如左轮会在“装弹/开枪/调查”之间切换。
                RefreshPrompt();
                return;
            }

            // 只有目标变化时才触发 focus/unfocus，避免每帧重复刷新 UI 和高亮。
            if (currentInteractable != null)
                currentInteractable.OnUnfocus();

            currentInteractable = nextInteractable;

            if (currentInteractable != null)
            {
                currentInteractable.OnFocus();
                RefreshPrompt();
                return;
            }

            promptUI?.Hide();
        }

        private void HandleInteractInput()
        {
            if (currentInteractable == null)
                return;

            if (!currentInteractable.CanInteract)
                return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                promptUI?.Hide();
                // 先播放统一调查/交互音效，再执行具体交互逻辑。
                onInteracted?.Invoke();
                currentInteractable.Interact(this);
            }
        }

        private void RefreshPrompt()
        {
            if (currentInteractable == null)
            {
                promptUI?.Hide();
                return;
            }

            if (!currentInteractable.CanInteract)
            {
                promptUI?.Hide();
                return;
            }

            string promptText = currentInteractable.GetPromptText(this);
            promptUI?.Show(currentInteractable.DisplayName, promptText);
        }
    }
}
