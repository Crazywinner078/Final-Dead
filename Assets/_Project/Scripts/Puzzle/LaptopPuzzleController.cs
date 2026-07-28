using Dangaronpo.Interaction;
using Dangaronpo.Player;
using Dangaronpo.UI;
using Dangaronpo.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 笔记本电脑谜题。要求手持指定道具后输入密码，成功后显示线索图片。
    /// </summary>
    public class LaptopPuzzleController : InteractableBase
    {
        [SerializeField] private ItemDataSO requiredHeldItem;
        [SerializeField] private InvestigationUI investigationUI;
        [SerializeField, TextArea(2, 4)] private string missingRequiredItemText = "You need to hold the USB before using this laptop.";
        [SerializeField] private LaptopPasswordUI laptopPasswordUI;
        [SerializeField] private ClueImageUI clueImageUI;
        [SerializeField] private Sprite clueImage;
        [SerializeField] private string clueTitle = "线索";
        [SerializeField] private string password = "0000";
        [SerializeField] private bool showClueAgainAfterSolved = true;
        [SerializeField] private UnityEvent onMissingRequiredItem;
        [SerializeField] private UnityEvent onPasswordAccepted;
        [SerializeField] private UnityEvent onPasswordRejected;

        private bool isSolved;

        public override void Interact(PlayerInteractor playerInteractor)
        {
            if (playerInteractor == null)
            {
                Debug.LogError($"{nameof(LaptopPuzzleController)} cannot find Player Interactor.", this);
                return;
            }

            if (isSolved)
            {
                // 解开后再次调查可以重复查看线索，避免玩家错过关键信息。
                if (showClueAgainAfterSolved)
                    ShowClue();

                return;
            }

            if (!HasRequiredHeldItem(playerInteractor))
            {
                // 和抽屉一样，这里要求“手持 U 盘”，不是只要背包里有 U 盘。
                ShowMissingRequiredItemMessage();
                onMissingRequiredItem?.Invoke();
                return;
            }

            if (laptopPasswordUI == null)
            {
                Debug.LogError($"{nameof(LaptopPuzzleController)} is missing Laptop Password UI.", this);
                return;
            }

            laptopPasswordUI.Show(this);
        }

        public bool TrySubmitPassword(string inputPassword)
        {
            if (isSolved)
                return true;

            if (NormalizePassword(inputPassword) != NormalizePassword(password))
            {
                if (laptopPasswordUI != null)
                    laptopPasswordUI.ShowFeedback("密码错误");

                onPasswordRejected?.Invoke();
                return false;
            }

            isSolved = true;
            onPasswordAccepted?.Invoke();

            if (laptopPasswordUI != null)
                laptopPasswordUI.Hide();

            // 密码 UI 只负责输入，真正的线索展示由谜题控制器统一触发。
            ShowClue();
            return true;
        }

        public void ClosePuzzle()
        {
            if (laptopPasswordUI != null)
                laptopPasswordUI.Hide();
        }

        private void ShowClue()
        {
            if (clueImageUI == null)
            {
                Debug.LogError($"{nameof(LaptopPuzzleController)} is missing Clue Image UI.", this);
                return;
            }

            if (clueImage == null)
            {
                Debug.LogError($"{nameof(LaptopPuzzleController)} is missing Clue Image.", this);
                return;
            }

            clueImageUI.Show(clueImage, clueTitle);
        }

        private bool HasRequiredHeldItem(PlayerInteractor playerInteractor)
        {
            if (requiredHeldItem == null)
                return true;

            if (playerInteractor == null || playerInteractor.Inventory == null)
                return false;

            return playerInteractor.Inventory.HeldItem == requiredHeldItem;
        }

        private void ShowMissingRequiredItemMessage()
        {
            if (investigationUI != null)
            {
                investigationUI.Show(missingRequiredItemText);
                return;
            }

            Debug.Log(missingRequiredItemText, this);
        }

        private static string NormalizePassword(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
