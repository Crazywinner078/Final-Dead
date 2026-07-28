using Dangaronpo.Player;
using UnityEngine;

namespace Dangaronpo.Interaction
{
    /// <summary>
    /// 调试用交互物体，只在控制台输出焦点和交互日志，方便验证射线检测是否正常。
    /// </summary>
    public class DebugInteractable : InteractableBase
    {
        public override void OnFocus()
        {
            Debug.Log($"Focus: {DisplayName}", this);
        }

        public override void OnUnfocus()
        {
            Debug.Log($"Unfocus: {DisplayName}", this);
        }

        public override void Interact(PlayerInteractor playerInteractor)
        {
            if (!CanInteract)
            {
                Debug.Log($"{DisplayName} cannot be interacted with right now.", this);
                return;
            }

            Debug.Log($"Interact: {DisplayName}", this);
        }
    }
}
