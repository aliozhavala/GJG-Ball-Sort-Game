using System;
using System.Threading;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using BallSort.Core.Session;
using BallSort.Game.Audio;
using BallSort.Game.Input;
using BallSort.Game.Services;
using BallSort.Game.UI;
using BallSort.Game.Views;
using UnityEngine;

namespace BallSort.Game.Flow
{
    /// <summary>
    /// Runs one visit to the Level scene: wires the session to the board, input, HUD and feedback,
    /// and moves through <see cref="LevelState"/>: Loading → Playing ⇄ Stuck → Won, with a
    /// Playing → ShufflePicking → Playing detour while the Shuffle booster waits for a tube.
    /// </summary>
    /// <remarks>
    /// The session owns the rules; this class only reacts to its events and to the HUD's buttons.
    /// On a win, progress and reward are saved first, then the celebration plays and the Win panel
    /// opens — so leaving the app mid-celebration loses nothing.
    /// <para>
    /// Entering the level begins an attempt, which picks the variant to play; Restart, settings → Home
    /// and Give up end it, each spending a life and counting a retry, so the next attempt plays the next
    /// variant (docs/levels.md, Variants and retries). Each asks Are You Sure first; at 0 lives Restart
    /// opens More Lives instead. Play on continues the attempt; a win spends nothing. A tutorial level
    /// shows its tutorial popup the first time it is entered (docs/screens.md, Tutorials).
    /// </para>
    /// <para>
    /// Boosters work only while Playing (the bar is hidden while stuck and locked otherwise). A booster
    /// at 0 opens the Buy popup instead of being used (docs/screens.md, Buying).
    /// </para>
    /// <para>
    /// Stuck is entered at once (the boosters lock), but the stuck bar replaces the booster bar only
    /// once the board has settled, so the last move is seen first (docs/animation-spec.md, 5.1).
    /// </para>
    /// <para>
    /// An editor test visit plays its chosen variant on every attempt and saves nothing: no life, no
    /// retry, no reward, no progress, no tutorial seen. Its popups still show, so they can be tested.
    /// </para>
    /// </remarks>
    public sealed class LevelFlow : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private TubeInput _input;
        [SerializeField] private LevelHud _hud;
        [SerializeField] private LevelFeedback _feedback;

        // Picks the order of each shuffled tube; main thread only.
        private readonly System.Random _shuffleRandom = new System.Random();

        // Picks the variant of each try from the sixth on; main thread only.
        private readonly System.Random _variantRandom = new System.Random();

        private LevelSession _session;
        private PlayerData _player;
        private EconomyConfig _economy;
        private EpisodeConfig _episodes;
        private TutorialConfig _tutorials;
        private SceneLoader _sceneLoader;
        private AudioService _audio;
        private HapticsService _haptics;
        private LevelVisit _visit;

        // The exit Are You Sure is asking about, and whether confirming it spends a life.
        private Exit _pendingExit;
        private bool _pendingExitCostsALife;

        // What Home shows after Continue: the episode the win moved into, if any.
        private HomeArrival _arrivalAfterWin = HomeArrival.None;

        // Set once the player leaves for Home, so a second tap cannot spend a second life.
        private bool _isLeaving;

        // Waits for the board to settle before the stuck bar shows; cancelled when leaving Stuck.
        private CancellationTokenSource _stuckReveal;

        /// <summary>The ways to leave an attempt unwon, each confirmed with Are You Sure first.</summary>
        private enum Exit
        {
            Home,
            Restart,
            GiveUp,
        }

        public LevelState State { get; private set; } = LevelState.Loading;

        /// <summary>The session being played, for editor tools and debugging; null until <see cref="Begin"/>.</summary>
        public LevelSession Session => _session;

        /// <summary>
        /// Begins the first attempt at <paramref name="level"/> and plays it. Call once, from the scene's bootstrap.
        /// </summary>
        /// <param name="level">The loaded level; each attempt plays one of its variants.</param>
        /// <param name="visit">Which level this is, and whether its win is recorded.</param>
        /// <param name="episodes">Gives the level's mascot frame and tells when a win unlocks an episode.</param>
        /// <param name="tutorials">The tutorial popups, keyed by level id.</param>
        /// <param name="audio">Plays the level's sound effects.</param>
        /// <param name="haptics">Plays the haptics paired with them.</param>
        public void Begin(
            LoadedLevel level, LevelVisit visit, PlayerData player, EconomyConfig economy, EpisodeConfig episodes,
            TutorialConfig tutorials, SceneLoader sceneLoader, AudioService audio, HapticsService haptics)
        {
            if (_session != null)
            {
                throw new InvalidOperationException("The level flow has already begun.");
            }

            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            _economy = economy != null ? economy : throw new ArgumentNullException(nameof(economy));
            _episodes = episodes != null ? episodes : throw new ArgumentNullException(nameof(episodes));
            _tutorials = tutorials != null ? tutorials : throw new ArgumentNullException(nameof(tutorials));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _audio = audio != null ? audio : throw new ArgumentNullException(nameof(audio));
            _haptics = haptics != null ? haptics : throw new ArgumentNullException(nameof(haptics));
            _visit = visit;

            int variant = _visit.IsEditorTest ? _visit.Variant : player.BeginAttempt(_variantRandom);
            LevelSession session = new LevelSession(level, variant, player.Boosters, economy.AddedTubeCapacity);
            _session = session;

            _boardView.Initialize(session);
            _feedback.Initialize(session, _boardView, audio, haptics);
            _input.Initialize(session, () => _hud.IsPopupOpen);
            _hud.Initialize(visit.Label, player, episodes.GetEpisodeForLevel(visit.LevelNumber).MascotFrame, audio, haptics);

            _input.SelectionChanged += _boardView.ShowSelection;
            _input.InvalidMove += _feedback.PlayInvalidMove;
            _input.ShuffleTubePicked += OnShuffleTubePicked;

            _session.Changed += RefreshHud;
            _session.StuckChanged += OnStuckChanged;
            _session.LevelCompleted += OnLevelCompleted;
            _player.Boosters.Changed += OnBoostersChanged;
            _player.Wallet.Changed += OnCoinsChanged;
            _hud.Buy.ShowCoins(_player.Wallet);

            _hud.Boosters.UndoClicked += OnUndoClicked;
            _hud.Boosters.ShuffleClicked += OnShuffleClicked;
            _hud.Boosters.AddTubeClicked += OnAddTubeClicked;
            _hud.Stuck.GiveUpClicked += OnGiveUpClicked;
            _hud.Stuck.PlayOnClicked += OnPlayOnClicked;
            _hud.Win.ContinueClicked += OnContinueClicked;
            _hud.RestartRequested += OnRestartRequested;
            _hud.HomeRequested += OnHomeRequested;
            _hud.AreYouSure.Confirmed += OnExitConfirmed;
            _hud.Tutorial.Dismissed += OnTutorialDismissed;

            // The validator rejects levels with no move at start, but a stuck start stays playable.
            Enter(_session.IsStuck ? LevelState.Stuck : LevelState.Playing);
            ShowTutorialIfDue();
        }

        private void Enter(LevelState state)
        {
            bool wasStuck = State == LevelState.Stuck;
            State = state;
            bool canPlay = state == LevelState.Playing || state == LevelState.Stuck;
            bool picking = state == LevelState.ShufflePicking;

            _input.IsAcceptingTaps = canPlay || picking;
            _input.IsPickingShuffleTube = picking;
            if (!canPlay)
            {
                _input.ClearSelection();
            }

            _hud.SetLocked(settingsLocked: !canPlay, boostersLocked: state != LevelState.Playing);
            _hud.SetTapsPassThrough(picking);
            if (state == LevelState.Stuck)
            {
                if (!wasStuck)
                {
                    RevealStuckWhenSettled();
                }
            }
            else
            {
                CancelStuckReveal();
                _hud.ShowStuck(false);
            }

            _boardView.ShowShufflePick(picking);
            RefreshHud();
        }

        // Fire-and-forget; cancelled when the level leaves Stuck or the scene unloads.
        private async void RevealStuckWhenSettled()
        {
            CancelStuckReveal();
            _stuckReveal = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            CancellationToken token = _stuckReveal.Token;
            try
            {
                await _feedback.WaitForStuckRevealAsync(token);
                _hud.ShowStuck(true);
                _feedback.PlayStuck();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelStuckReveal()
        {
            if (_stuckReveal == null)
            {
                return;
            }

            _stuckReveal.Cancel();
            _stuckReveal.Dispose();
            _stuckReveal = null;
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
            if (!_visit.IsEditorTest)
            {
                _player.RecordWin(_visit.LevelNumber, _economy.GetWinReward(_session.Level.Tier));
                int unlocked = _episodes.GetUnlockedEpisode(_visit.LevelNumber, _player.CurrentLevel);
                if (unlocked >= 0)
                {
                    _arrivalAfterWin = HomeArrival.WithUnlockedEpisode(unlocked);
                }
            }

            PlayWinSequence();
        }

        // Fire-and-forget from an event handler; cancelled if the scene unloads mid-celebration.
        private async void PlayWinSequence()
        {
            try
            {
                await _feedback.PlayCelebrationAsync(destroyCancellationToken);
                _hud.Win.Show(_visit.Label, _visit.IsEditorTest ? 0 : _economy.GetWinReward(_session.Level.Tier));
            }
            catch (OperationCanceledException)
            {
            }
        }

        // --- HUD buttons ---------------------------------------------------------------------------

        private void OnUndoClicked()
        {
            if (State != LevelState.Playing || OpenBuyIfNone(BoosterType.Undo))
            {
                return;
            }

            if (!_session.TryUndo(out var reason))
            {
                ShowRefusal(_hud.Boosters.Describe(reason));
            }
        }

        /// <summary>Starts the pick: the next tap on an eligible tube shuffles it, anything else cancels.</summary>
        private void OnShuffleClicked()
        {
            if (State != LevelState.Playing || OpenBuyIfNone(BoosterType.Shuffle))
            {
                return;
            }

            ShuffleBlockReason reason = _session.ShuffleBlockReason;
            if (reason != ShuffleBlockReason.None)
            {
                RefuseShuffle(reason);
                return;
            }

            Enter(LevelState.ShufflePicking);
        }

        private void OnShuffleTubePicked(int tubeIndex)
        {
            if (State != LevelState.ShufflePicking)
            {
                return;
            }

            // Back to Playing first: the shuffle's own events may move on to Stuck or Won.
            Enter(LevelState.Playing);
            if (tubeIndex == TubeInput.NoTube || !_session.CanShuffleTube(tubeIndex))
            {
                return;   // Cancelled: nothing spent.
            }

            if (!_session.TryShuffleTube(tubeIndex, _shuffleRandom, out ShuffleBlockReason reason))
            {
                RefuseShuffle(reason);
            }
        }

        /// <summary>A refused shuffle: the invalid sound and the toast saying why (SFX guide §5).</summary>
        private void RefuseShuffle(ShuffleBlockReason reason)
        {
            string message = _hud.Boosters.Describe(reason);
            if (!string.IsNullOrEmpty(message))
            {
                _audio.Play(Sfx.MoveInvalid);
                _hud.Toast.Show(message);
            }
        }

        private void OnAddTubeClicked()
        {
            if (State != LevelState.Playing || OpenBuyIfNone(BoosterType.AddTube))
            {
                return;
            }

            if (!_session.TryAddTube(out var reason))
            {
                ShowRefusal(_hud.Boosters.Describe(reason));
            }
        }

        /// <summary>A booster at 0 opens the Buy popup instead of being used.</summary>
        /// <returns>True when the player holds none (the popup is open).</returns>
        private bool OpenBuyIfNone(BoosterType type)
        {
            if (_player.Boosters.GetCount(type) > 0)
            {
                return false;
            }

            _hud.Buy.Show(
                _hud.Buy.BuyTitle(_hud.Boosters.GetDisplayName(type)), _hud.Boosters.GetPicture(type),
                _economy.GetBoosterPrice(type), () => TryBuyBooster(type));
            return true;
        }

        /// <returns>True when bought, which closes the popup; without enough coins it stays open.</returns>
        /// <remarks>Buy's press plays no button sound: the result's sound replaces it (SFX guide §7).</remarks>
        private bool TryBuyBooster(BoosterType type)
        {
            if (_player.TryBuyBooster(type, _economy.GetBoosterPrice(type), _economy.BoostersPerPurchase, out _))
            {
                // The badge already shows the new count: the purchase refreshed the bar.
                _audio.Play(Sfx.RewardJingle);
                _haptics.Success();
                _hud.Boosters.PlayPurchased(type);
                return true;
            }

            _audio.Play(Sfx.MoveInvalid);
            _hud.Toast.Show(_hud.Buy.NotEnoughCoinsText);
            return false;
        }

        private void OnPlayOnClicked()
        {
            // The button is disabled when unavailable; this guards a tap racing a balance change.
            if (!_session.TryPlayOn(_player.Wallet, _economy.PlayOnCost, out _))
            {
                RefreshHud();
            }
        }

        // --- Leaving the attempt: Are You Sure, More Lives -------------------------------------------

        private void OnHomeRequested() => AskToLeave(Exit.Home);

        private void OnGiveUpClicked() => AskToLeave(Exit.GiveUp);

        private void OnRestartRequested()
        {
            // At 0 lives Restart cannot start a new attempt: More Lives instead, over the settings popup.
            if (!_visit.IsEditorTest && !_player.CanStartLevel())
            {
                _hud.MoreLives.Open();
                return;
            }

            AskToLeave(Exit.Restart);
        }

        private void AskToLeave(Exit exit)
        {
            if (State != LevelState.Playing && State != LevelState.Stuck)
            {
                return;
            }

            // A test visit spends nothing, so it leaves out the life line, as 0 lives does.
            _pendingExit = exit;
            _player.RefreshLives();
            _pendingExitCostsALife = !_visit.IsEditorTest && _player.LifeCount > 0;
            _hud.AreYouSure.Ask(_pendingExitCostsALife, restarts: exit == Exit.Restart);
        }

        private void OnExitConfirmed()
        {
            if (State != LevelState.Playing && State != LevelState.Stuck)
            {
                return;
            }

            // Confirm plays no button sound: spending a life has its own (SFX guide §6.4).
            if (_pendingExitCostsALife)
            {
                _audio.Play(Sfx.LifeLost);
                _haptics.Warning();
            }
            else
            {
                _audio.Play(Sfx.UiButton);
                _haptics.Light();
            }

            if (_pendingExit == Exit.Restart)
            {
                RestartAttempt();
            }
            else
            {
                LeaveBeforeWin();
            }
        }

        private void RestartAttempt()
        {
            if (_visit.IsEditorTest)
            {
                _hud.CloseSettings();
                _session.Restart(_visit.Variant);
            }
            else if (_player.TryRestartAttempt(_variantRandom, out int variant))
            {
                _hud.CloseSettings();
                _session.Restart(variant);
            }
            else
            {
                // The last life went between asking and confirming; the settings popup stays beneath.
                _hud.MoreLives.Open();
            }
        }

        /// <summary>Give up and settings → Home, confirmed: the attempt ends without a win, which costs a life.</summary>
        private void LeaveBeforeWin()
        {
            if (_isLeaving)
            {
                return;
            }

            if (!_visit.IsEditorTest)
            {
                _player.LeaveAttempt();
            }

            LeaveToHome(HomeArrival.None);
        }

        /// <summary>Continue on the Win panel: costs nothing, and Home may show the episode the win unlocked.</summary>
        private void OnContinueClicked() => LeaveToHome(_arrivalAfterWin);

        private void LeaveToHome(HomeArrival arrival)
        {
            if (_isLeaving)
            {
                return;
            }

            _isLeaving = true;
            _input.IsAcceptingTaps = false;
            _hud.SetLocked(settingsLocked: true, boostersLocked: true);
            _sceneLoader.LoadHome(arrival);
        }

        // --- Tutorial --------------------------------------------------------------------------------

        /// <summary>A tutorial level shows its popup the first time it is entered (every time on a test visit).</summary>
        private void ShowTutorialIfDue()
        {
            LoadedLevel level = _session.Level;
            if (!level.IsTutorial || (!_visit.IsEditorTest && _player.HasSeenTutorial(level.Id)))
            {
                return;
            }

            if (!_tutorials.TryGetEntry(level.Id, out TutorialEntry entry))
            {
                Debug.LogWarning($"Level {level.Id} is a tutorial level, but {_tutorials.name} has no entry for it.", _tutorials);
                return;
            }

            _hud.Tutorial.Show(entry.Text, entry.Image);
        }

        private void OnTutorialDismissed()
        {
            if (!_visit.IsEditorTest)
            {
                _player.MarkTutorialSeen(_session.Level.Id);
            }
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
            CancelStuckReveal();
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
                _input.ShuffleTubePicked -= OnShuffleTubePicked;
            }

            if (_hud != null)
            {
                _hud.Boosters.UndoClicked -= OnUndoClicked;
                _hud.Boosters.ShuffleClicked -= OnShuffleClicked;
                _hud.Boosters.AddTubeClicked -= OnAddTubeClicked;
                _hud.Stuck.GiveUpClicked -= OnGiveUpClicked;
                _hud.Stuck.PlayOnClicked -= OnPlayOnClicked;
                _hud.Win.ContinueClicked -= OnContinueClicked;
                _hud.RestartRequested -= OnRestartRequested;
                _hud.HomeRequested -= OnHomeRequested;
                _hud.AreYouSure.Confirmed -= OnExitConfirmed;
                _hud.Tutorial.Dismissed -= OnTutorialDismissed;
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
