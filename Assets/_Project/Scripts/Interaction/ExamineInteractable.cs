using Dangaronpo.Interaction;
using Dangaronpo.UI;
using System.Collections;
using System.Collections.Generic;
using Dangaronpo.Player;
using UnityEngine;

namespace Dangaronpo.Interaction
{
    /// <summary>
    /// 最简单的调查物体：按 E 后只显示一段调查文本。
    /// </summary>
    public class ExamineInteractable : InteractableBase
    {
        [SerializeField] private InvestigationUI investigationUI;

        [SerializeField, TextArea(2, 6)]
        private string examineText = "柜门半开着，里面的横杆上似乎少了什么东西。";

        public override void Interact(PlayerInteractor interactor)
        {
            if (investigationUI == null)
            {
                Debug.LogError($"{nameof(ExamineInteractable)} is missing Investigation UI.", this);
                return;
            }

            // 文本 UI 自己会锁住玩家移动和镜头，这里只负责把内容交过去。
            investigationUI.Show(examineText);
        }
    }
}
