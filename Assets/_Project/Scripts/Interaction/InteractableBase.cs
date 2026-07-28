using Dangaronpo.Player;
using UnityEngine;

namespace Dangaronpo.Interaction
{
    /// <summary>
    /// 交互物体的基础类，集中保存提示文本和交互开关，具体行为交给子类实现。
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Object";
        [SerializeField] private string promptText = "Investigate";
        [SerializeField] private bool canInteract = true;

        public string DisplayName => displayName;
        public string PromptText => promptText;
        public bool CanInteract => canInteract;

        public virtual string GetPromptText(PlayerInteractor playerInteractor)
        {
            return promptText;
        }

        public virtual void OnFocus()
        {
            // 子类可以重写这里做描边、高亮或音效；默认不做任何事。
        }

        public virtual void OnUnfocus()
        {
            // 子类可以重写这里关闭描边、高亮或临时提示；默认不做任何事。
        }

        public abstract void Interact(PlayerInteractor playerInteractor);
    }
}
