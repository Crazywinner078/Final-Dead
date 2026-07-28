using Dangaronpo.Player;

namespace Dangaronpo.Interaction
{
    /// <summary>
    /// 所有可交互物体的统一入口。玩家射线只认识这个接口，不关心目标到底是道具、抽屉还是谜题。
    /// </summary>
    public interface IInteractable
    {
        string DisplayName { get; }
        string PromptText { get; }
        bool CanInteract { get; }

        string GetPromptText(PlayerInteractor playerInteractor);
        void OnFocus();
        void OnUnfocus();
        void Interact(PlayerInteractor playerInteractor);
    }
}
