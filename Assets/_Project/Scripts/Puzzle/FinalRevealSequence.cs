using System.Collections;
using Dangaronpo.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 最终演出：锁住玩家、切到台子特写、升起台子并启用左轮/子弹等最终物件。
    /// </summary>
    public class FinalRevealSequence : MonoBehaviour
    {
        [Header("Player And Camera")]
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float cameraMoveDuration = 0.35f;
        [SerializeField] private bool restoreCameraWhenFinished = true;

        [Header("Pedestal")]
        [SerializeField] private Transform pedestalRoot;
        [SerializeField] private Transform hiddenPoint;
        [SerializeField] private Transform raisedPoint;
        [SerializeField] private float pedestalRiseDuration = 2.5f;
        [SerializeField] private AnimationCurve pedestalRiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool movePedestalToHiddenPointOnAwake = true;

        [Header("Reveal Objects")]
        [SerializeField] private GameObject[] objectsToReveal;
        [SerializeField] private Behaviour[] behavioursToEnableAfterReveal;
        [SerializeField] private Collider[] collidersToEnableAfterReveal;
        [SerializeField] private Light[] lightsToEnableDuringReveal;

        [Header("Timing")]
        [SerializeField] private float waitBeforeRise = 0.25f;
        [SerializeField] private float waitAfterRise = 0.75f;
        [SerializeField] private bool playOnlyOnce = true;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip revealClip;

        [Header("Events")]
        [SerializeField] private UnityEvent onSequenceStarted;
        [SerializeField] private UnityEvent onSequenceFinished;

        private Coroutine sequenceRoutine;
        private bool hasPlayed;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;

        private void Awake()
        {
            if (movePedestalToHiddenPointOnAwake && pedestalRoot != null && hiddenPoint != null)
                pedestalRoot.SetPositionAndRotation(hiddenPoint.position, hiddenPoint.rotation);

            // 演出开始前，最终物件、可交互脚本、碰撞体和灯光都保持隐藏/禁用。
            SetObjectsActive(objectsToReveal, false);
            SetBehavioursEnabled(behavioursToEnableAfterReveal, false);
            SetCollidersEnabled(collidersToEnableAfterReveal, false);
            SetLightsEnabled(lightsToEnableDuringReveal, false);
            ValidateReferences();
        }

        public void Play()
        {
            if (playOnlyOnce && hasPlayed)
                return;

            if (sequenceRoutine != null)
                StopCoroutine(sequenceRoutine);

            sequenceRoutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            hasPlayed = true;
            onSequenceStarted?.Invoke();

            if (cameraTransform != null)
            {
                // 记录玩家原本相机位置，演出结束后可以平滑回到原视角。
                originalCameraPosition = cameraTransform.position;
                originalCameraRotation = cameraTransform.rotation;
            }

            if (playerModeController != null)
                playerModeController.EnterReadingText();

            Cursor.visible = false;

            SetLightsEnabled(lightsToEnableDuringReveal, true);
            SetObjectsActive(objectsToReveal, true);

            // 音频可以配置成一次性 revealClip，也可以让 AudioSource 播自己的 clip。
            if (audioSource != null)
            {
                if (revealClip != null)
                    audioSource.PlayOneShot(revealClip);
                else
                    audioSource.Play();
            }

            yield return MoveCameraRoutine(cameraTarget, cameraMoveDuration);

            if (waitBeforeRise > 0f)
                yield return new WaitForSeconds(waitBeforeRise);

            yield return RisePedestalRoutine();

            // 台子升完后才打开交互和碰撞，避免玩家在演出中途射线点到最终物件。
            SetBehavioursEnabled(behavioursToEnableAfterReveal, true);
            SetCollidersEnabled(collidersToEnableAfterReveal, true);

            if (waitAfterRise > 0f)
                yield return new WaitForSeconds(waitAfterRise);

            if (restoreCameraWhenFinished)
                yield return MoveCameraToRoutine(originalCameraPosition, originalCameraRotation, cameraMoveDuration);

            if (playerModeController != null)
                playerModeController.ExitReadingText();

            onSequenceFinished?.Invoke();
            sequenceRoutine = null;
        }

        private IEnumerator RisePedestalRoutine()
        {
            if (pedestalRoot == null || hiddenPoint == null || raisedPoint == null)
                yield break;

            Vector3 startPosition = hiddenPoint.position;
            Quaternion startRotation = hiddenPoint.rotation;
            Vector3 endPosition = raisedPoint.position;
            Quaternion endRotation = raisedPoint.rotation;

            float duration = Mathf.Max(0f, pedestalRiseDuration);

            if (duration <= 0f)
            {
                pedestalRoot.SetPositionAndRotation(endPosition, endRotation);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float easedT = pedestalRiseCurve != null ? pedestalRiseCurve.Evaluate(t) : t;

                // 位置和旋转都从隐藏点插值到升起点，方便用两个空物体直接调演出终点。
                pedestalRoot.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, endPosition, easedT),
                    Quaternion.Slerp(startRotation, endRotation, easedT));

                elapsed += Time.deltaTime;
                yield return null;
            }

            pedestalRoot.SetPositionAndRotation(endPosition, endRotation);
        }

        private IEnumerator MoveCameraRoutine(Transform target, float duration)
        {
            if (target == null || cameraTransform == null)
                yield break;

            yield return MoveCameraToRoutine(target.position, target.rotation, duration);
        }

        private IEnumerator MoveCameraToRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            if (cameraTransform == null)
                yield break;

            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float safeDuration = Mathf.Max(0f, duration);

            if (safeDuration <= 0f)
            {
                cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                float t = elapsed / safeDuration;
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                cameraTransform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, easedT),
                    Quaternion.Slerp(startRotation, targetRotation, easedT));

                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        private static void SetObjectsActive(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            foreach (GameObject target in targets)
            {
                if (target != null)
                    target.SetActive(active);
            }
        }

        private static void SetBehavioursEnabled(Behaviour[] targets, bool enabled)
        {
            if (targets == null)
                return;

            foreach (Behaviour target in targets)
            {
                if (target != null)
                    target.enabled = enabled;
            }
        }

        private static void SetCollidersEnabled(Collider[] targets, bool enabled)
        {
            if (targets == null)
                return;

            foreach (Collider target in targets)
            {
                if (target != null)
                    target.enabled = enabled;
            }
        }

        private static void SetLightsEnabled(Light[] targets, bool enabled)
        {
            if (targets == null)
                return;

            foreach (Light target in targets)
            {
                if (target != null)
                    target.enabled = enabled;
            }
        }

        private void ValidateReferences()
        {
            if (playerModeController == null)
                Debug.LogError($"{nameof(FinalRevealSequence)} is missing Player Mode Controller.", this);

            if (cameraTransform == null)
                Debug.LogError($"{nameof(FinalRevealSequence)} is missing Camera Transform.", this);

            if (cameraTarget == null)
                Debug.LogError($"{nameof(FinalRevealSequence)} is missing Camera Target.", this);

            if (pedestalRoot == null)
                Debug.LogError($"{nameof(FinalRevealSequence)} is missing Pedestal Root.", this);

            if (hiddenPoint == null)
                Debug.LogError($"{nameof(FinalRevealSequence)} is missing Hidden Point.", this);

            if (raisedPoint == null)
                Debug.LogError($"{nameof(FinalRevealSequence)} is missing Raised Point.", this);
        }
    }
}
