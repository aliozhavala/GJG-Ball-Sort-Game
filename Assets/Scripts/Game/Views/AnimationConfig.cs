using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// Board motion: timings, eases and amounts of the select, flight, landing, invalid-move and
    /// tube-complete animations (docs/animation-spec.md, which names each field). Distances are in ball
    /// diameters (d), times in seconds at <see cref="Speed"/> 1.
    /// </summary>
    /// <remarks>
    /// Every board tween runs on the <see cref="BoardClock"/>, which applies <see cref="Speed"/> and the
    /// hit-stop, so durations here never need dividing by the speed.
    /// </remarks>
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "Ball Sort/Animation Config")]
    public sealed class AnimationConfig : ScriptableObject
    {
        [Tooltip("Board animation speed: 2 plays every board animation twice as fast.")]
        [SerializeField, Min(0.05f)] private float _speed = 1f;

        [Header("2.2 Select")]
        [Tooltip("Height of a lifted ball's centre above its tube's mouth, in d. Also the tap area above the rim.")]
        [SerializeField, Min(0f)] private float _liftHeight = 0.72f;
        [SerializeField, Min(0f)] private float _liftDuration = 0.23f;
        [SerializeField] private Ease _liftEase = Ease.OutCubic;
        [Tooltip("Bob of a held ball, up and down, in d.")]
        [SerializeField, Min(0f)] private float _liftBob = 0.06f;
        [SerializeField, Min(0.05f)] private float _liftBobPeriod = 1.25f;

        [Header("2.3 Flight")]
        [SerializeField, Min(0f)] private float _riseDuration = 0.1f;
        [SerializeField] private Ease _riseEase = Ease.OutQuad;
        [SerializeField, Min(0f)] private float _arcBaseDuration = 0.15f;
        [Tooltip("Added to the arc's duration per screen width travelled (horizontal + vertical distance between the mouths).")]
        [SerializeField, Min(0f)] private float _arcDurationPerScreenWidth = 0.23f;
        [SerializeField] private Ease _arcEase = Ease.InOutSine;
        [Tooltip("Arc height above the straight line, in d.")]
        [SerializeField, Min(0f)] private float _arcHeightBase = 0.5f;
        [Tooltip("Arc height added per unit of distance between the mouths.")]
        [SerializeField, Min(0f)] private float _arcHeightPerDistance = 0.32f;
        [SerializeField, Min(0f)] private float _dropDuration = 0.12f;
        [SerializeField] private Ease _dropEase = Ease.InQuad;
        [Tooltip("Delay between consecutive balls of one move.")]
        [SerializeField, Min(0f)] private float _chainDelay = 0.08f;
        [Tooltip("Scales every squash and stretch.")]
        [SerializeField, Min(0f)] private float _squash = 0.7f;
        [Tooltip("Speed, in screen widths per second, that gives the full stretch.")]
        [SerializeField, Min(0.01f)] private float _stretchFullSpeed = 6.7f;
        [SerializeField, Range(0f, 1f)] private float _stretchMax = 0.3f;
        [Tooltip("No stretch below this speed, in screen widths per second.")]
        [SerializeField, Min(0f)] private float _stretchMinSpeed = 0.15f;
        [SerializeField] private bool _trail = true;
        [Tooltip("How long a trail point lives (0.15 s = 9 frames at 60 fps). Real time, not board time.")]
        [SerializeField, Min(0.01f)] private float _trailTime = 0.15f;
        [Tooltip("Trail width at the ball, in ball radii.")]
        [SerializeField, Min(0f)] private float _trailWidth = 1.4f;
        [SerializeField, Range(0f, 1f)] private float _trailAlpha = 0.45f;

        [Header("2.4 Landing")]
        [SerializeField, Min(0f)] private float _landSquashAmount = 0.32f;
        [SerializeField, Min(0f)] private float _landSquashDuration = 0.45f;
        [SerializeField, Min(0f)] private float _landSquashDamping = 11f;
        [SerializeField, Min(0f)] private float _landSquashFrequency = 30f;
        [Tooltip("Hop bounce height, in d (two bounces over the duration, damped).")]
        [SerializeField, Min(0f)] private float _hopBounceHeight = 0.4f;
        [SerializeField, Min(0.01f)] private float _hopBounceDuration = 0.55f;
        [SerializeField, Min(0f)] private float _hopBounceDamping = 4f;
        [Tooltip("Bump of the balls below a landing ball and of its tube, in d.")]
        [SerializeField, Min(0f)] private float _bumpHeight = 0.07f;
        [SerializeField, Min(0.01f)] private float _ballBumpDuration = 0.2f;
        [Tooltip("Extra delay per ball of depth below the landing ball.")]
        [SerializeField, Min(0f)] private float _bumpRippleDelay = 0.03f;
        [SerializeField, Min(0.01f)] private float _tubeBumpDuration = 0.22f;
        [SerializeField] private bool _droplets = true;

        [Header("2.5 Invalid move")]
        [Tooltip("Sideways shake of the tapped tube, in d.")]
        [SerializeField, Min(0f)] private float _invalidShakeAmount = 0.16f;
        [SerializeField, Min(0.01f)] private float _invalidShakeDuration = 0.4f;
        [SerializeField, Min(0f)] private float _invalidShakeFrequency = 55f;
        [SerializeField, Min(0f)] private float _invalidShakeDamping = 9f;
        [SerializeField, Min(0f)] private float _shockFaceDuration = 0.7f;

        [Header("2.6 Tube complete")]
        [Tooltip("Real seconds the board runs slowed down when the cork seats.")]
        [SerializeField, Min(0f)] private float _hitStopDuration = 0.06f;
        [SerializeField, Range(0f, 1f)] private float _hitStopTimeScale = 0.06f;
        [Tooltip("Board scale kick when the cork seats: 1 → 1 + this → 1.")]
        [SerializeField, Min(0f)] private float _punchScale = 0.028f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.3f;
        [Tooltip("How far above the rim the cork starts, in d. Keep it inside the gap between rows (the prototype's 90 px, about 2 d, reaches the row above).")]
        [SerializeField, Min(0f)] private float _corkDropHeight = 1.2f;
        [SerializeField, Min(0f)] private float _corkDropDuration = 0.45f;
        [SerializeField] private Ease _corkEase = Ease.OutBounce;
        [Tooltip("When the cork first touches the rim (OutBounce's first contact): cork sound, hit-stop and punch.")]
        [SerializeField, Min(0f)] private float _corkImpactTime = 0.16f;
        [SerializeField] private bool _glow = true;
        [SerializeField, Min(0f)] private float _glowFadeDuration = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _glowIdleAlpha = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _glowPulse = 0.12f;
        [SerializeField, Min(0.05f)] private float _glowPulsePeriod = 1.6f;
        [SerializeField] private bool _dance = true;
        [Tooltip("Delay between the balls' hops, bottom to top.")]
        [SerializeField, Min(0f)] private float _danceStagger = 0.07f;
        [SerializeField] private bool _sparkles = true;
        [SerializeField] private bool _confetti = true;
        [Tooltip("Sparkles grow with the number of tubes already completed in the level.")]
        [SerializeField] private bool _chainEscalate = true;

        [Header("3.4 Ice break")]
        [Tooltip("When a clay (ice) tube completes, its ice cracks for this long, then bursts (sound, shards, ring). " +
            "Same moment as the cork's impact by default, so the two sounds land together.")]
        [SerializeField, Min(0f)] private float _iceCrackTime = 0.16f;
        [Tooltip("Sideways shiver of the cracking ice, in d (stands in for the crack art).")]
        [SerializeField, Min(0f)] private float _iceShiverAmount = 0.04f;
        [SerializeField, Min(0f)] private float _iceShiverFrequency = 70f;
        [Tooltip("How far the cracking ice turns white before it bursts: 0 = not at all, 1 = fully white.")]
        [SerializeField, Range(0f, 1f)] private float _iceCrackBrighten = 0.5f;

        [Header("6.1 Blink")]
        [Tooltip("Now and then one ball on the board blinks (a revealed ball resting with its open face).")]
        [SerializeField] private bool _blink = true;
        [Tooltip("Board seconds between two blinks on the whole board (one ball each time), picked at random between " +
            "the shortest and the longest for each blink. Lower = more blinking.")]
        [SerializeField, Min(0.1f)] private float _boardBlinkIntervalMin = 2f;
        [SerializeField, Min(0.1f)] private float _boardBlinkIntervalMax = 5f;
        [Tooltip("Half-open frame while the eyes close.")]
        [SerializeField, Min(0f)] private float _blinkCloseDuration = 0.08f;
        [SerializeField, Min(0f)] private float _blinkClosedDuration = 0.16f;
        [Tooltip("Half-open frame while the eyes open again.")]
        [SerializeField, Min(0f)] private float _blinkOpenDuration = 0.08f;

        [Header("Later beats (snaps until their animations exist)")]
        [Tooltip("A veil opens this long after the last ball of the completing move lands.")]
        [SerializeField, Min(0f)] private float _towelDelay = 0.45f;
        [Tooltip("A found key's mark leaves this long after the last ball lands, when the move also completed a tube.")]
        [SerializeField, Min(0f)] private float _keyWaitAfterComplete = 0.25f;

        [Header("5.1 Stuck")]
        [Tooltip("The stuck bar appears this long after the board has settled (the last ball landed, the last beat played).")]
        [SerializeField, Min(0f)] private float _stuckDelay = 0.4f;
        [Tooltip("The stuck bar's fade-in (real time).")]
        [SerializeField, Min(0f)] private float _stuckFadeDuration = 0.25f;

        [Header("Win (placeholder)")]
        [Tooltip("How long the celebration plays, once the board has settled, before the Win panel opens.")]
        [SerializeField, Min(0f)] private float _celebrationDuration = 1f;

        [Header("UI (real time, not board time)")]
        [Tooltip("A purchase's punch on what it filled (booster badge, lives heart, coins pill): scale 1 → 1 + this → 1, springing.")]
        [SerializeField, Min(0f)] private float _purchasePunchScale = 0.25f;
        [SerializeField, Min(0.01f)] private float _purchasePunchDuration = 0.35f;
        [Tooltip("Spring oscillations of the purchase punch.")]
        [SerializeField, Range(1, 12)] private int _purchasePunchVibrato = 6;

        public float Speed => _speed;
        public float LiftHeight => _liftHeight;
        public float LiftDuration => _liftDuration;
        public Ease LiftEase => _liftEase;
        public float LiftBob => _liftBob;
        public float LiftBobPeriod => _liftBobPeriod;
        public float RiseDuration => _riseDuration;
        public Ease RiseEase => _riseEase;
        public float ArcBaseDuration => _arcBaseDuration;
        public float ArcDurationPerScreenWidth => _arcDurationPerScreenWidth;
        public Ease ArcEase => _arcEase;
        public float ArcHeightBase => _arcHeightBase;
        public float ArcHeightPerDistance => _arcHeightPerDistance;
        public float DropDuration => _dropDuration;
        public Ease DropEase => _dropEase;
        public float ChainDelay => _chainDelay;
        public float Squash => _squash;
        public float StretchFullSpeed => _stretchFullSpeed;
        public float StretchMax => _stretchMax;
        public float StretchMinSpeed => _stretchMinSpeed;
        public bool Trail => _trail;
        public float TrailTime => _trailTime;
        public float TrailWidth => _trailWidth;
        public float TrailAlpha => _trailAlpha;
        public float LandSquashAmount => _landSquashAmount;
        public float LandSquashDuration => _landSquashDuration;
        public float LandSquashDamping => _landSquashDamping;
        public float LandSquashFrequency => _landSquashFrequency;
        public float HopBounceHeight => _hopBounceHeight;
        public float HopBounceDuration => _hopBounceDuration;
        public float HopBounceDamping => _hopBounceDamping;
        public float BumpHeight => _bumpHeight;
        public float BallBumpDuration => _ballBumpDuration;
        public float BumpRippleDelay => _bumpRippleDelay;
        public float TubeBumpDuration => _tubeBumpDuration;
        public bool Droplets => _droplets;
        public float InvalidShakeAmount => _invalidShakeAmount;
        public float InvalidShakeDuration => _invalidShakeDuration;
        public float InvalidShakeFrequency => _invalidShakeFrequency;
        public float InvalidShakeDamping => _invalidShakeDamping;
        public float ShockFaceDuration => _shockFaceDuration;
        public float HitStopDuration => _hitStopDuration;
        public float HitStopTimeScale => _hitStopTimeScale;
        public float PunchScale => _punchScale;
        public float PunchDuration => _punchDuration;
        public float CorkDropHeight => _corkDropHeight;
        public float CorkDropDuration => _corkDropDuration;
        public Ease CorkEase => _corkEase;
        public float CorkImpactTime => _corkImpactTime;
        public bool Glow => _glow;
        public float GlowFadeDuration => _glowFadeDuration;
        public float GlowIdleAlpha => _glowIdleAlpha;
        public float GlowPulse => _glowPulse;
        public float GlowPulsePeriod => _glowPulsePeriod;
        public bool Dance => _dance;
        public float DanceStagger => _danceStagger;
        public bool Sparkles => _sparkles;
        public bool Confetti => _confetti;
        public bool ChainEscalate => _chainEscalate;
        public float IceCrackTime => _iceCrackTime;
        public float IceShiverAmount => _iceShiverAmount;
        public float IceShiverFrequency => _iceShiverFrequency;
        public float IceCrackBrighten => _iceCrackBrighten;
        public bool Blink => _blink;
        public float BoardBlinkIntervalMin => Mathf.Min(_boardBlinkIntervalMin, _boardBlinkIntervalMax);
        public float BoardBlinkIntervalMax => Mathf.Max(_boardBlinkIntervalMin, _boardBlinkIntervalMax);
        public float BlinkCloseDuration => _blinkCloseDuration;
        public float BlinkClosedDuration => _blinkClosedDuration;
        public float BlinkOpenDuration => _blinkOpenDuration;
        public float TowelDelay => _towelDelay;
        public float KeyWaitAfterComplete => _keyWaitAfterComplete;
        public float StuckDelay => _stuckDelay;
        public float StuckFadeDuration => _stuckFadeDuration;
        public float CelebrationDuration => _celebrationDuration;
        public float PurchasePunchScale => _purchasePunchScale;
        public float PurchasePunchDuration => _purchasePunchDuration;
        public int PurchasePunchVibrato => _purchasePunchVibrato;
    }
}
