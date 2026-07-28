using Dangaronpo.Data;
using UnityEngine;

namespace Dangaronpo.Player
{
    /// <summary>
    /// 手持物品显示器。监听背包的 HeldItemChanged，把对应 HeldPrefab 生成到右手/镜头挂点下。
    /// </summary>
    public class HeldItemView : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private Transform heldItemRoot;

        private GameObject currentHeldInstance;

        private void Reset()
        {
            heldItemRoot = transform;
            playerInventory = GetComponentInParent<PlayerInventory>();
        }

        private void Awake()
        {
            if (heldItemRoot == null)
                heldItemRoot = transform;

            if (playerInventory == null)
                playerInventory = GetComponentInParent<PlayerInventory>();

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (playerInventory == null)
                return;

            // 事件驱动刷新，比在 Update 里每帧检查当前手持物品更干净。
            playerInventory.HeldItemChanged += RefreshHeldItem;
            RefreshHeldItem(playerInventory.HeldItem);
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.HeldItemChanged -= RefreshHeldItem;

            ClearHeldItem();
        }

        private void RefreshHeldItem(ItemDataSO item)
        {
            ClearHeldItem();

            if (item == null)
                return;

            if (item.HeldPrefab == null)
            {
                Debug.LogWarning($"{item.DisplayName} has no held prefab assigned.", this);
                return;
            }

            // 手持 prefab 自己负责摆好角度和大小，这里只把它放到挂点原点。
            currentHeldInstance = Instantiate(item.HeldPrefab, heldItemRoot);
            currentHeldInstance.transform.localPosition = Vector3.zero;
            currentHeldInstance.transform.localRotation = Quaternion.identity;
            currentHeldInstance.transform.localScale = Vector3.one;
        }

        private void ClearHeldItem()
        {
            if (currentHeldInstance == null)
                return;

            Destroy(currentHeldInstance);
            currentHeldInstance = null;
        }

        private void ValidateReferences()
        {
            if (playerInventory == null)
                Debug.LogError($"{nameof(HeldItemView)} is missing Player Inventory.", this);

            if (heldItemRoot == null)
                Debug.LogError($"{nameof(HeldItemView)} is missing Held Item Root.", this);
        }
    }
}
