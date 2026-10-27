using System;
using BallSort.Core.Player;
using BallSort.Core.Session;
using BallSort.Game.Input;
using BallSort.Game.Services;
using BallSort.Game.UI;
using BallSort.Game.Views;
using UnityEngine;

namespace BallSort.Game.Flow
{
    /// <summary>
    /// Runs one visit to the Level scene: wires the session to the board, input, HUD and feedback,
    /// and moves through <see cref="LevelState"/>: Loading → Playing ⇄ Stuck → Won.
    /// </summary>
    /// <remarks>
    /// The session owns the rules; this class only reacts to its events and to the HUD's buttons.
    /// On a win, progress and reward are saved first, then the celebration plays and the Win panel
    /// opens — so leaving the app mid-celebration loses nothing.
    /// </remarks>
    public sealed class LevelFlow : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private TubeInput _input;
        [SerializeField] private LevelHud _hud;
        [SerializeField] private LevelFeedback _feedback;

        private LevelSession _session;
        private PlayerData _player;
        private EconomyConfig _economy;
        private SceneLoader _sceneLoader;
        private int _levelNumber;

        public LevelState State { get; private set; } = LevelState.Loading;

        /// <summary>The session being played, for editor tools and debugging; null until <see cref="Begin"/>.</summary>
        public LevelSession Session => _session;

        /// <summary>Starts play on <paramref name="session"/>. Call once, from the scene's bootstrap.</summary>
        public void Begin(
            LevelSession session, int levelNumber, PlayerData player, EconomyConfig economy, SceneLoader sceneLoader)
        {
            if (_session != null)
            {
                throw new InvalidOperationException("The level flow has already begun.");
            }

            _session = session ?? throw new ArgumentNullException(nameof(session));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _economy = economy != null ? economy : throw new ArgumentNullException(nameof(economy));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _levelNumber = levelNumber;

            _boardView.Initialize(session);
            _feedback.Initialize(session);
            _input.Initialize(session, () => _hud.IsPopupOpen);
            _hud.Initialize(levelNumber, player);

            _input.SelectionChanged += _boardView.ShowSelection;
            _input.InvalidMove += _feedback.PlayInvalidMove;

            _session.Changed += RefreshHud;
            _session.StuckChanged += OnStuckChanged;
            _session.LevelCompleted += OnLevelCompleted;
            _player.Boosters.Changed += OnBoostersChanged;
            _player.Wallet.Changed += OnCoinsChanged;

            _hud.Boosters.UndoClicked += OnUndoClicked;
            _hud.Boosters.ShuffleClicked += OnShuffleClicked;
            _hud.Boosters.AddTubeClicked += OnAddTubeClicked;
            _hud.Stuck.GiveUpClicked += LeaveToHome;
            _hud.Stuck.PlayOnClicked += OnPlayOnClicked;
            _hud.Win.ContinueClicked += LeaveToHome;
            _hud.RestartRequested += OnRestartRequested;
            _hud.HomeRequested += LeaveToHome;

            // The validator rejects levels with no move at start, but a stuck start stays playable.
            Enter(_session.IsStuck ? LevelState.Stuck : LevelState.Playing);
        }

        private void Enter(LevelState state)
        {
            State = state;
            bool canPlay = state == LevelState.Playing || state == LevelState.Stuck;

            _input.IsAcceptingTaps = canPlay;
            if (!canPlay)
            {
                _input.ClearSelection();
            }

            _hud.SetLocked(!canPlay);
            _hud.ShowStuck(state == LevelState.Stuck);
            RefreshHud();
        }

        private void RefreshHud()
        {
            _hud.Boosters.Refresh(_session, _player.Boosters);
            if (State == LevelState.Stuck)
            {
                _hud.Stuck.ShowPlayOn(
                    _economy.PlayOnCost,
                    _session.GetPlayOnBlockReason(_player.Wallet, _economy.PlayOnCost) == PlayOnBlockReason.None);
            }
        }

        // --- Session events ------------------------------------------------------------------------

        private void OnBoostersChanged(BoosterType _, int __) => RefreshHud();

        private void OnCoinsChanged(int _) => RefreshHud();

        private void OnStuckChanged(bool stuck)
        {
            if (State != LevelState.Won)
            {
                Enter(stuck ? LevelState.Stuck : LevelState.Playing);
            }
        }

        private void OnLevelCompleted()
        {
            if (State == LevelState.Won)
            {
                return;
            }

            Enter(LevelState.Won);
            _player.RecordWin(_levelNumber, _economy.WinReward);
            PlayWinSequence();
        }

        // Fire-and-forget from an event handler; cancelled if the scene unloads mid-celebration.
        private async void PlayWinSequence()
        {
            try
            {
                await _feedback.PlayCelebrationAsync(destroyCancellationToken);
                _hud.Win.Show(_levelNumber, _economy.WinReward);
            }
            catch (OperationCanceledException)
            {
            }
        }

        // --- HUD buttons ---------------------------------------------------------------------------

        private void OnUndoClicked()
        {
            if (!_session.TryUndo(out var reason))
            {
                ShowRefusal(_hud.Boosters.Describe(reason));
            }
        }

        private void OnShuffleClicked()
        {
            if (!_session.TryShuffle(out var reason))
            {
                ShowRefusal(_hud.Boosters.Describe(reason));
            }
        }

        private void OnAddTubeClicked()
        {
            if (!_session.TryAddTube(out var reason))
            {
                ShowRefusal(_hud.Boosters.Describe(reason));
            }
        }

        private void OnPlayOnClicked()
        {
            // The button is disabled when unavailable; this guards a tap racing a balance change.
            if (!_session.TryPlayOn(_player.Wallet, _economy.PlayOnCost, out _))
            {
                RefreshHud();
            }
        }

        private void OnRestartRequested()
        {
            if (State == LevelState.Playing || State == LevelState.Stuck)
            {
                _session.Restart();
            }
        }

        /// <summary>Give up, settings → Home, and Continue after a win: all leave without further progress.</summary>
        private void LeaveToHome()
        {
            _input.IsAcceptingTaps = false;
            _hud.SetLocked(true);
            _sceneLoader.LoadHome();
        }

        private void ShowRefusal(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _hud.Toast.Show(message);
            }
        }

        private void OnDestroy()
        {
            if (_session == null)
            {
                return;
            }

            // The player data outlives this scene; the rest goes with it, but unsubscribe anyway.
            _player.Boosters.Changed -= OnBoostersChanged;
            _player.Wallet.Changed -= OnCoinsChanged;

            _session.Changed -= RefreshHud;
            _session.StuckChanged -= OnStuckChanged;
            _session.LevelCompleted -= OnLevelCompleted;

            if (_input != null)
            {
                _input.SelectionChanged -= _boardView.ShowSelection;
                _input.InvalidMove -= _feedback.PlayInvalidMove;
            }

            if (_hud != null)
            {
                _hud.Boosters.UndoClicked -= OnUndoClicked;
                _hud.Boosters.ShuffleClicked -= OnShuffleClicked;
                _hud.Boosters.AddTubeClicked -= OnAddTubeClicked;
                _hud.Stuck.GiveUpClicked -= LeaveToHome;
                _hud.Stuck.PlayOnClicked -= OnPlayOnClicked;
                _hud.Win.ContinueClicked -= LeaveToHome;
                _hud.RestartRequested -= OnRestartRequested;
                _hud.HomeRequested -= LeaveToHome;
            }
        }

        private void Awake()
        {
            if (_boardView == null || _input == null || _hud == null || _feedback == null)
            {
                Debug.LogError($"{name}: board view, input, HUD and feedback must be assigned.", this);
                enabled = false;
            }
        }
    }
}
