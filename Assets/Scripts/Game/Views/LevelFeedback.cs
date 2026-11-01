using System.Threading;
using BallSort.Core.Model;
using BallSort.Core.Session;
using BallSort.Game.Audio;
using BallSort.Game.Services;
using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// The plug-in point for gameplay feedback: selection, invalid taps, landings, completed tubes,
    /// boosters and the win celebration. Effects, sounds and haptics play here without touching the
    /// input or the level flow.
    /// </summary>
    /// <remarks>
    /// The board view's presentation events are followed here (a ball lifted or dropped back, landed,
    /// a completion started, a cork seated), so the feedback plays when the player sees it, not when the
    /// model changed (docs/animation-spec.md, Sounds). The boosters' sounds follow the session: they
    /// play when the booster is applied. The level flow routes input's invalid taps to <see cref="PlayInvalidMove"/> and awaits
    /// <see cref="PlayCelebrationAsync"/>.
    /// </remarks>
    public sealed class LevelFeedback : MonoBehaviour
    {
        [SerializeField] private AnimationConfig _animation;
        [SerializeField] private BoardClock _clock;
        [Tooltip("Placeholder celebration (\"Well done!\"); shown for the celebration duration.")]
        [SerializeField] private GameObject _celebration;

        [Header("Effects (counts, sizes and sprites on the prefabs)")]
        [Tooltip("Sparkles bursting up from a completed tube's mouth.")]
        [SerializeField] private VfxBurst _sparkles;
        [Tooltip("Ring expanding from a completed tube's centre.")]
        [SerializeField] private VfxBurst _ring;
        [Tooltip("Confetti from a completed tube's mouth.")]
        [SerializeField] private VfxBurst _confetti;
        [Tooltip("Droplets where a ball lands.")]
        [SerializeField] private VfxBurst _droplets;
        [Tooltip("Ice shards and a puff where a completed clay tube's ice bursts (colours on the prefab).")]
        [SerializeField] private VfxBurst _iceBreak;
        [Tooltip("Colour of the ring when the ice bursts (the ring is the tube-complete ring).")]
        [SerializeField] private Color _iceRingColour = new Color(0.75f, 0.92f, 1f);
        [Tooltip("Lilac smoke where a hidden ball turns face up (colour on the prefab).")]
        [SerializeField] private VfxBurst _revealPuff;
        [Tooltip("Pink smoke over a tube whose veil lifts (colour on the prefab).")]
        [SerializeField] private VfxBurst _towelPuff;
        [Tooltip("Gold sparks at the mouth of a tube whose veil lifts (colour on the prefab).")]
        [SerializeField] private VfxBurst _towelSparks;

        private LevelSession _session;
        private BoardView _board;
        private AudioService _audio;
        private HapticsService _haptics;
        private VfxPool _sparklesPool;
        private VfxPool _ringPool;
        private VfxPool _confettiPool;
        private VfxPool _dropletsPool;
        private VfxPool _iceBreakPool;
        private VfxPool _revealPuffPool;
        private VfxPool _towelPuffPool;
        private VfxPool _towelSparksPool;
        private Tween _punch;
        private TweenCallback<float> _punchStep;

        /// <param name="session">The session whose restarts and shuffles reset the board's feel.</param>
        /// <param name="board">The board view the feedback plays on.</param>
        /// <param name="audio">Plays the sound effects.</param>
        /// <param name="haptics">Plays the haptics paired with them.</param>
        public void Initialize(LevelSession session, BoardView board, AudioService audio, HapticsService haptics)
        {
            Unsubscribe();
            _session = session;
            _board = board;
            _audio = audio;
            _haptics = haptics;
            _session.Restarted += ResetFeel;
            _session.MoveUndone += OnUndone;
            _session.Shuffled += OnShuffled;
            _session.TubeAdded += OnTubeAdded;
            _board.BallLifted += OnBallLifted;
            _board.BallLowered += OnBallLowered;
            _board.BallLanded += OnBallLanded;
            _board.CompletionStarted += PlayTubeCompleted;
            _board.CorkSeated += OnCorkSeated;
            _board.IceBroken += OnIceBroken;
            _board.LockBeatPlayed += OnLockBeat;
            _board.VeilLifting += OnVeilLifting;
            _board.BallRevealing += OnBallRevealing;
            _celebration.SetActive(false);

            Transform effects = board.transform;
            _sparklesPool = new VfxPool(_sparkles, effects, 2);
            _ringPool = new VfxPool(_ring, effects, 2);
            _confettiPool = new VfxPool(_confetti, effects, 2);
            _dropletsPool = new VfxPool(_droplets, effects, 4);
            _iceBreakPool = new VfxPool(_iceBreak, effects, 1);
            _revealPuffPool = new VfxPool(_revealPuff, effects, 1);
            _towelPuffPool = new VfxPool(_towelPuff, effects, 1);
            _towelSparksPool = new VfxPool(_towelSparks, effects, 1);
        }

        /// <summary>
        /// A tap could not be acted on, including taps on covered and clay tubes: the tube shakes, its
        /// balls look shocked and the error sound plays (a lifted ball's drop-back plays its own).
        /// </summary>
        public void PlayInvalidMove(int tubeIndex)
        {
            _board.ShowInvalid(tubeIndex);
            _audio.Play(Sfx.MoveInvalid);
            _haptics.Warning();
        }

        /// <summary>
        /// A completed tube's last ball has landed: the chime (higher with the chain), sparkles (more with
        /// the chain), ring and confetti. The cork, glow and dance play on the tube itself; the haptic comes
        /// with the cork.
        /// </summary>
        public void PlayTubeCompleted(TubeView tube, int colorId, int chain)
        {
            Color colour = _board.Palette.GetColor(colorId);
            int escalation = _animation.ChainEscalate ? chain : 0;
            _audio.Play(Sfx.TubeCompleteChime, _audio.Config.ChainPitch(escalation));
            Vector3 mouth = tube.BallSpace.TransformPoint(new Vector3(0f, tube.BodyHeight, 0f));
            if (_animation.Sparkles)
            {
                _sparklesPool.Play(mouth, colour, escalation);
                _ringPool.Play(tube.BallSpace.TransformPoint(new Vector3(0f, tube.BodyHeight * 0.5f, 0f)), colour, 0);
            }

            if (_animation.Confetti)
            {
                _confettiPool.Play(mouth, colour, 0);
            }
        }

        /// <summary>
        /// Completes when the stuck bar may appear (docs/animation-spec.md, 5.1): the board has settled
        /// (no ball flying, no beat waiting) and stayed settled for <see cref="AnimationConfig.StuckDelay"/>,
        /// so the last move reads before the message. A move made meanwhile restarts the wait.
        /// </summary>
        public async Awaitable WaitForStuckRevealAsync(CancellationToken cancellationToken)
        {
            float delay = _animation.StuckDelay / _animation.Speed;
            float settledSince = -1f;
            while (true)
            {
                if (!_board.IsSettled)
                {
                    settledSince = -1f;
                }
                else if (settledSince < 0f)
                {
                    settledSince = Time.time;
                }

                if (settledSince >= 0f && Time.time - settledSince >= delay)
                {
                    return;
                }

                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        /// <summary>The stuck bar appears: the "no" sound and its haptic (SFX guide §3.4).</summary>
        public void PlayStuck()
        {
            _audio.Play(Sfx.MoveInvalid);
            _haptics.Warning();
        }

        /// <summary>
        /// Plays the win celebration and completes when it is over. Placeholder: once the board has
        /// settled (the last ball landed, the last cork seated), shows a label for
        /// <see cref="AnimationConfig.CelebrationDuration"/> seconds. The win is already saved.
        /// </summary>
        public async Awaitable PlayCelebrationAsync(CancellationToken cancellationToken)
        {
            while (!_board.IsSettled)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            _celebration.SetActive(true);
            try
            {
                await Awaitable.WaitForSecondsAsync(_animation.CelebrationDuration, cancellationToken);
            }
            finally
            {
                if (_celebration != null)
                {
                    _celebration.SetActive(false);
                }
            }
        }

        private void OnBallLifted()
        {
            _audio.Play(Sfx.BallSelect);
            _haptics.Selection();
        }

        private void OnBallLowered() => _audio.Play(Sfx.BallDeselect);

        // Undo and shuffle flights land silently (SFX guide, Boosters). A hidden ball (moved by a shuffle)
        // makes no droplets: their colour would give it away.
        private void OnBallLanded(BallView ball)
        {
            if (!ball.LandsSilently)
            {
                _audio.Play(Sfx.BallLand, _audio.Config.LandPitch(ball.Slot));
                _haptics.Landing();
            }

            if (_animation.Droplets && !ball.IsHidden)
            {
                _dropletsPool.Play(ball.transform.position, _board.Palette.GetColor(ball.ColorId), 0);
            }
        }

        /// <summary>The cork seats: its sound and haptic, and the board freezes for a beat and kicks (hit-stop and punch).</summary>
        private void OnCorkSeated(TubeView tube)
        {
            _audio.Play(Sfx.TubeCork);
            _haptics.Success();
            _clock.HitStop();
            BoardTweens.KillIfActive(_punch);
            float duration = _animation.PunchDuration;
            _punch = DOVirtual.Float(0f, duration, duration, _punchStep)
                .SetEase(Ease.Linear).OnComplete(EndPunch).OnBoardClock();
        }

        /// <summary>
        /// A completed clay tube's ice bursts (docs/animation-spec.md, 3.4): its sound and haptic, shards
        /// and a puff, and a ring in the ice's colour. With the default timing it lands with the cork.
        /// </summary>
        private void OnIceBroken(TubeView tube, Vector3 centre)
        {
            _audio.Play(Sfx.IceBreak);
            _haptics.Heavy();
            _iceBreakPool.Play(centre, Color.white, 0);
            if (_animation.Sparkles)
            {
                _ringPool.Play(centre, _iceRingColour, 0);
            }
        }

        /// <summary>
        /// A hidden ball turns face up (docs/animation-spec.md, 3.1): a lilac puff, and after a move (not
        /// a shuffle, SFX guide 4.1) its sound and a Light haptic.
        /// </summary>
        private void OnBallRevealing(BallView ball, bool withSound)
        {
            _revealPuffPool.Play(ball.transform.position, Color.white, 0);
            if (withSound)
            {
                _audio.Play(Sfx.HiddenReveal);
                _haptics.Light();
            }
        }

        /// <summary>
        /// A veil lifts (docs/animation-spec.md, 3.2): pink smoke over the tube and gold sparks at its
        /// mouth; the first veil of a completion plays the towel's sound and a Heavy haptic (SFX guide 4.2).
        /// </summary>
        private void OnVeilLifting(CoverView veil, bool withSound)
        {
            _towelPuffPool.Play(veil.VeilCentre, Color.white, 0);
            _towelSparksPool.Play(veil.VeilMouth, Color.white, 0);
            if (withSound)
            {
                _audio.Play(Sfx.TowelPull);
                _haptics.Heavy();
            }
        }

        /// <summary>
        /// A lock box's opening (docs/animation-spec.md, 3.3; SFX guide 4.3): one sound per box at each
        /// step, the key's flight, its seating (with the box's wobble) and the covers sliding open.
        /// </summary>
        private void OnLockBeat(CoverView box, LockBeat beat)
        {
            switch (beat)
            {
                case LockBeat.KeyFlying:
                    _audio.Play(Sfx.LockboxKeyFly);
                    break;
                case LockBeat.KeySeated:
                    _audio.Play(Sfx.LockboxKeyIn);
                    _haptics.Medium();
                    break;
                case LockBeat.Opening:
                    _audio.Play(Sfx.LockboxBreak);
                    _haptics.Heavy();
                    break;
            }
        }

        private void PunchStep(float t) =>
            _board.Layout.SetPunch(1f + _animation.PunchScale * BoardTweens.HalfSine(t, _animation.PunchDuration));

        private void EndPunch() => _board.Layout.SetPunch(1f);

        // The boosters' own sounds, when the booster is applied (SFX guide §5); the button's press plays
        // its click separately. Only the Undo booster undoes; Play on adds a tube as Add tube does.
        private void OnUndone(MoveEventArgs _) => PlayBooster(Sfx.BoosterUndo);

        private void OnShuffled(TubeShuffledEventArgs _) => PlayBooster(Sfx.BoosterShuffle);

        private void OnTubeAdded(TubeAddedEventArgs _) => PlayBooster(Sfx.BoosterAddTube);

        private void PlayBooster(Sfx sfx)
        {
            _audio.Play(sfx);
            _haptics.Light();
        }

        // A rebuilt board starts at rest.
        private void ResetFeel()
        {
            BoardTweens.KillIfActive(_punch);
            EndPunch();
            _clock.CancelHitStop();
        }

        private void Unsubscribe()
        {
            if (_session != null)
            {
                _session.Restarted -= ResetFeel;
                _session.MoveUndone -= OnUndone;
                _session.Shuffled -= OnShuffled;
                _session.TubeAdded -= OnTubeAdded;
                _session = null;
            }

            if (_board != null)
            {
                _board.BallLifted -= OnBallLifted;
                _board.BallLowered -= OnBallLowered;
                _board.BallLanded -= OnBallLanded;
                _board.CompletionStarted -= PlayTubeCompleted;
                _board.CorkSeated -= OnCorkSeated;
                _board.IceBroken -= OnIceBroken;
                _board.LockBeatPlayed -= OnLockBeat;
                _board.VeilLifting -= OnVeilLifting;
                _board.BallRevealing -= OnBallRevealing;
            }
        }

        private void Awake()
        {
            if (_animation == null || _clock == null || _celebration == null ||
                _sparkles == null || _ring == null || _confetti == null || _droplets == null || _iceBreak == null ||
                _revealPuff == null || _towelPuff == null || _towelSparks == null)
            {
                Debug.LogError($"{name}: animation config, board clock, celebration and the eight effects must be assigned.", this);
            }

            _punchStep = PunchStep;
        }

        private void OnDestroy()
        {
            BoardTweens.KillIfActive(_punch);
            Unsubscribe();
        }
    }
}
