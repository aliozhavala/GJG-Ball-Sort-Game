using System.Threading;
using BallSort.Core.Session;
using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// The plug-in point for gameplay feedback: invalid taps, landings, completed tubes and the win
    /// celebration. Animation effects, audio and haptics fill in these methods without touching the
    /// input or the level flow.
    /// </summary>
    /// <remarks>
    /// The board view's presentation events are followed here (a ball landed, a completion started, a
    /// cork seated), so the feedback plays when the player sees it, not when the model changed. The
    /// level flow routes input's invalid taps to <see cref="PlayInvalidMove"/> and awaits
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

        private LevelSession _session;
        private BoardView _board;
        private VfxPool _sparklesPool;
        private VfxPool _ringPool;
        private VfxPool _confettiPool;
        private VfxPool _dropletsPool;
        private Tween _punch;
        private TweenCallback<float> _punchStep;

        /// <param name="session">The session whose restarts and shuffles reset the board's feel.</param>
        /// <param name="board">The board view the feedback plays on.</param>
        public void Initialize(LevelSession session, BoardView board)
        {
            Unsubscribe();
            _session = session;
            _board = board;
            _session.Restarted += ResetFeel;
            _session.Shuffled += ResetFeel;
            _board.BallLanded += OnBallLanded;
            _board.CompletionStarted += PlayTubeCompleted;
            _board.CorkSeated += OnCorkSeated;
            _celebration.SetActive(false);

            Transform effects = board.transform;
            _sparklesPool = new VfxPool(_sparkles, effects, 2);
            _ringPool = new VfxPool(_ring, effects, 2);
            _confettiPool = new VfxPool(_confetti, effects, 2);
            _dropletsPool = new VfxPool(_droplets, effects, 4);
        }

        /// <summary>
        /// A tap could not be acted on, including taps on covered and clay tubes: the tube shakes and
        /// its balls look shocked. Step 6: error sound, haptic.
        /// </summary>
        public void PlayInvalidMove(int tubeIndex) => _board.ShowInvalid(tubeIndex);

        /// <summary>
        /// A completed tube's last ball has landed: sparkles (more with the chain), ring and confetti.
        /// The cork, glow and dance play on the tube itself. Step 6: chime, haptic.
        /// </summary>
        public void PlayTubeCompleted(TubeView tube, int colorId, int chain)
        {
            Color colour = _board.Palette.GetColor(colorId);
            int escalation = _animation.ChainEscalate ? chain : 0;
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

        private void OnBallLanded(BallView ball)
        {
            if (_animation.Droplets)
            {
                _dropletsPool.Play(ball.transform.position, _board.Palette.GetColor(ball.ColorId), 0);
            }
        }

        /// <summary>The cork seats: the board freezes for a beat and kicks (hit-stop and punch). Step 6: cork sound, haptic.</summary>
        private void OnCorkSeated(TubeView tube)
        {
            _clock.HitStop();
            BoardTweens.KillIfActive(_punch);
            float duration = _animation.PunchDuration;
            _punch = DOVirtual.Float(0f, duration, duration, _punchStep)
                .SetEase(Ease.Linear).OnComplete(EndPunch).OnBoardClock();
        }

        private void PunchStep(float t) =>
            _board.Layout.SetPunch(1f + _animation.PunchScale * BoardTweens.HalfSine(t, _animation.PunchDuration));

        private void EndPunch() => _board.Layout.SetPunch(1f);

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
                _session.Shuffled -= ResetFeel;
                _session = null;
            }

            if (_board != null)
            {
                _board.BallLanded -= OnBallLanded;
                _board.CompletionStarted -= PlayTubeCompleted;
                _board.CorkSeated -= OnCorkSeated;
            }
        }

        private void Awake()
        {
            if (_animation == null || _clock == null || _celebration == null ||
                _sparkles == null || _ring == null || _confetti == null || _droplets == null)
            {
                Debug.LogError($"{name}: animation config, board clock, celebration and the four effects must be assigned.", this);
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
