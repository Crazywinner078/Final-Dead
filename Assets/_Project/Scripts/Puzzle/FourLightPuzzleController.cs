using System.Collections;
using Dangaronpo.CameraSystem;
using Dangaronpo.Interaction;
using Dangaronpo.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 四灯机关。调查后进入特写，并循环播放 134、3、34、12 加随机闪烁重置的灯光序列。
    /// </summary>
    public class FourLightPuzzleController : InteractableBase
    {
        [SerializeField] private CameraFocusController cameraFocusController;
        [SerializeField] private Transform closeUpCameraTarget;
        [SerializeField] private PuzzleLightView[] lights = new PuzzleLightView[4];
        [SerializeField] private string[] lightSteps = { "134", "3", "34", "12" };
        [SerializeField] private float stepOnDuration = 0.6f;
        [SerializeField] private float stepOffDuration = 0.25f;
        [SerializeField] private float resetBlinkDuration = 1.2f;
        [SerializeField] private float resetBlinkInterval = 0.08f;
        [SerializeField] private float cyclePauseDuration = 0.35f;
        [SerializeField] private bool restartSequenceOnInteract = true;
        [SerializeField] private bool stopSequenceWhenCloseUpExits;
        [SerializeField] private UnityEvent onCloseUpEntered;
        [SerializeField] private UnityEvent onSequenceStarted;
        [SerializeField] private UnityEvent onLightStep;
        [SerializeField] private UnityEvent onResetBlinkStarted;
        [SerializeField] private UnityEvent onSequenceStopped;

        private Coroutine sequenceRoutine;

        private void Awake()
        {
            SetAllLights(false);
        }

        private void OnDisable()
        {
            StopSequence();
        }

        public override void Interact(PlayerInteractor playerInteractor)
        {
            // 特写模式会通过 PlayerModeController 关闭玩家移动和镜头转动。
            if (cameraFocusController != null && closeUpCameraTarget != null)
                cameraFocusController.EnterFocus(closeUpCameraTarget, HandleCloseUpExited);
            else
                Debug.LogWarning($"{nameof(FourLightPuzzleController)} is missing camera focus references.", this);

            onCloseUpEntered?.Invoke();

            if (restartSequenceOnInteract)
                RestartSequence();
            else
                StartSequence();
        }

        public void StartSequence()
        {
            if (sequenceRoutine != null)
                return;

            sequenceRoutine = StartCoroutine(SequenceRoutine());
            onSequenceStarted?.Invoke();
        }

        public void RestartSequence()
        {
            StopSequence();
            sequenceRoutine = StartCoroutine(SequenceRoutine());
            onSequenceStarted?.Invoke();
        }

        public void StopSequence()
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            SetAllLights(false);
            onSequenceStopped?.Invoke();
        }

        private IEnumerator SequenceRoutine()
        {
            while (true)
            {
                for (int i = 0; i < lightSteps.Length; i++)
                {
                    // lightSteps 用字符串表达同时亮起的灯，例如 "134" 表示第 1、3、4 盏灯。
                    ShowStep(lightSteps[i]);
                    yield return new WaitForSeconds(stepOnDuration);

                    SetAllLights(false);
                    yield return new WaitForSeconds(stepOffDuration);
                }

                yield return ResetBlinkRoutine();

                SetAllLights(false);
                yield return new WaitForSeconds(cyclePauseDuration);
            }
        }

        private IEnumerator ResetBlinkRoutine()
        {
            float elapsed = 0f;
            onResetBlinkStarted?.Invoke();

            // 序列末尾的随机乱闪只表示状态重置，不承载新的密码信息。
            while (elapsed < resetBlinkDuration)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null)
                        lights[i].SetLit(Random.value > 0.5f);
                }

                float waitTime = Mathf.Max(0.01f, resetBlinkInterval);
                elapsed += waitTime;
                yield return new WaitForSeconds(waitTime);
            }
        }

        private void ShowStep(string step)
        {
            SetAllLights(false);

            if (string.IsNullOrWhiteSpace(step))
                return;

            for (int i = 0; i < step.Length; i++)
            {
                int lightIndex = step[i] - '1';

                if (lightIndex < 0 || lightIndex >= lights.Length)
                    continue;

                // 玩家看到的是 1-4 编号，数组内部是 0-3 索引。
                if (lights[lightIndex] != null)
                    lights[lightIndex].SetLit(true);
            }

            onLightStep?.Invoke();
        }

        private void SetAllLights(bool lit)
        {
            if (lights == null)
                return;

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].SetLit(lit);
            }
        }

        private void HandleCloseUpExited()
        {
            if (stopSequenceWhenCloseUpExits)
                StopSequence();
        }
    }
}
