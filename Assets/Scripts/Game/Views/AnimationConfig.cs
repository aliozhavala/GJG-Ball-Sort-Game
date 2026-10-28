using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Motion timings and eases. Placeholder values until the animation pass (roadmap step 6).
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "Ball Sort/Animation Config")]
    public sealed class AnimationConfig : ScriptableObject
    {
        [Header("Select")]
        [SerializeField, Min(0f)] private float _liftDuration = 0.12f;
        [SerializeField] private Ease _liftEase = Ease.OutQuad;

        [Header("Move")]
        [SerializeField, Min(0f)] private float _flyDuration = 0.22f;
        [SerializeField] private Ease _flyEase = Ease.InOutQuad;
        [SerializeField, Min(0f)] private float _dropDuration = 0.15f;
        [SerializeField] private Ease _dropEase = Ease.OutQuad;
        [Tooltip("Delay between consecutive balls of one run.")]
        [SerializeField, Min(0f)] private float _ballStagger = 0.04f;

        [Header("Feedback")]
        [SerializeField, Min(0f)] private float _revealFlipDuration = 0.2f;
        [SerializeField, Min(0f)] private float _invalidShakeDuration = 0.25f;
        [SerializeField, Min(0f)] private float _invalidShakeStrength = 0.12f;
        [SerializeField, Min(0f)] private float _capDropDuration = 0.2f;
        [Tooltip("Placeholder until step 6: a refused tap flashes its tube in this colour, fading out.")]
        [SerializeField] private Color _invalidFlashColor = new Color(1f, 0.25f, 0.2f, 0.6f);
        [SerializeField, Min(0f)] private float _invalidFlashDuration = 0.3f;

        [Header("Win")]
        [Tooltip("How long the celebration plays before the Win panel opens.")]
        [SerializeField, Min(0f)] private float _celebrationDuration = 1f;

        public float LiftDuration => _liftDuration;
        public Ease LiftEase => _liftEase;
        public float FlyDuration => _flyDuration;
        public Ease FlyEase => _flyEase;
        public float DropDuration => _dropDuration;
        public Ease DropEase => _dropEase;
        public float BallStagger => _ballStagger;
        public float RevealFlipDuration => _revealFlipDuration;
        public float InvalidShakeDuration => _invalidShakeDuration;
        public float InvalidShakeStrength => _invalidShakeStrength;
        public float CapDropDuration => _capDropDuration;
        public Color InvalidFlashColor => _invalidFlashColor;
        public float InvalidFlashDuration => _invalidFlashDuration;
        public float CelebrationDuration => _celebrationDuration;
    }
}
