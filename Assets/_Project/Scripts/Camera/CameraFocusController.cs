using System;
using System.Collections;
using Dangaronpo.Player;
using UnityEngine;

namespace Dangaronpo.CameraSystem
{
    /// <summary>
    /// 镜头特写控制器。进入特写时移动相机并锁住玩家控制，退出时回到进入前的位置和角度。
    /// </summary>
    public class CameraFocusController : MonoBehaviour
    {
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float transitionDuration = 0.25f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool hideCursorDuringFocus = true;
        [SerializeField] private bool keepCameraLockedAtFocusTarget = true;

        private Coroutine moveRoutine;
        private Transform currentFocusTarget;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Action focusExited;
        private bool isFocused;
        private bool isExiting;
        private int enteredFrame = -1;

        public bool IsFocused => isFocused;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (!isFocused || isExiting)
                return;

            // 避免玩家按 E 进入特写的同一帧，又被 Update 立刻识别成退出特写。
            if (Time.frameCount == enteredFrame)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
                ExitFocus();
        }

        private void LateUpdate()
        {
            if (!isFocused || isExiting || !keepCameraLockedAtFocusTarget)
                return;

            if (moveRoutine != null || currentFocusTarget == null || cameraTransform == null)
                return;

            cameraTransform.SetPositionAndRotation(currentFocusTarget.position, currentFocusTarget.rotation);
        }

        public void EnterFocus(Transform focusTarget, Action onFocusExited = null)
        {
            if (focusTarget == null)
            {
                Debug.LogError($"{nameof(CameraFocusController)} is missing focus target.", this);
                return;
            }

            if (cameraTransform == null)
            {
                Debug.LogError($"{nameof(CameraFocusController)} is missing Camera Transform.", this);
                return;
            }

            if (!isFocused)
            {
                // 只在第一次进入特写时记录原始位置，连续切换目标时不会覆盖返回点。
                originalPosition = cameraTransform.position;
                originalRotation = cameraTransform.rotation;
            }

            focusExited = onFocusExited;
            currentFocusTarget = focusTarget;
            isFocused = true;
            isExiting = false;
            enteredFrame = Time.frameCount;

            if (playerModeController != null)
                playerModeController.EnterReadingText();

            if (hideCursorDuringFocus)
                Cursor.visible = false;

            StartMove(cameraTransform.position, cameraTransform.rotation, focusTarget.position, focusTarget.rotation, null);
        }

        public void ExitFocus()
        {
            if (!isFocused || isExiting)
                return;

            isExiting = true;
            StartMove(cameraTransform.position, cameraTransform.rotation, originalPosition, originalRotation, CompleteExit);
        }

        private void CompleteExit()
        {
            isFocused = false;
            isExiting = false;

            if (playerModeController != null)
                playerModeController.ExitReadingText();

            Action callback = focusExited;
            focusExited = null;
            currentFocusTarget = null;
            callback?.Invoke();
        }

        private void StartMove(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, Action onComplete)
        {
            if (moveRoutine != null)
                StopCoroutine(moveRoutine);

            // 新的镜头移动会中断旧移动，保证快速重复操作时相机不会被多个协程同时控制。
            moveRoutine = StartCoroutine(MoveRoutine(fromPosition, fromRotation, toPosition, toRotation, onComplete));
        }

        private IEnumerator MoveRoutine(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, Action onComplete)
        {
            float duration = Mathf.Max(0f, transitionDuration);

            if (duration <= 0f)
            {
                cameraTransform.SetPositionAndRotation(toPosition, toRotation);
                moveRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float easedT = transitionCurve != null ? transitionCurve.Evaluate(t) : t;

                cameraTransform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, toPosition, easedT),
                    Quaternion.Slerp(fromRotation, toRotation, easedT));

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(toPosition, toRotation);
            moveRoutine = null;
            onComplete?.Invoke();
        }

        private void ValidateReferences()
        {
            if (playerModeController == null)
                Debug.LogError($"{nameof(CameraFocusController)} is missing Player Mode Controller.", this);

            if (cameraTransform == null)
                Debug.LogError($"{nameof(CameraFocusController)} is missing Camera Transform.", this);
        }
    }
}
