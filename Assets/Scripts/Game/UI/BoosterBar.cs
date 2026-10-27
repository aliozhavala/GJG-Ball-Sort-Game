using System;
using BallSort.Core.Commands;
using BallSort.Core.Player;
using BallSort.Core.Session;
using UnityEngine;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Level screen's booster bar — Undo, Shuffle, Add tube — and the player-facing text for
    /// each reason a booster is unavailable (docs/screens.md).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BoosterBar : MonoBehaviour
    {
        [SerializeField] private BoosterButton _undo;
        [SerializeField] private BoosterButton _shuffle;
        [SerializeField] private BoosterButton _addTube;
        [SerializeField] private CanvasGroup _group;

        [Header("Refusal messages")]
        [SerializeField] private string _nothingToUndo = "Nothing to undo";
        [SerializeField] private string _undoSealed = "Can't undo past a completed tube";
        [SerializeField] private string _noUndosLeft = "No undos left";
        [SerializeField] private string _noAddTubesLeft = "No add-tube boosters left";
        [SerializeField] private string _noRoomForTubes = "No room for more tubes";
        [SerializeField] private string _shuffleComingSoon = "Coming soon";
        [SerializeField] private string _noShufflesLeft = "No shuffles left";

        public event Action UndoClicked;
        public event Action ShuffleClicked;
        public event Action AddTubeClicked;

        /// <summary>Shows each booster's count and dims the ones the session would refuse.</summary>
        public void Refresh(LevelSession session, BoosterInventory boosters)
        {
            _undo.Show(boosters.GetCount(BoosterType.Undo), session.UndoBlockReason == UndoBlockReason.None);
            _shuffle.Show(boosters.GetCount(BoosterType.Shuffle), session.ShuffleBlockReason == ShuffleBlockReason.None);
            _addTube.Show(boosters.GetCount(BoosterType.AddTube), session.AddTubeBlockReason == AddTubeBlockReason.None);
        }

        /// <summary>Whether the buttons react at all (false while the win sequence plays).</summary>
        public void SetInteractable(bool interactable) => _group.interactable = interactable;

        /// <summary>The toast for a refused undo, or null when no toast is due.</summary>
        public string Describe(UndoBlockReason reason) =>
            reason switch
            {
                UndoBlockReason.NoMovesToUndo => _nothingToUndo,
                UndoBlockReason.SealedByTubeCompletion => _undoSealed,
                UndoBlockReason.NoBoostersLeft => _noUndosLeft,
                _ => null,   // Stuck: the bar is hidden then and the banner explains.
            };

        /// <summary>The toast for a refused Add tube, or null when no toast is due.</summary>
        public string Describe(AddTubeBlockReason reason) =>
            reason switch
            {
                AddTubeBlockReason.NoRoom => _noRoomForTubes,
                AddTubeBlockReason.NoBoostersLeft => _noAddTubesLeft,
                _ => null,
            };

        /// <summary>The toast for a refused shuffle, or null when no toast is due.</summary>
        public string Describe(ShuffleBlockReason reason) =>
            reason switch
            {
                ShuffleBlockReason.NotAvailable => _shuffleComingSoon,
                ShuffleBlockReason.NoBoostersLeft => _noShufflesLeft,
                _ => null,
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
