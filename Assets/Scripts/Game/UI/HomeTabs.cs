using System;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Home's bottom navigation: each tab shows its page and its highlight and hides the others. Home is
    /// selected on arriving; Leaderboard and Shop are placeholder pages.
    /// </summary>
    public sealed class HomeTabs : MonoBehaviour
    {
        [SerializeField] private Tab[] _tabs = new Tab[0];

        [Tooltip("Index in Tabs of the tab selected on arriving (Home).")]
        [SerializeField, Min(0)] private int _startTab = 1;

        public void Initialize()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;

                // Selection shows through the highlight, never by disabling a tab: a tab saved as not
                // interactable (the old "always selected" Home look) would ignore every tap.
                _tabs[i].Button.interactable = true;
                _tabs[i].Button.onClick.AddListener(() => Select(index));
            }

            Select(_startTab);
        }

        public void Select(int index)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].Page.SetActive(i == index);
                _tabs[i].Highlight.SetActive(i == index);
            }
        }

        private void Awake()
        {
            foreach (Tab tab in _tabs)
            {
                if (tab == null || tab.Button == null || tab.Page == null || tab.Highlight == null)
                {
                    Debug.LogError($"{name}: every tab needs a button, a page and a highlight.", this);
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Tab tab in _tabs)
            {
                if (tab?.Button != null)
                {
                    tab.Button.onClick.RemoveAllListeners();
                }
            }
        }

        [Serializable]
        private sealed class Tab
        {
            [SerializeField] private Button _button;

            [Tooltip("Shown while this tab is selected.")]
            [SerializeField] private GameObject _page;

            [Tooltip("The selected-tab highlight (an art slot), shown while this tab is selected.")]
            [SerializeField] private GameObject _highlight;

            public Button Button => _button;

            public GameObject Page => _page;

            public GameObject Highlight => _highlight;
        }
    }
}
