using UnityEngine;


namespace Dangaronpo.Player
{
    /// <summary>
    /// 玩家模式切换器。打开文本、背包、设置或特写时统一禁用移动/视角/交互。
    /// </summary>
    public class PlayerModeController : MonoBehaviour
    {
        /// <summary>
        /// FreeLook 是正常游玩状态，其它状态会锁住玩家控制并释放鼠标。
        /// </summary>
        public enum PlayerMode
        {
            FreeLook,
            ReadingText,
            Inventory,
            Settings
        }

        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private PlayerInteractor playerInteractor;

        public PlayerMode CurrentMode { get;private set; }=PlayerMode.FreeLook;


        private void Awake()
        {
            ValidateReferences();
            SetMode(PlayerMode.FreeLook);
        }
        public void EnterReadingText()
        {
            SetMode(PlayerMode.ReadingText);
        }
        public void ExitReadingText()
        {
            SetMode(PlayerMode.FreeLook);
        }

        public void EnterInventory()
        {
            SetMode(PlayerMode.Inventory);
        }

        public void ExitInventory()
        {
            SetMode(PlayerMode.FreeLook);
        }

        public void EnterSettings()
        {
            SetMode(PlayerMode.Settings);
        }

        public void ExitSettings()
        {
            SetMode(PlayerMode.FreeLook);
        }

        private void SetMode(PlayerMode mode)
        {
            CurrentMode = mode;

            bool isFreeLook = CurrentMode == PlayerMode.FreeLook;

            // 所有 UI/演出都走这里锁玩家，避免每个 UI 自己分别控制移动和镜头。
            if(playerMotor != null )
            { 
                playerMotor.enabled = isFreeLook;
            }
            if(playerLook != null )
            {
                playerLook.enabled = isFreeLook;
            }
            if (playerInteractor != null )
            {
                playerInteractor.enabled = isFreeLook;
            }
            Cursor.lockState = isFreeLook ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isFreeLook;
        }

        private void ValidateReferences()
        {
            if (playerMotor == null)
                Debug.LogError($"{nameof(PlayerModeController)} is missing Player Motor.", this);

            if (playerLook == null)
                Debug.LogError($"{nameof(PlayerModeController)} is missing Player Look.", this);

            if (playerInteractor == null)
                Debug.LogError($"{nameof(PlayerModeController)} is missing Player Interactor.", this);
        }
    }
}
