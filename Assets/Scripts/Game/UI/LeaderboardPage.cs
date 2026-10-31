using System.Collections.Generic;
using BallSort.Core.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Home's Leaderboard page: the player at rank 1 with their saved name and avatar, then the fixed
    /// bots of <see cref="LeaderboardConfig"/>; the top three avatars also sit on the podium tubes drawn
    /// at the top of the panel. No backend: nothing is fetched or scored.
    /// </summary>
    /// <remarks>
    /// Rows are made once from a template when the page is initialized; the page refreshes whenever it
    /// is shown, which is the only time the name or avatar (changed in the avatar menu) can differ.
    /// </remarks>
    public sealed class LeaderboardPage : MonoBehaviour
    {
        [SerializeField] private LeaderboardConfig _config;

        [Tooltip("An inactive row in the rows container, copied once per entry.")]
        [SerializeField] private LeaderboardRow _rowTemplate;

        [SerializeField] private Sprite _playerRowSprite;
        [SerializeField] private Sprite _otherRowSprite;

        [Tooltip("The avatar pictures on the podium tubes: element 0 is rank 1 (middle tube), then 2 (left) and 3 (right).")]
        [SerializeField] private Image[] _podium = new Image[0];

        private readonly List<LeaderboardRow> _rows = new List<LeaderboardRow>();
        private PlayerData _player;
        private AvatarConfig _avatars;

        public void Initialize(PlayerData player, AvatarConfig avatars)
        {
            _player = player;
            _avatars = avatars;
            if (_rows.Count == 0)
            {
                BuildRows();
            }

            Refresh();
        }

        private void BuildRows()
        {
            _rowTemplate.gameObject.SetActive(false);
            Transform container = _rowTemplate.transform.parent;
            int count = 1 + _config.Bots.Count;
            for (int i = 0; i < count; i++)
            {
                LeaderboardRow row = Instantiate(_rowTemplate, container);
                row.name = $"Row{i + 1}";
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
        }

        private void Refresh()
        {
            if (_player == null)
            {
                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                bool isPlayer = i == 0;
                LeaderboardBot bot = isPlayer ? default : _config.Bots[i - 1];
                string entryName = isPlayer ? _player.PlayerName : bot.Name;
                Sprite avatar = _avatars.GetPicture(isPlayer ? _player.AvatarId : bot.AvatarId);
                _rows[i].Show(i + 1, entryName, avatar, isPlayer ? _playerRowSprite : _otherRowSprite);

                if (i < _podium.Length)
                {
                    _podium[i].sprite = avatar;
                    _podium[i].enabled = avatar != null;
                }
            }
        }

        private void OnEnable() => Refresh();

        private void Awake()
        {
            if (_config == null || _rowTemplate == null || _playerRowSprite == null || _otherRowSprite == null)
            {
                Debug.LogError($"{name}: config, row template and both row sprites must be assigned.", this);
            }

            foreach (Image picture in _podium)
            {
                if (picture == null)
                {
                    Debug.LogError($"{name}: every podium picture must be assigned.", this);
                    return;
                }
            }
        }
    }
}
