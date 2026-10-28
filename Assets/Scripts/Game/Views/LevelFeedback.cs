using System.Threading;
using BallSort.Core.Model;
using BallSort.Core.Session;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// The plug-in point for gameplay feedback: invalid taps, completed tubes and the win
    /// celebration. Animation, audio and haptics (roadmap step 6) fill in these methods without
    /// touching the input or the level flow.
    /// </summary>
    /// <remarks>
    /// Session events are followed here; the level flow routes input's invalid taps to
    /// <see cref="PlayInvalidMove"/> and awaits <see cref="PlayCelebrationAsync"/>.
    /// </remarks>
    public sealed class LevelFeedback : MonoBehaviour
    {
        [SerializeField] private AnimationConfig _animation;
        [Tooltip("Placeholder celebration (\"Well done!\"); shown for the celebration duration.")]
        [SerializeField] private GameObject _celebration;

        private LevelSession _session;
        private BoardView _board;

        /// <param name="session">The session whose events trigger feedback.</param>
        /// <param name="board">The board view the feedback plays on.</param>
        public void Initialize(LevelSession session, BoardView board)
        {
            Unsubscribe();
            _session = session;
            _board = board;
            _session.TubeCompleted += OnTubeCompleted;
            _celebration.SetActive(false);
        }

        /// <summary>
        /// A tap could not be acted on, including taps on covered and clay tubes. Placeholder: the
        /// tube flashes. Step 6: shake the tube, error sound, light haptic.
        /// </summary>
        public void PlayInvalidMove(int tubeIndex)
        {
            _board.Tubes[tubeIndex].Flash(_animation.InvalidFlashColor, _animation.InvalidFlashDuration);
        }

        /// <summary>A tube was corked. Step 6: cap drop, sparkle, chime, haptic.</summary>
        public void PlayTubeCompleted(int tubeIndex)
        {
        }

        /// <summary>
        /// Plays the win celebration and completes when it is over. Placeholder: shows a label for
        /// <see cref="AnimationConfig.CelebrationDuration"/> seconds.
        /// </summary>
        public async Awaitable PlayCelebrationAsync(CancellationToken cancellationToken)
        {
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

        private void OnTubeCompleted(TubeCompletedEventArgs e) => PlayTubeCompleted(e.TubeIndex);

        private void Unsubscribe()
        {
            if (_session != null)
            {
                _session.TubeCompleted -= OnTubeCompleted;
                _session = null;
            }
        }

        private void Awake()
        {
            if (_animation == null || _celebration == null)
            {
                Debug.LogError($"{name}: animation config and celebration must be assigned.", this);
            }
        }

        private void OnDestroy() => Unsubscribe();
    }
}
