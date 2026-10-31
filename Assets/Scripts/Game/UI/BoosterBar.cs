using System;
using BallSort.Core.Commands;
using BallSort.Core.Player;
using BallSort.Core.Session;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Level screen's booster bar — Undo, Shuffle, Add tube — the boosters' display names, and the
    /// player-facing text for each reason a booster is unavailable (docs/screens.md).
    /// </summary>
    /// <remarks>
    /// A booster at 0 shows the buy badge and is never dimmed: tapping it opens the Buy popup, whatever
    /// else would refuse it. The whole bar takes no taps while locked (stuck, won, a shuffle pick).
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BoosterBar : MonoBehaviour
    {
        [SerializeField] private BoosterButton _undo;
        [SerializeField] private BoosterButton _shuffle;
        [SerializeField] private BoosterButton _addTube;
        [SerializeField] private CanvasGroup _group;

        [Header("Names (Buy popup: \"Buy {name}\")")]
        [SerializeField] private string _undoName = "Undo";
        [SerializeField] private string _shuffleName = "Shuffle";
        [SerializeField] private string _addTubeName = "Add tube";

        [Header("Pictures (Buy popup)")]
        [SerializeField] private Sprite _undoPicture;
        [SerializeField] private Sprite _shufflePicture;
        [SerializeField] private Sprite _addTubePicture;

        [Header("Refusal messages")]
        [SerializeField] private string _nothingToUndo = "Nothing to undo";
        [SerializeField] private string _undoSealed = "Can't undo past a completed tube";
        [SerializeField] private string _noRoomForTubes = "No room for more tubes";
        [SerializeField] private string _nothingToShuffle = "No tube to shuffle";

        public event Action UndoClicked;
        public event Action ShuffleClicked;
        public event Action AddTubeClicked;

        /// <summary>
        /// Shows each booster's count (or the buy badge at 0) and dims the ones the session would refuse.
        /// </summary>
        public void Refresh(LevelSession session, BoosterInventory boosters)
        {
            Show(_undo, boosters.GetCount(BoosterType.Undo), session.UndoBlockReason != UndoBlockReason.None);
            Show(_shuffle, boosters.GetCount(BoosterType.Shuffle), session.ShuffleBlockReason != ShuffleBlockReason.None);
            Show(_addTube, boosters.GetCount(BoosterType.AddTube), session.AddTubeBlockReason != AddTubeBlockReason.None);
        }

        /// <summary>The booster's name as the player reads it, e.g. in "Buy Add tube".</summary>
        public string GetDisplayName(BoosterType type) =>
            type switch
            {
                BoosterType.Undo => _undoName,
                BoosterType.Shuffle => _shuffleName,
                BoosterType.AddTube => _addTubeName,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown booster type."),
            };

        /// <summary>The booster's picture in the Buy popup; null when it has none.</summary>
        public Sprite GetPicture(BoosterType type) =>
            type switch
            {
                BoosterType.Undo => _undoPicture,
                BoosterType.Shuffle => _shufflePicture,
                BoosterType.AddTube => _addTubePicture,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown booster type."),
            };

        // A booster at 0 can always be bought, so it is never dimmed.
        private static void Show(BoosterButton button, int count, bool refused) => button.Show(count, count > 0 && refused);

        /// <summary>Whether the buttons react at all (false while the win sequence plays).</summary>
        public void SetInteractable(bool interactable) => _group.interactable = interactable;

        /// <summary>Whether the bar catches taps at all; when false they reach whatever is beneath it.</summary>
        public void SetCatchesTaps(bool catchesTaps) => _group.blocksRaycasts = catchesTaps;

        /// <summary>The toast for a refused undo, or null when no toast is due.</summary>
        public string Describe(UndoBlockReason reason) =>
            reason switch
            {
                UndoBlockReason.NoMovesToUndo => _nothingToUndo,
                UndoBlockReason.SealedByTubeCompletion => _undoSealed,
                _ => null,   // Stuck: the bar is hidden then. No boosters: the flow opens Buy instead.
            };

        /// <summary>The toast for a refused Add tube, or null when no toast is due.</summary>
        public string Describe(AddTubeBlockReason reason) =>
            reason switch
            {
                AddTubeBlockReason.NoRoom => _noRoomForTubes,
                _ => null,
            };

        /// <summary>The toast for a refused shuffle, or null when no toast is due.</summary>
        public string Describe(ShuffleBlockReason reason) =>
            reason switch
            {
                ShuffleBlockReason.NoEligibleTube => _nothingToShuffle,
                _ => null,   // Won, Stuck: the bar takes no taps. No boosters: the flow opens Buy instead.
            };

        private void OnUndoClicked() => UndoClicked?.Invoke();

        private void OnShuffleClicked() => ShuffleClicked?.Invoke();

        private void OnAddTubeClicked() => AddTubeClicked?.Invoke();

        private void Awake()
        {
            if (_undo == null || _shuffle == null || _addTube == null || _group == null)
            {
                Debug.LogError($"{name}: every booster button and the canvas group must be assigned.", this);
                return;
            }

            _undo.Clicked += OnUndoClicked;
            _shuffle.Clicked += OnShuffleClicked;
            _addTube.Clicked += OnAddTubeClicked;
        }

        private void OnDestroy()
        {
            if (_undo != null)
            {
                _undo.Clicked -= OnUndoClicked;
            }

            if (_shuffle != null)
            {
                _shuffle.Clicked -= OnShuffleClicked;
            }

            if (_addTube != null)
            {
                _addTube.Clicked -= OnAddTubeClicked;
            }
        }

        private void OnValidate()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }
        }
    }
}
