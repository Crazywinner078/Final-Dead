using System.Collections;
using Dangaronpo.Data;
using Dangaronpo.Interaction;
using Dangaronpo.Player;
using Dangaronpo.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 保险柜谜题控制器。负责两次密码阶段、开启动画、内部物品显隐和最终密码通过事件。
    /// </summary>
    public class SafePuzzleController : InteractableBase
    {
        /// <summary>
        /// 保险柜流程状态：第一次密码开柜，等待玩家拾取内部物品，然后恢复最终密码输入。
        /// </summary>
        private enum SafeStage
        {
            FirstCode,
            WaitingForInsideItem,
            FinalCode,
            FinalSolved
        }

        [SerializeField] private SafePuzzleUI safePuzzleUI;
        [SerializeField] private Animator safeAnimator;
        [SerializeField] private string openStateName = "Take 001";
        [SerializeField] private AnimationClip openAnimationClip;
        [SerializeField] private GameObject insidePickupObject;
        [SerializeField] private GameObject[] insidePickupObjects;
        [SerializeField] private Transform[] insidePickupTransforms;
        [SerializeField] private Collider[] collidersToDisableAfterOpen;
        [SerializeField] private ItemDataSO itemRequiredForFinalCode;
        [SerializeField] private string firstCode = "0000";
        [SerializeField] private string finalCode = "0000";
        [SerializeField] private UnityEvent onIncorrectCode;
        [SerializeField] private UnityEvent onFirstCodeSolved;
        [SerializeField] private UnityEvent onSafeOpenStarted;
        [SerializeField] private UnityEvent onSafeOpenFinished;
        [SerializeField] private UnityEvent onFinalCodeSolved;

        private PlayerInventory playerInventory;
        private Coroutine openRoutine;
        private SafeStage currentStage = SafeStage.FirstCode;
        private bool isOpening;
        private bool hasOpenedVisual;

        private void Awake()
        {
            if (safeAnimator == null && TryGetComponent(out Animator animator))
                safeAnimator = animator;

            // 保险柜模型自带动画时，禁用 Animator 可以防止进游戏自动播放开门动画。
            if (safeAnimator != null)
                safeAnimator.enabled = false;

            // 初始隐藏保险柜内部物品，只有第一次密码正确并开门完成后才显示。
            SetInsidePickupObjectsActive(false);

            ValidateCode(firstCode, nameof(firstCode));
            ValidateCode(finalCode, nameof(finalCode));
        }

        private void OnDisable()
        {
            UnbindInventory();
        }

        public override void Interact(PlayerInteractor playerInteractor)
        {
            if (playerInteractor == null)
            {
                Debug.LogError($"{nameof(SafePuzzleController)} cannot find Player Interactor.", this);
                return;
            }

            BindInventory(playerInteractor.Inventory);

            // 第一次打开后，在玩家拾取指定内部物品前，不再弹出密码界面。
            if (currentStage == SafeStage.WaitingForInsideItem)
                return;

            if (currentStage == SafeStage.FinalSolved)
                return;

            if (safePuzzleUI == null)
            {
                Debug.LogError($"{nameof(SafePuzzleController)} is missing Safe Puzzle UI.", this);
                return;
            }

            safePuzzleUI.Show(this);
        }

        public bool TrySubmitCode(string code)
        {
            if (currentStage == SafeStage.FirstCode)
                return TrySubmitFirstCode(code);

            if (currentStage == SafeStage.FinalCode)
                return TrySubmitFinalCode(code);

            return false;
        }

        public void ClosePuzzle()
        {
            if (safePuzzleUI != null)
                safePuzzleUI.Hide();
        }

        private bool TrySubmitFirstCode(string code)
        {
            if (NormalizeCode(code) != NormalizeCode(firstCode))
            {
                ShowIncorrectCode();
                return false;
            }

            currentStage = SafeStage.WaitingForInsideItem;

            if (safePuzzleUI != null)
                safePuzzleUI.Hide();

            // 第一次密码只负责打开保险柜，不直接发奖励，奖励物品由玩家自己从柜内拾取。
            onFirstCodeSolved?.Invoke();
            StartOpenSequence();
            return true;
        }

        private bool TrySubmitFinalCode(string code)
        {
            if (NormalizeCode(code) != NormalizeCode(finalCode))
            {
                ShowIncorrectCode();
                return false;
            }

            currentStage = SafeStage.FinalSolved;

            if (safePuzzleUI != null)
                safePuzzleUI.Hide();

            Debug.Log($"{nameof(SafePuzzleController)} final code solved.", this);
            onFinalCodeSolved?.Invoke();
            return true;
        }

        private void StartOpenSequence()
        {
            if (isOpening || hasOpenedVisual)
                return;

            if (openRoutine != null)
                StopCoroutine(openRoutine);

            openRoutine = StartCoroutine(OpenSequenceRoutine());
        }

        private IEnumerator OpenSequenceRoutine()
        {
            isOpening = true;
            onSafeOpenStarted?.Invoke();

            if (safeAnimator != null && !string.IsNullOrWhiteSpace(openStateName))
            {
                // 模型自带开门动画时，只在密码正确后手动启用并从第 0 帧播放。
                safeAnimator.enabled = true;
                safeAnimator.Play(openStateName, 0, 0f);
                safeAnimator.Update(0f);
            }
            else
            {
                Debug.LogWarning($"{nameof(SafePuzzleController)} is missing Animator or open state name.", this);
            }

            float waitTime = GetOpenAnimationDuration();
            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);
            else
                yield return null;

            SetInsidePickupObjectsActive(true);

            // 开门后临时关掉门/柜体碰撞，让玩家射线可以打到保险柜里的拾取物。
            SetOpenedCollidersEnabled(false);

            isOpening = false;
            hasOpenedVisual = true;
            openRoutine = null;
            onSafeOpenFinished?.Invoke();
        }

        private void BindInventory(PlayerInventory inventory)
        {
            if (playerInventory == inventory)
                return;

            UnbindInventory();

            playerInventory = inventory;

            if (playerInventory != null)
            {
                playerInventory.ItemAdded += HandleItemAdded;

                // 如果玩家已经拥有该物品，再次交互时也能直接进入最终密码阶段。
                if (playerInventory.HasItem(itemRequiredForFinalCode))
                    UnlockFinalCodeStage();
            }
        }

        private void UnbindInventory()
        {
            if (playerInventory == null)
                return;

            playerInventory.ItemAdded -= HandleItemAdded;
            playerInventory = null;
        }

        private void HandleItemAdded(ItemDataSO item)
        {
            if (item == itemRequiredForFinalCode)
                UnlockFinalCodeStage();
        }

        private void UnlockFinalCodeStage()
        {
            if (currentStage != SafeStage.WaitingForInsideItem)
                return;

            currentStage = SafeStage.FinalCode;
            // 最终密码阶段恢复保险柜本体碰撞，让玩家再次对保险柜按 E 输入密码。
            SetOpenedCollidersEnabled(true);
        }

        private float GetOpenAnimationDuration()
        {
            if (openAnimationClip != null)
                return Mathf.Max(0f, openAnimationClip.length);

            return 0.75f;
        }

        private void SetOpenedCollidersEnabled(bool enabled)
        {
            if (collidersToDisableAfterOpen == null)
                return;

            foreach (Collider targetCollider in collidersToDisableAfterOpen)
            {
                if (targetCollider != null)
                    targetCollider.enabled = enabled;
            }
        }

        private void SetInsidePickupObjectsActive(bool active)
        {
            if (insidePickupObject != null)
                insidePickupObject.SetActive(active);

            if (insidePickupObjects == null)
            {
                SetInsidePickupTransformsActive(active);
                return;
            }

            foreach (GameObject target in insidePickupObjects)
            {
                if (target != null)
                    target.SetActive(active);
            }

            SetInsidePickupTransformsActive(active);
        }

        private void SetInsidePickupTransformsActive(bool active)
        {
            if (insidePickupTransforms == null)
                return;

            foreach (Transform target in insidePickupTransforms)
            {
                if (target != null)
                    target.gameObject.SetActive(active);
            }
        }

        private void ShowIncorrectCode()
        {
            if (safePuzzleUI != null)
                safePuzzleUI.ShowFeedback("Incorrect code");

            onIncorrectCode?.Invoke();
        }

        private void ValidateCode(string code, string fieldName)
        {
            if (!string.IsNullOrWhiteSpace(code) && code.Length != 4)
                Debug.LogWarning($"{nameof(SafePuzzleController)} expects {fieldName} to be a 4-digit code.", this);
        }

        private static string NormalizeCode(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
