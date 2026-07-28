using System.Collections;
using Dangaronpo.Audio;
using Dangaronpo.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 左轮最终演出。隐藏场景里的枪，显示镜头前演出枪，缓慢移动到画面中央后播放音效并进入结局 UI。
    /// </summary>
    public class RevolverEndingSequence : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private bool hideCursorDuringSequence = true;

        [Header("World Revolver")]
        [SerializeField] private GameObject[] worldObjectsToHide;
        [SerializeField] private Renderer[] worldRenderersToDisable;
        [SerializeField] private Collider[] worldCollidersToDisable;

        [Header("Camera Revolver")]
        [SerializeField] private GameObject endingGunRoot;
        [SerializeField] private Transform endingGunTransform;
        [SerializeField] private Transform startPose;
        [SerializeField] private Transform aimPose;
        [SerializeField] private Vector3 startLocalPosition = new Vector3(0.44f, -0.34f, 0.68f);
        [SerializeField] private Vector3 startLocalEulerAngles = new Vector3(8f, -24f, 0f);
        [SerializeField] private Vector3 startLocalScale = Vector3.one;
        [SerializeField] private Vector3 aimLocalPosition = new Vector3(0.18f, -0.08f, 0.62f);
        [SerializeField] private Vector3 aimLocalEulerAngles = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 aimLocalScale = Vector3.one;
        [SerializeField] private bool animateScale;
        [SerializeField] private bool aimAtCameraOnFinalPose = true;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private LocalAxis muzzleLocalAxis = LocalAxis.NegativeX;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float aimDuration = 1.85f;
        [SerializeField, Min(0f)] private float holdBeforeShot = 0.45f;
        [SerializeField, Min(0f)] private float endingDelayAfterShot;
        [SerializeField] private AnimationCurve aimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool playOnlyOnce = true;

        [Header("Audio")]
        [SerializeField] private AudioCuePlayer audioCuePlayer;
        [SerializeField] private MusicPlayer musicPlayer;
        [SerializeField] private bool fadeOutMusicOnStart = true;
        [SerializeField, Min(0f)] private float musicFadeOutDuration = 3f;
        [SerializeField] private AudioCueSO triggerPullCue;
        [SerializeField] private AudioCueSO trueEndingCue;
        [SerializeField] private AudioCueSO badEndingCue;

        [Header("Ending")]
        [SerializeField] private EndingController endingController;

        [Header("Optional UI")]
        [SerializeField] private GameObject[] uiObjectsToHideOnStart;

        [Header("Events")]
        [SerializeField] private UnityEvent onSequenceStarted;
        [SerializeField] private UnityEvent onAimReached;
        [SerializeField] private UnityEvent onShotMoment;
        [SerializeField] private UnityEvent onTrueShot;
        [SerializeField] private UnityEvent onBadShot;
        [SerializeField] private UnityEvent onEndingRequested;

        private Coroutine sequenceRoutine;
        private bool hasPlayed;

        private enum LocalAxis
        {
            PositiveX,
            NegativeX,
            PositiveY,
            NegativeY,
            PositiveZ,
            NegativeZ
        }

        private struct LocalPose
        {
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }

            public LocalPose(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }
        }

        private void Awake()
        {
            HideEndingGunImmediate();
            ValidateReferences();
        }

        public void PlayTrueEnding()
        {
            PlaySequence(true);
        }

        public void PlayBadEnding()
        {
            PlaySequence(false);
        }

        private void PlaySequence(bool isTrueEnding)
        {
            if (playOnlyOnce && hasPlayed)
                return;

            if (sequenceRoutine != null)
                return;

            sequenceRoutine = StartCoroutine(PlaySequenceRoutine(isTrueEnding));
        }

        private IEnumerator PlaySequenceRoutine(bool isTrueEnding)
        {
            hasPlayed = true;

            LockPlayerControl();
            HideWorldRevolver();
            SetObjectsActive(uiObjectsToHideOnStart, false);
            PrepareEndingGun();
            FadeOutMusic();
            onSequenceStarted?.Invoke();

            yield return MoveGunToAimRoutine();

            onAimReached?.Invoke();

            if (holdBeforeShot > 0f)
                yield return new WaitForSecondsRealtime(holdBeforeShot);

            PlayShotAudio(isTrueEnding);
            onShotMoment?.Invoke();

            if (isTrueEnding)
                onTrueShot?.Invoke();
            else
                onBadShot?.Invoke();

            if (endingDelayAfterShot > 0f)
                yield return new WaitForSecondsRealtime(endingDelayAfterShot);

            onEndingRequested?.Invoke();

            if (endingController != null)
                endingController.ShowEnding();
            else
                Debug.LogError($"{nameof(RevolverEndingSequence)} is missing Ending Controller.", this);

            sequenceRoutine = null;
        }

        private void LockPlayerControl()
        {
            if (playerModeController != null)
                playerModeController.EnterReadingText();

            if (!hideCursorDuringSequence)
                return;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void HideWorldRevolver()
        {
            SetObjectsActiveSafely(worldObjectsToHide, false);
            SetRenderersEnabled(worldRenderersToDisable, false);
            SetCollidersEnabled(worldCollidersToDisable, false);
        }

        private void HideEndingGunImmediate()
        {
            GameObject gunRootObject = GetEndingGunRootObject();

            if (gunRootObject == null)
                return;

            if (WouldDisableThisComponent(gunRootObject))
            {
                Debug.LogWarning($"{nameof(RevolverEndingSequence)} should not be placed on Ending Gun Root or its child.", this);
                return;
            }

            gunRootObject.SetActive(false);
        }

        private void PrepareEndingGun()
        {
            GameObject gunRootObject = GetEndingGunRootObject();

            if (gunRootObject != null)
                gunRootObject.SetActive(true);

            Transform gunTransform = GetGunTransform();

            if (gunTransform == null)
                return;

            // 一开始先把镜头前枪放到起始姿势，接下来再插值到瞄准自己的姿势。
            ApplyPose(gunTransform, GetStartPose());
        }

        private IEnumerator MoveGunToAimRoutine()
        {
            Transform gunTransform = GetGunTransform();

            if (gunTransform == null)
                yield break;

            LocalPose start = GetStartPose();
            LocalPose target = GetAimPose();
            float duration = Mathf.Max(0f, aimDuration);

            if (duration <= 0f)
            {
                ApplyPose(gunTransform, target);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float easedT = aimCurve != null ? aimCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

                gunTransform.localPosition = Vector3.Lerp(start.Position, target.Position, easedT);
                gunTransform.localRotation = Quaternion.Slerp(start.Rotation, target.Rotation, easedT);

                if (animateScale)
                    gunTransform.localScale = Vector3.Lerp(start.Scale, target.Scale, easedT);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ApplyPose(gunTransform, target);
        }

        private void PlayShotAudio(bool isTrueEnding)
        {
            PlayCue(triggerPullCue);
            PlayCue(isTrueEnding ? trueEndingCue : badEndingCue);
        }

        private void FadeOutMusic()
        {
            if (!fadeOutMusicOnStart || musicPlayer == null)
                return;

            musicPlayer.StopMusic(musicFadeOutDuration);
        }

        private void PlayCue(AudioCueSO cue)
        {
            if (audioCuePlayer == null || cue == null)
                return;

            audioCuePlayer.Play(cue);
        }

        private LocalPose GetStartPose()
        {
            return GetPose(startPose, startLocalPosition, startLocalEulerAngles, startLocalScale);
        }

        private LocalPose GetAimPose()
        {
            LocalPose pose = GetPose(aimPose, aimLocalPosition, aimLocalEulerAngles, aimLocalScale);

            if (!aimAtCameraOnFinalPose)
                return pose;

            return AlignPoseToAimTarget(pose);
        }

        private LocalPose GetPose(Transform poseTransform, Vector3 fallbackPosition, Vector3 fallbackEulerAngles, Vector3 fallbackScale)
        {
            Transform gunTransform = GetGunTransform();
            Transform parent = gunTransform != null ? gunTransform.parent : null;

            if (poseTransform == null)
                return new LocalPose(fallbackPosition, Quaternion.Euler(fallbackEulerAngles), fallbackScale);

            if (gunTransform != null && (poseTransform == gunTransform || poseTransform.IsChildOf(gunTransform)))
            {
                Debug.LogWarning($"{nameof(RevolverEndingSequence)} ignored {poseTransform.name} because pose markers should be siblings of the ending gun, not children of it.", poseTransform);
                return new LocalPose(fallbackPosition, Quaternion.Euler(fallbackEulerAngles), fallbackScale);
            }

            if (parent == null)
                return new LocalPose(poseTransform.position, poseTransform.rotation, poseTransform.localScale);

            Vector3 localPosition = parent.InverseTransformPoint(poseTransform.position);
            Quaternion localRotation = Quaternion.Inverse(parent.rotation) * poseTransform.rotation;

            return new LocalPose(localPosition, localRotation, poseTransform.localScale);
        }

        private LocalPose AlignPoseToAimTarget(LocalPose pose)
        {
            Transform gunTransform = GetGunTransform();
            Transform parent = gunTransform != null ? gunTransform.parent : null;

            if (!TryGetAimTargetPosition(parent, out Vector3 targetPosition))
                return pose;

            Vector3 toTarget = targetPosition - pose.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
                return pose;

            Vector3 muzzleAxis = GetLocalAxisVector(muzzleLocalAxis);
            Quaternion axisCorrection = Quaternion.FromToRotation(muzzleAxis, Vector3.forward);

            // 在相机局部空间里看向相机原点，再把模型实际枪口轴修正到 LookRotation 的 forward 轴。
            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up) * axisCorrection;

            return new LocalPose(pose.Position, targetRotation, pose.Scale);
        }

        private bool TryGetAimTargetPosition(Transform poseParent, out Vector3 targetPosition)
        {
            if (poseParent != null)
            {
                targetPosition = aimTarget != null ? poseParent.InverseTransformPoint(aimTarget.position) : Vector3.zero;
                return true;
            }

            if (aimTarget != null)
            {
                targetPosition = aimTarget.position;
                return true;
            }

            targetPosition = Vector3.zero;
            return false;
        }

        private static Vector3 GetLocalAxisVector(LocalAxis axis)
        {
            switch (axis)
            {
                case LocalAxis.PositiveX:
                    return Vector3.right;
                case LocalAxis.NegativeX:
                    return Vector3.left;
                case LocalAxis.PositiveY:
                    return Vector3.up;
                case LocalAxis.NegativeY:
                    return Vector3.down;
                case LocalAxis.PositiveZ:
                    return Vector3.forward;
                case LocalAxis.NegativeZ:
                    return Vector3.back;
                default:
                    return Vector3.left;
            }
        }

        private void ApplyPose(Transform target, LocalPose pose)
        {
            target.localPosition = pose.Position;
            target.localRotation = pose.Rotation;

            if (animateScale)
                target.localScale = pose.Scale;
        }

        private Transform GetGunTransform()
        {
            if (endingGunTransform != null)
                return endingGunTransform;

            return endingGunRoot != null ? endingGunRoot.transform : null;
        }

        private GameObject GetEndingGunRootObject()
        {
            if (endingGunRoot != null)
                return endingGunRoot;

            return endingGunTransform != null ? endingGunTransform.gameObject : null;
        }

        private void SetObjectsActiveSafely(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            foreach (GameObject target in targets)
            {
                if (target == null)
                    continue;

                if (WouldDisableThisComponent(target))
                {
                    Debug.LogWarning($"{nameof(RevolverEndingSequence)} skipped hiding {target.name} because it would disable the sequence component.", this);
                    continue;
                }

                target.SetActive(active);
            }
        }

        private bool WouldDisableThisComponent(GameObject target)
        {
            return target == gameObject || transform.IsChildOf(target.transform);
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

        private static void SetRenderersEnabled(Renderer[] targets, bool enabled)
        {
            if (targets == null)
                return;

            foreach (Renderer target in targets)
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

        private void ValidateReferences()
        {
            if (playerModeController == null)
                Debug.LogWarning($"{nameof(RevolverEndingSequence)} has no Player Mode Controller. Sequence will not lock player control.", this);

            if (endingGunRoot == null && endingGunTransform == null)
                Debug.LogError($"{nameof(RevolverEndingSequence)} needs Ending Gun Root or Ending Gun Transform.", this);

            if (audioCuePlayer == null)
                Debug.LogWarning($"{nameof(RevolverEndingSequence)} has no Audio Cue Player. Shot sounds must be handled by UnityEvents.", this);

            if (endingController == null)
                Debug.LogWarning($"{nameof(RevolverEndingSequence)} has no Ending Controller. Use On Ending Requested if another object shows the ending UI.", this);
        }
    }
}
