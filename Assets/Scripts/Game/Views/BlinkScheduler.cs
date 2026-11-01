using System.Collections.Generic;
using DG.Tweening;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Makes one ball on the board blink now and then (docs/animation-spec.md, 6.1): after a random wait
    /// between <see cref="AnimationConfig.BoardBlinkIntervalMin"/> and
    /// <see cref="AnimationConfig.BoardBlinkIntervalMax"/> of board time, a random ball that can blink
    /// (<see cref="BallView.CanBlink"/>) does, so the board's blink rate does not grow with its ball count.
    /// </summary>
    /// <remarks>
    /// Owned by the <see cref="BoardView"/>, which reads the tubes it is given anew at every blink, so
    /// restarts, moves and added tubes need nothing. <see cref="AnimationConfig.Blink"/> is read at
    /// every blink too, so it can be switched in Play mode.
    /// </remarks>
    public sealed class BlinkScheduler
    {
        private readonly IReadOnlyList<TubeView> _tubes;
        private readonly AnimationConfig _animation;
        private readonly List<BallView> _candidates = new List<BallView>();
        private readonly TweenCallback _onDue;
        private Tween _next;

        public BlinkScheduler(IReadOnlyList<TubeView> tubes, AnimationConfig animation)
        {
            _tubes = tubes;
            _animation = animation;
            _onDue = OnDue;
        }

        /// <summary>Starts the blinks, or starts their wait over.</summary>
        public void Start()
        {
            Stop();
            ScheduleNext();
        }

        /// <summary>No more blinks until <see cref="Start"/>; a blink already playing finishes.</summary>
        public void Stop() => BoardTweens.KillIfActive(_next);

        private void ScheduleNext() =>
            _next = DOVirtual.DelayedCall(
                    UnityEngine.Random.Range(_animation.BoardBlinkIntervalMin, _animation.BoardBlinkIntervalMax), _onDue, false)
                .OnBoardClock();

        private void OnDue()
        {
            if (_animation.Blink)
            {
                _candidates.Clear();
                foreach (TubeView tube in _tubes)
                {
                    foreach (BallView ball in tube.Balls)
                    {
                        if (ball.CanBlink)
                        {
                            _candidates.Add(ball);
                        }
                    }
                }

                if (_candidates.Count > 0)
                {
                    _candidates[UnityEngine.Random.Range(0, _candidates.Count)].TryBlink();
                }
            }

            ScheduleNext();
        }
    }
}
