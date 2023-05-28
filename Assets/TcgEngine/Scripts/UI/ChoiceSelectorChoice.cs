using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// One choice in the choice selector
    /// Its a button you can click
    /// </summary>

    public class ChoiceSelectorChoice : MonoBehaviour
    {
        public Text title;
        public Text subtitle;
        public Image highlight;

        [HideInInspector]
        public int choice;

        public UnityAction<int> onClick;

        private bool focus = false;

        private void Awake()
        {

        }

        private void Update()
        {
            if (highlight != null)
                highlight.enabled = focus;
        }

        public void SetChoice(int index, string title, string sub)
        {
            this.choice = index;
            this.title.text = title;
            this.subtitle.text = sub;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnClick()
        {
            onClick?.Invoke(choice);
        }

        public void MouseEnter()
        {
            focus = true;
        }

        public void MouseExit()
        {
            focus = false;
        }
    }
}
