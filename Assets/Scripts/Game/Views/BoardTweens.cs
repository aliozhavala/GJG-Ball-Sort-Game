using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>Helpers shared by the board's views: the board clock and the motion formulas of docs/animation-spec.md.</summary>
    public static class BoardTweens
    {
        /// <summary>Runs <paramref name="tween"/> on the <see cref="BoardClock"/> instead of DOTween's own update.</summary>
        public static T OnBoardClock<T>(this T tween) where T : Tween => tween.SetUpdate(UpdateType.Manual);

        /// <summary>Completes <paramref name="tween"/> if it is still running (its callbacks fire).</summary>
        public static void CompleteIfActive(Tween tween)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Complete(true);
            }
        }

        /// <summary>Kills <paramref name="tween"/> if it is still running (no callbacks).</summary>
        public static void KillIfActive(Tween tween)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }

        /// <summary>A half sine bump: 0 → 1 → 0 over <paramref name="duration"/>.</summary>
        public static float HalfSine(float t, float duration) => Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));

        /// <summary>Two damped bounces over <paramref name="duration"/>: |sin(2πt / duration)| · e^(−damping·t).</summary>
        public static float Hop(float t, float duration, float damping) =>
            Mathf.Abs(Mathf.Sin(2f * Mathf.PI * t / duration)) * Mathf.Exp(-damping * t);

        /// <summary>A damped wobble starting at 1: e^(−damping·t) · cos(frequency·t).</summary>
        public static float Wobble(float t, float frequency, float damping) =>
            Mathf.Exp(-damping * t) * Mathf.Cos(frequency * t);

        /// <summary>A damped shake starting at 0: sin(frequency·t) · e^(−damping·t).</summary>
        public static float Shake(float t, float frequency, float damping) =>
            Mathf.Sin(frequency * t) * Mathf.Exp(-damping * t);
    }
}
