using System.Collections;
using Dangaronpo.UI;
using UnityEngine;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 结局触发入口。左轮的真假结局事件都调用这里，最后显示同一套黑屏结局 UI。
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        [SerializeField] private EndingUI endingUI;
        [SerializeField, Min(0f)] private float showDelay;

        private Coroutine showRoutine;
        private bool hasShown;

        public void ShowEnding()
        {
            if (hasShown)
                return;

            if (endingUI == null)
            {
                Debug.LogError($"{nameof(EndingController)} is missing Ending UI.", this);
                return;
            }

            hasShown = true;

            if (showDelay <= 0f)
            {
                endingUI.Show();
                return;
            }

            showRoutine = StartCoroutine(ShowEndingAfterDelay());
        }

        private IEnumerator ShowEndingAfterDelay()
        {
            yield return new WaitForSecondsRealtime(showDelay);
            endingUI.Show();
            showRoutine = null;
        }

        private void OnDisable()
        {
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }
        }
    }
}
