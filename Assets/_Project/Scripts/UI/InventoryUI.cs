using System.Collections.Generic;
using Dangaronpo.Data;
using Dangaronpo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 背包主界面。负责显示道具 slot、选择道具、调查、取出和道具组合。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private InventorySlotUI slotPrefab;
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private InventoryTooltipUI tooltipUI;
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI selectedDescriptionText;
        [SerializeField] private GameObject actionPanel;
        [SerializeField] private RectTransform actionPanelRoot;
        [SerializeField] private Vector2 actionPanelOffset = new Vector2(12f, -12f);
        [SerializeField] private Button examineButton;
        [SerializeField] private Button takeOutButton;
        [SerializeField] private TextMeshProUGUI takeOutButtonText;
        [SerializeField] private string takeOutButtonLabel = "取出";
        [SerializeField] private string putAwayButtonLabel = "收起";
        [SerializeField] private Button combineButton;
        [SerializeField] private InvestigationUI investigationUI;
        [SerializeField] private ClueImageUI clueImageUI;
        [SerializeField] private List<ItemCombinationRecipeSO> combinationRecipes = new List<ItemCombinationRecipeSO>();
        [SerializeField] private bool logDebugInfo;

        private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
        private CanvasGroup canvasGroup;
        private ItemDataSO combineSourceItem;
        private RectTransform selectedSlotTransform;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            BindButtons();
            SetVisible(false);
            SetActionPanelVisible(false);
            ValidateReferences();
        }

        private void OnEnable()
        {
            if (playerInventory == null)
                return;

            // 背包内容和选中状态都由 PlayerInventory 事件驱动，UI 不直接改底层列表。
            playerInventory.ItemsChanged += RefreshSlots;
            playerInventory.SelectedItemChanged += RefreshSelectedItem;
        }

        private void OnDisable()
        {
            if (playerInventory == null)
                return;

            playerInventory.ItemsChanged -= RefreshSlots;
            playerInventory.SelectedItemChanged -= RefreshSelectedItem;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                Toggle();

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (!CanOpen())
                return;

            IsOpen = true;
            // 每次打开都重建 slot，保证拾取、消耗、合成后的数量显示是最新的。
            RefreshSlots();
            RefreshSelectedItem(playerInventory != null ? playerInventory.SelectedItem : null);
            SetVisible(true);

            if (playerModeController != null)
                playerModeController.EnterInventory();
        }

        public void Close()
        {
            IsOpen = false;
            // 关闭背包时重置临时操作状态，避免下次打开还残留组合模式或选中高亮。
            ClearCombineMode();
            tooltipUI?.Hide();
            SetActionPanelVisible(false);
            selectedSlotTransform = null;

            if (playerInventory != null && playerInventory.SelectedItem != null)
                playerInventory.SelectItem(null);

            SetVisible(false);

            if (playerModeController != null && playerModeController.CurrentMode == PlayerModeController.PlayerMode.Inventory)
                playerModeController.ExitInventory();
        }

        private bool CanOpen()
        {
            if (playerInventory == null)
            {
                Debug.LogError($"{nameof(InventoryUI)} is missing Player Inventory.", this);
                return false;
            }

            if (playerModeController == null)
                return true;

            return playerModeController.CurrentMode == PlayerModeController.PlayerMode.FreeLook
                || playerModeController.CurrentMode == PlayerModeController.PlayerMode.Inventory;
        }

        private void RefreshSlots()
        {
            ClearSlots();

            if (playerInventory == null || slotPrefab == null || slotsRoot == null)
                return;

            if (logDebugInfo)
                Debug.Log($"{nameof(InventoryUI)} refreshing slots. Item count: {playerInventory.Items.Count}", this);

            foreach (ItemDataSO item in playerInventory.Items)
            {
                InventorySlotUI slot = Instantiate(slotPrefab, slotsRoot);
                slot.gameObject.SetActive(true);
                // slot 只负责显示和回传点击事件，具体选择/组合逻辑留在 InventoryUI。
                slot.Setup(item, playerInventory.GetItemCount(item), HandleSlotClicked);
                slots.Add(slot);
            }

            RefreshSelection();
        }

        private void ClearSlots()
        {
            foreach (InventorySlotUI slot in slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }

            slots.Clear();
            selectedSlotTransform = null;
        }

        private void HandleSlotClicked(ItemDataSO item, RectTransform slotTransform)
        {
            if (playerInventory == null)
                return;

            tooltipUI?.Hide();

            if (combineSourceItem != null)
            {
                // 已经点过“组合”后，再点另一个道具就尝试用两者查配方。
                TryCombine(combineSourceItem, item);
                return;
            }

            // 普通点击只改变选中项，并把行动面板移动到该 slot 右下角。
            selectedSlotTransform = slotTransform;
            playerInventory.SelectItem(item);
            MoveActionPanelToSlot(slotTransform);
        }

        private void RefreshSelectedItem(ItemDataSO selectedItem)
        {
            if (selectedItem == null)
                selectedSlotTransform = null;

            RefreshSelection();

            if (selectedNameText != null)
                selectedNameText.text = GetSelectedItemDisplayName(selectedItem);

            if (selectedDescriptionText != null)
                selectedDescriptionText.text = selectedItem != null ? selectedItem.Description : "Click an item to select it.";

            SetActionPanelVisible(selectedItem != null);
            RefreshActionButtons(selectedItem);
        }

        private void RefreshSelection()
        {
            ItemDataSO selectedItem = playerInventory != null ? playerInventory.SelectedItem : null;

            foreach (InventorySlotUI slot in slots)
            {
                bool isSelected = slot.ItemData == selectedItem;
                slot.SetSelected(isSelected);

                if (isSelected && selectedSlotTransform == null)
                    selectedSlotTransform = slot.transform as RectTransform;
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void BindButtons()
        {
            if (takeOutButtonText == null && takeOutButton != null)
                takeOutButtonText = takeOutButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (examineButton != null)
                examineButton.onClick.AddListener(ExamineSelectedItem);

            if (takeOutButton != null)
                takeOutButton.onClick.AddListener(TakeOutSelectedItem);

            if (combineButton != null)
                combineButton.onClick.AddListener(StartCombineMode);
        }

        private void ExamineSelectedItem()
        {
            ItemDataSO selectedItem = playerInventory != null ? playerInventory.SelectedItem : null;

            if (selectedItem == null)
                return;

            if (selectedDescriptionText != null)
                selectedDescriptionText.text = selectedItem.ExamineText;

            if (selectedItem.ExamineImage != null)
            {
                // 有线索图的道具优先展示图片，例如纸条、照片、相机内容。
                ShowClueImage(selectedItem);
                return;
            }

            // 没有图片时，用 tooltip 作为“调查文本弹窗”。
            ShowTooltipMessage(selectedItem.DisplayName, selectedItem.ExamineText, selectedSlotTransform);
        }

        private void TakeOutSelectedItem()
        {
            ItemDataSO selectedItem = playerInventory != null ? playerInventory.SelectedItem : null;

            if (selectedItem == null || !selectedItem.CanTakeOut)
                return;

            tooltipUI?.Hide();

            if (playerInventory.HeldItem == selectedItem)
            {
                // 已经拿在手上的道具，再点同一个按钮就收回背包。
                playerInventory.HoldItem(null);
                Close();
                return;
            }

            // 取出后关闭背包，让玩家回到第一人称状态并拿着该道具和场景互动。
            playerInventory.HoldItem(selectedItem);
            Close();
        }

        private void StartCombineMode()
        {
            ItemDataSO selectedItem = playerInventory != null ? playerInventory.SelectedItem : null;

            if (selectedItem == null || !selectedItem.CanCombine)
                return;

            tooltipUI?.Hide();
            combineSourceItem = selectedItem;

            if (selectedDescriptionText != null)
                // 组合模式只记录第一个道具，第二个道具由下一次 slot 点击决定。
                selectedDescriptionText.text = $"Choose an item to combine with {selectedItem.DisplayName}.";
        }

        private void TryCombine(ItemDataSO firstItem, ItemDataSO secondItem)
        {
            if (playerInventory == null)
                return;

            if (firstItem == null || secondItem == null || firstItem == secondItem)
            {
                ClearCombineMode();
                ShowInvestigation("Cannot combine these items.");
                return;
            }

            ItemCombinationRecipeSO recipe = FindRecipe(firstItem, secondItem);

            if (recipe == null)
            {
                ClearCombineMode();
                ShowInvestigation("Cannot combine these items.");
                return;
            }

            ApplyRecipe(recipe, firstItem, secondItem);
            ClearCombineMode();
        }

        private ItemCombinationRecipeSO FindRecipe(ItemDataSO firstItem, ItemDataSO secondItem)
        {
            foreach (ItemCombinationRecipeSO recipe in combinationRecipes)
            {
                if (recipe != null && recipe.Matches(firstItem, secondItem))
                    return recipe;
            }

            return null;
        }

        private void ApplyRecipe(ItemCombinationRecipeSO recipe, ItemDataSO firstItem, ItemDataSO secondItem)
        {
            if (recipe.ResultItem == null)
            {
                Debug.LogError($"{nameof(InventoryUI)} recipe is missing Result Item.", recipe);
                return;
            }

            if (recipe.ShouldConsume(firstItem))
                playerInventory.RemoveItem(firstItem);

            if (recipe.ShouldConsume(secondItem))
                playerInventory.RemoveItem(secondItem);

            Close();
            // 新道具通过 AddItem 进入背包，因此也会触发拾取确认 UI 和拾取音效。
            playerInventory.AddItem(recipe.ResultItem);
        }

        private void ClearCombineMode()
        {
            combineSourceItem = null;
        }

        private void SetActionPanelVisible(bool visible)
        {
            if (actionPanel != null)
                actionPanel.SetActive(visible);
        }

        private void RefreshActionButtons(ItemDataSO selectedItem)
        {
            if (examineButton != null)
                examineButton.gameObject.SetActive(selectedItem != null);

            if (takeOutButton != null)
            {
                takeOutButton.gameObject.SetActive(selectedItem != null && selectedItem.CanTakeOut);

                if (takeOutButtonText != null)
                    takeOutButtonText.text = playerInventory != null && selectedItem != null && playerInventory.HeldItem == selectedItem
                        ? putAwayButtonLabel
                        : takeOutButtonLabel;
            }

            if (combineButton != null)
                combineButton.gameObject.SetActive(selectedItem != null && selectedItem.CanCombine);
        }

        private void MoveActionPanelToSlot(RectTransform slotTransform)
        {
            if (slotTransform == null)
                return;

            RectTransform targetPanel = actionPanelRoot != null
                ? actionPanelRoot
                : actionPanel != null ? actionPanel.transform as RectTransform : null;

            if (targetPanel == null)
                return;

            Vector3[] corners = new Vector3[4];
            slotTransform.GetWorldCorners(corners);

            // GetWorldCorners: 0 左下，1 左上，2 右上，3 右下。
            Vector3 bottomRight = corners[3];
            targetPanel.position = bottomRight + new Vector3(actionPanelOffset.x, actionPanelOffset.y, 0f);
        }

        private void ShowTooltipMessage(string title, string description, RectTransform anchor)
        {
            if (tooltipUI != null)
                tooltipUI.ShowText(title, description, anchor);
            else
                Debug.Log($"{title}: {description}", this);
        }

        private void ShowInvestigation(string text)
        {
            Close();

            if (investigationUI != null)
                investigationUI.Show(text);
            else
                Debug.Log(text, this);
        }

        private void ShowClueImage(ItemDataSO item)
        {
            Close();

            if (clueImageUI != null)
                clueImageUI.Show(item.ExamineImage, item.DisplayName);
            else
                Debug.LogError($"{nameof(InventoryUI)} is missing Clue Image UI.", this);
        }

        private void ValidateReferences()
        {
            if (playerInventory == null)
                Debug.LogError($"{nameof(InventoryUI)} is missing Player Inventory.", this);

            if (slotPrefab == null)
                Debug.LogError($"{nameof(InventoryUI)} is missing Slot Prefab.", this);

            if (slotsRoot == null)
                Debug.LogError($"{nameof(InventoryUI)} is missing Slots Root.", this);

            if (tooltipUI == null)
                Debug.LogError($"{nameof(InventoryUI)} is missing Tooltip UI.", this);

            if (actionPanel == null)
                Debug.LogError($"{nameof(InventoryUI)} is missing Action Panel.", this);

            if (actionPanelRoot == null && actionPanel != null && actionPanel.transform as RectTransform == null)
                Debug.LogError($"{nameof(InventoryUI)} Action Panel needs a RectTransform.", this);

            if (investigationUI == null)
                Debug.LogError($"{nameof(InventoryUI)} is missing Investigation UI.", this);

            if (clueImageUI == null)
                Debug.LogWarning($"{nameof(InventoryUI)} has no Clue Image UI. Examine images will not be shown.", this);
        }

        private string GetSelectedItemDisplayName(ItemDataSO selectedItem)
        {
            if (selectedItem == null)
                return "No item selected";

            int quantity = playerInventory != null ? playerInventory.GetItemCount(selectedItem) : 1;

            if (quantity > 1)
                return $"{selectedItem.DisplayName} x{quantity}";

            return selectedItem.DisplayName;
        }
    }
}
