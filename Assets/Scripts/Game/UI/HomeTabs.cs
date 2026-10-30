using System;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Home's bottom navigation: one bar image drawn with the selected tab raised, and an invisible tap
    /// area over each of its sections. Selecting a tab shows its page, hides the others and swaps the
    /// bar to that tab's version. Home is selected on arriving; Leaderboard and Shop are placeholder
    /// pages.
    /// </summary>
    public sealed class HomeTabs : MonoBehaviour
    {
        [Tooltip("The bar art; its sprite is swapped to the selected tab's version.")]
        [SerializeField] private Image _bar;
        [SerializeField] private Tab[] _tabs = new Tab[0];
        [Tooltip("Index in Tabs of the tab selected on arriving (Home).")]
        [SerializeField, Min(0)] private int _startTab = 1;

        public void Initialize()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                // Selection shows through the bar, never by disabling a tab: a tab saved as not
                // interactable would ignore every tap.
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
            }

            _bar.sprite = _tabs[index].BarSprite;
        }

        private void Awake()
        {
            if (_bar == null)
            {
                Debug.LogError($"{name}: the bar image must be assigned.", this);
                return;
            }

            foreach (Tab tab in _tabs)
            {
                if (tab == null || tab.Button == null || tab.Page == null || tab.BarSprite == null)
                {
                    Debug.LogError($"{name}: every tab needs a button, a page and a bar sprite.", this);
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
            [Tooltip("Invisible tap area over this tab's section of the bar.")]
            [SerializeField] private Button _button;
            [Tooltip("Shown while this tab is selected.")]
            [SerializeField] private GameObject _page;
            [Tooltip("The bar drawn with this tab selected (an art slot).")]
            [SerializeField] private Sprite _barSprite;

            public Button Button => _button;
            public GameObject Page => _page;
            public Sprite BarSprite => _barSprite;
        }
    }
}
