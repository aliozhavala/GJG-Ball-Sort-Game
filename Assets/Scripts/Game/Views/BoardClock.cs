using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// The board's own time: drives every board tween (<see cref="BoardTweens.OnBoardClock{T}"/>) at
    /// <see cref="AnimationConfig.Speed"/>, slowed down during a hit-stop.
    /// </summary>
    /// <remarks>
    /// Board tweens use DOTween's manual update, which this clock advances once a frame, so a hit-stop
    /// slows the board alone: UI, input, timers, sounds and particles keep normal time, and
    /// <c>Time.timeScale</c> is never touched. Presentation only; no game state lives here.
    /// </remarks>
    public sealed class BoardClock : MonoBehaviour
    {
        [SerializeField] private AnimationConfig _animation;

        private float _hitStopLeft;

        /// <summary>Slows the board to the hit-stop rate for the hit-stop's duration (real time).</summary>
        public void HitStop() => _hitStopLeft = _animation.HitStopDuration;

        /// <summary>Ends a running hit-stop, e.g. when the board is rebuilt.</summary>
        public void CancelHitStop() => _hitStopLeft = 0f;

        private void Update()
        {
            float rate = _animation.Speed;
            if (_hitStopLeft > 0f)
            {
                rate *= _animation.HitStopTimeScale;
                _hitStopLeft -= Time.unscaledDeltaTime;
            }

            DOTween.ManualUpdate(Time.deltaTime * rate, Time.unscaledDeltaTime * rate);
        }

        private void Awake()
        {
            if (_animation == null)
            {
                Debug.LogError($"{name}: animation config must be assigned.", this);
                enabled = false;
            }
        }
    }
}
