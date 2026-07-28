using TMPro;
using UnityEngine;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 屏幕中心交互提示。由 PlayerInteractor 在射线目标变化时显示或隐藏。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private string interactKey = "E";

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            Hide();
        }

        public void Show(string displayName, string promptTextValue)
        {
            if (promptText == null)
            {
                Debug.LogError($"{nameof(InteractionPromptUI)} is missing Prompt Text.", this);
                return;
            }

            if (!EnsureCanvasGroup())
                return;

            // promptTextValue 来自交互物体，例如 Investigate / Pick Up / Open。
            promptText.text = $"[{interactKey}] {promptTextValue} {displayName}";

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Hide()
        {
            if (!EnsureCanvasGroup())
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private bool EnsureCanvasGroup()
        {
            if (canvasGroup != null)
                return true;

            if (TryGetComponent(out canvasGroup))
                return true;

            Debug.LogError($"{nameof(InteractionPromptUI)} requires a CanvasGroup on the same GameObject.", this);
            return false;
        }
    }
}
