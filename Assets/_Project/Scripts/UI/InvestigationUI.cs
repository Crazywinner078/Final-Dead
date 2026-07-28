using System.Collections;
using System.Collections.Generic;
using Dangaronpo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Events;


namespace Dangaronpo.UI
{
    /// <summary>
    /// 调查文本面板。显示场景调查结果，并在打开时锁住玩家移动和视角。
    /// </summary>
    public class InvestigationUI : MonoBehaviour
    {
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private UnityEvent onShown;
        [SerializeField] private UnityEvent onHidden;

        private CanvasGroup canvasGroup;

        public bool isOpen {  get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            Hide();
        }
        private void Update()
        {
            if(!isOpen)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }
        public void Show(string text)
        {
            if(bodyText==null)
            {
                Debug.LogError($"{nameof(InvestigationUI)} is missing Body Text.", this);
                return;
            }
            // 只负责显示传入文本，具体文本来源由 Examine/Drawer/Puzzle 等交互脚本决定。
            bodyText.text = text;

            isOpen = true;
            canvasGroup.alpha = 1.0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (playerModeController != null)
                playerModeController.EnterReadingText();
            else
                Debug.LogError($"{nameof(InvestigationUI)} is missing Player Mode Controller.", this);

            onShown?.Invoke();
        }

        public void Hide()
        {
            bool wasOpen = isOpen;
            isOpen = false;


            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (playerModeController != null)
                // 关闭文本后恢复自由移动。
                playerModeController.ExitReadingText();

            if (wasOpen)
                onHidden?.Invoke();
        }
    }
}
