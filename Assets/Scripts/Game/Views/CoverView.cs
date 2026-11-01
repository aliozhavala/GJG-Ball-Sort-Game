using System;
using System.Collections.Generic;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using DG.Tweening;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One cover on screen: a veil over one tube (the towel with a sticker of the ball colour that opens
    /// it), or a lock's box across all of its tubes (<see cref="LockBoxView"/>).
    /// </summary>
    /// <remarks>
    /// Pooled by <see cref="BoardView"/> and parented to the board root next to the tubes, so it can
    /// span several of them; <see cref="FitToTubes"/> follows them after every layout change.
    /// <para>
    /// Every change of a cover's state goes through <see cref="SetOpen"/> (a snap) or an opening
    /// (<see cref="PlayVeilLift"/>, <see cref="PlayUnlock"/>), which also hide or show the covered tubes'
    /// balls. Hiding the balls themselves (not just drawing over them) lets the cover art have soft or
    /// translucent edges. A closed veil sways a little from its rim.
    /// </para>
    /// </remarks>
    public sealed class CoverView : MonoBehaviour
    {
        [Tooltip("The veil's towel.")]
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("The sticker on the veil: the open-eyes ball of the colour that opens it.")]
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("The lock box, used by locks only.")]
        [SerializeField] private LockBoxView _lockBox;

        private readonly List<TubeView> _tubes = new List<TubeView>();
        private Color _bodyTint = Color.white;
        private Color _iconTint = Color.white;
        private Vector3 _home;
        private float _shakeX;
        private Tween _shakeTween;
        private AnimationConfig _animation;
        private float _diameter;
        private TweenCallback<float> _shakeStep;

        // The opening playing: a veil's lift or a lock box's unlock.
        private Sequence _opening;

        // The veil's sway while closed and its lift (docs/animation-spec.md, 3.2), about a pivot on its axis.
        private float _rimHeight;
        private float _veilCentre;
        private float _liftY;
        private float _angle;
        private float _pivotY;
        private float _alpha = 1f;
        private float _shade = 1f;
        private float _swayPhase;
        private Tween _sway;
        private Action<CoverView, bool> _liftStarted;
        private bool _liftWithSound;
        private TweenCallback<float> _swayStep;
        private TweenCallback<float> _liftStep;
        private TweenCallback _beginLift;
        private TweenCallback _finishLift;

        // The unlock (docs/animation-spec.md, 3.3).
        private BallView _keyBall;
        private TubeView _keyTube;
        private Action<CoverView, LockBeat> _beat;
        private Vector3 _keyFrom;
        private float _keyFromWidth;
        private Vector3 _keyControl;
        private TweenCallback<float> _keyFlightStep;
        private TweenCallback<float> _keyFadeStep;
        private TweenCallback<float> _wobbleStep;
        private TweenCallback<float> _doorStep;
        private TweenCallback<float> _boxFadeStep;
        private TweenCallback _startKeyFlight;
        private TweenCallback _seatKey;
        private TweenCallback _beginOpening;
        private TweenCallback _finishUnlock;

        public CoverKind Kind { get; private set; }

        /// <summary>The veil's colour id or the lock's id.</summary>
        public int Id { get; private set; }

        /// <summary>The covered tubes; one for a veil, every tube of the lock for a lock box.</summary>
        public IReadOnlyList<TubeView> Tubes => _tubes;

        /// <summary>The model's state: open from the moment its opening is scheduled.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Still drawn over its tubes: closed, or open but still playing its opening.</summary>
        public bool IsDrawn => !IsOpen || (_opening != null && _opening.IsActive());

        /// <summary>A veil's middle, world space (where its lift's smoke puffs).</summary>
        public Vector3 VeilCentre => transform.TransformPoint(new Vector3(0f, _veilCentre, 0f));

        /// <summary>The covered tube's mouth under a veil, world space.</summary>
        public Vector3 VeilMouth => transform.TransformPoint(new Vector3(0f, _rimHeight, 0f));

        /// <summary>
        /// Makes this a veil that opens on <paramref name="colorId"/>, and forgets any previous tubes.
        /// Add its tube, then fit and close it.
        /// </summary>
        public void SetupVeil(int colorId, TubeVisualConfig config, ColorPalette palette, AnimationConfig animation)
        {
            Begin(CoverKind.Veil, colorId);
            _animation = animation;
            _bodyTint = config.VeilTint;
            _body.sprite = config.VeilSprite;
            _body.color = _bodyTint;
            _body.drawMode = SpriteDrawMode.Sliced;

            // The sticker is the colour's own ball, so its body is Veil Sticker Size wide.
            Sprite sticker = palette.GetSprite(colorId, BallExpression.Open);
            float stickerWidth = config.VeilStickerSize * palette.GetSpriteSize(colorId);
            _iconTint = palette.GetSpriteTint(colorId);
            _icon.sprite = sticker;
            _icon.color = _iconTint;
            _icon.transform.localScale = Vector3.one * (stickerWidth / SpriteWidth(sticker));
            _lockBox.gameObject.SetActive(false);
        }

        /// <summary>
        /// Makes this lock <paramref name="lockId"/>'s box (its mid from <paramref name="locks"/>), and
        /// forgets any previous tubes. Add its tubes, then fit and close it.
        /// </summary>
        public void SetupLock(int lockId, TubeVisualConfig config, LockPalette locks)
        {
            Begin(CoverKind.Lock, lockId);
            _body.sprite = null;
            _icon.sprite = null;
            _lockBox.gameObject.SetActive(true);
            _lockBox.Setup(config, locks.GetMid(lockId), locks.GetKey(lockId));
        }

        private void Begin(CoverKind kind, int id)
        {
            Kind = kind;
            Id = id;
            _shade = 1f;
            _tubes.Clear();
            StopMotion();
        }

        public void AddTube(TubeView tube) => _tubes.Add(tube);

        /// <summary>Darkens the cover by <paramref name="shade"/> (1 = as drawn), e.g. while the Shuffle booster picks a tube.</summary>
        public void SetShade(float shade)
        {
            _shade = shade;
            ApplyVeilColours();
            if (Kind == CoverKind.Lock)
            {
                _lockBox.SetShade(shade);
            }
        }

        // The towel and its sticker, shaded and faded as one.
        private void ApplyVeilColours()
        {
            _body.color = Shade(_bodyTint, _shade, _alpha);
            _icon.color = Shade(_iconTint, _shade, _alpha);
        }

        private static Color Shade(Color tint, float shade, float alpha) =>
            new Color(tint.r * shade, tint.g * shade, tint.b * shade, tint.a * alpha);

        /// <summary>
        /// Fits the cover to the covered tubes. A veil hangs from its tube's rim like the art, as wide as
        /// drawn, shortened by <see cref="TubeVisualConfig.VeilHeightScale"/>. A lock box spans from
        /// the left rim of the leftmost tube to the right rim of the rightmost and from below their base
        /// to above their rims (<see cref="TubeVisualConfig.LockBoxPadding"/>).
        /// </summary>
        /// <remarks>A lock's tubes sit next to each other in one row and all have capacity 4 (docs/mechanics.md).</remarks>
        public void FitToTubes(TubeVisualConfig config)
        {
            if (_tubes.Count == 0)
            {
                return;
            }

            float left = float.MaxValue;
            float right = float.MinValue;
            float height = 0f;
            foreach (TubeView tube in _tubes)
            {
                float x = tube.HomePosition.x;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
                height = Mathf.Max(height, tube.BodyHeight);
            }

            _home = new Vector3((left + right) * 0.5f, _tubes[0].HomePosition.y, 0f);
            ApplyOffset();
            if (Kind == CoverKind.Veil)
            {
                FitVeil(config, height);
                return;
            }

            Vector2 padding = config.LockBoxPadding;
            float rimWidth = SpriteWidth(config.RimBackSprite) * config.ArtScale;
            _lockBox.transform.localPosition = new Vector3(0f, -padding.y, 0f);
            _lockBox.Fit(right - left + rimWidth + 2f * padding.x, height + 2f * padding.y);
        }

        /// <remarks>
        /// Drawn at the tube art's scale, squashed vertically by the height scale, so on a capacity-4
        /// tube its hem crosses the bottom ball and that ball's lower half shows. Its hem stays at that
        /// height on taller tubes: only the stripes between the domed top and the hem stretch. The
        /// sticker sits in the veil's middle.
        /// </remarks>
        private void FitVeil(TubeVisualConfig config, float tubeHeight)
        {
            float scaleX = config.ArtScale;
            float scaleY = config.ArtScale * config.VeilHeightScale;
            float artHeight = SpriteHeight(_body.sprite);
            float hemHeight = config.TubeHeight(LevelLayout.NormalCapacity) - artHeight * scaleY;
            float veilHeight = Mathf.Max(tubeHeight - hemHeight, 0.1f);
            _body.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            _body.transform.localPosition = new Vector3(0f, tubeHeight, 0f);
            _body.size = new Vector2(SpriteWidth(_body.sprite), veilHeight / scaleY);
            _rimHeight = tubeHeight;
            _veilCentre = tubeHeight - veilHeight * 0.5f;
            _icon.transform.localPosition = new Vector3(0f, _veilCentre, 0f);
        }

        /// <summary>
        /// The single entry point for a cover's state: closed shows the cover and hides the covered
        /// balls (a veil leaves its tube's bottom ball showing under its hem; a lock box hides them all),
        /// open hides the cover and shows them again. Snaps, ending an opening still playing; a lock's
        /// opening is <see cref="PlayUnlock"/>.
        /// </summary>
        public void SetOpen(bool open)
        {
            KillOpening();
            ApplyOpen(open);
        }

        private void ApplyOpen(bool open)
        {
            IsOpen = open;
            if (Kind == CoverKind.Veil && !open)
            {
                StartSway();
            }
            else
            {
                StopSway();
            }

            _body.enabled = !open && _body.sprite != null;
            _icon.enabled = !open && _icon.sprite != null;
            if (Kind == CoverKind.Lock)
            {
                _lockBox.SetClosed(!open);
            }

            foreach (TubeView tube in _tubes)
            {
                tube.SetCovered(!open, showBottomBall: Kind == CoverKind.Veil);
            }
        }

        /// <summary>
        /// The invalid-move shake (docs/animation-spec.md, 2.5) for a tap on a covered tube: the cover
        /// shakes with every tube under it, so nothing peeks out.
        /// </summary>
        public void Shake(AnimationConfig animation, float ballDiameter)
        {
            _animation = animation;
            _diameter = ballDiameter;
            BoardTweens.KillIfActive(_shakeTween);
            float duration = animation.InvalidShakeDuration;
            _shakeTween = DOVirtual.Float(0f, duration, duration, _shakeStep)
                .SetEase(Ease.Linear).OnComplete(StopMotion).OnBoardClock();
            foreach (TubeView tube in _tubes)
            {
                tube.Shake();
            }
        }

        /// <summary>
        /// Plays a veil's lift (docs/animation-spec.md, 3.2) after <paramref name="delay"/> seconds of board
        /// time: the towel and its sticker rise, fade and tilt a little mid-way; the tube's balls show from
        /// its start. The cover counts as open from now on. <paramref name="started"/> hears the start
        /// (with <paramref name="withSound"/>) unless a tap on the tube skips through it.
        /// </summary>
        /// <returns>The lift, to keep as a beat on the tube.</returns>
        public Tween PlayVeilLift(
            float delay, float ballDiameter, AnimationConfig animation, bool withSound, Action<CoverView, bool> started)
        {
            KillOpening();
            IsOpen = true;
            _animation = animation;
            _diameter = ballDiameter;
            _liftStarted = started;
            _liftWithSound = withSound;

            Sequence lift = DOTween.Sequence();
            lift.InsertCallback(0f, _beginLift);
            lift.Insert(0f, DOVirtual.Float(0f, 1f, animation.TowelLiftDuration, _liftStep).SetEase(Ease.Linear));
            lift.OnComplete(_finishLift);
            lift.SetDelay(delay);
            _opening = lift.OnBoardClock();
            return _opening;
        }

        // The sway stops, the balls under the towel show, and the lift's smoke and sound start.
        private void BeginLift()
        {
            StopSway();
            _pivotY = _veilCentre;
            foreach (TubeView tube in _tubes)
            {
                tube.SetCovered(false, showBottomBall: false);
            }

            if (!IsSkipped())
            {
                _liftStarted?.Invoke(this, _liftWithSound);
            }
        }

        // Smoothstep: rises and fades together; the tilt peaks mid-way, about the towel's middle.
        private void LiftStep(float u)
        {
            float eased = u * u * (3f - 2f * u);
            _liftY = _animation.TowelRise * _diameter * eased;
            _angle = _animation.TowelTilt * Mathf.Sin(Mathf.PI * u);
            _alpha = 1f - eased;
            ApplyOffset();
            ApplyVeilColours();
        }

        private void FinishLift()
        {
            _opening = null;
            ResetVeilPose();
            ApplyOpen(true);
        }

        // A closed veil sways from its rim, each with its own phase; the toggle is read every step.
        private void StartSway()
        {
            StopSway();
            if (_animation == null)
            {
                return;
            }

            float period = Mathf.Max(_animation.TowelSwayPeriod, 0.05f);
            _swayPhase = UnityEngine.Random.value * 2f * Mathf.PI;
            _sway = DOVirtual.Float(0f, period, period, _swayStep)
                .SetEase(Ease.Linear).SetLoops(-1).OnBoardClock();
        }

        private void SwayStep(float t)
        {
            float period = Mathf.Max(_animation.TowelSwayPeriod, 0.05f);
            _pivotY = _rimHeight;
            _angle = _animation.TowelSway
                ? _animation.TowelSwayAngle * Mathf.Sin(2f * Mathf.PI * t / period + _swayPhase)
                : 0f;
            ApplyOffset();
        }

        private void StopSway()
        {
            BoardTweens.KillIfActive(_sway);
            _sway = null;
            _angle = 0f;
            ApplyOffset();
        }

        private void ResetVeilPose()
        {
            _liftY = 0f;
            _angle = 0f;
            _alpha = 1f;
            ApplyOffset();
            ApplyVeilColours();
        }

        /// <summary>
        /// Plays a lock box's opening (docs/animation-spec.md, 3.3) after <paramref name="delay"/> seconds
        /// of board time: the key leaves <paramref name="keyBall"/>'s hands and flies into the slot, the box
        /// wobbles as it seats and the key fades; then the mid gives way to the two cover halves, which
        /// slide apart and show the tubes (with the inside behind them), and the rest of the box fades.
        /// The cover counts as open from now on. <paramref name="beat"/> hears each step that plays in time,
        /// not those a tap on a tube skips through.
        /// </summary>
        /// <param name="keyBall">The ball holding the key, or null to open without the key's flight.</param>
        /// <param name="keyTube">The key ball's tube, which can finish the opening like the box's own tubes.</param>
        /// <returns>The opening, to keep as a beat on the box's tubes and the key's.</returns>
        public Tween PlayUnlock(
            BallView keyBall, TubeView keyTube, float delay, float ballDiameter, AnimationConfig animation,
            Action<CoverView, LockBeat> beat)
        {
            KillOpening();
            IsOpen = true;
            _animation = animation;
            _diameter = ballDiameter;
            _keyBall = keyBall;
            _keyTube = keyTube;
            _beat = beat;

            float fly = animation.KeyFlyDuration;
            float wobble = animation.BoxWobbleDuration;
            float fade = animation.LockFadeDuration;
            float open = fly + animation.BoxOpenDelay;
            float fadeOut = open + animation.BoxSlideDuration + animation.BoxHoldDuration;

            Sequence unlock = DOTween.Sequence();
            unlock.InsertCallback(0f, _startKeyFlight);
            unlock.Insert(0f, DOVirtual.Float(0f, 1f, fly, _keyFlightStep).SetEase(animation.KeyFlyEase));
            unlock.InsertCallback(fly, _seatKey);
            unlock.Insert(fly, DOVirtual.Float(0f, wobble, wobble, _wobbleStep).SetEase(Ease.Linear));
            unlock.Insert(fly + animation.KeyHoldDuration, DOVirtual.Float(1f, 0f, fade, _keyFadeStep).SetEase(Ease.Linear));
            unlock.InsertCallback(open, _beginOpening);
            unlock.Insert(open, DOVirtual.Float(0f, 1f, animation.BoxSlideDuration, _doorStep).SetEase(animation.BoxSlideEase));
            unlock.Insert(fadeOut, DOVirtual.Float(1f, 0f, fade, _boxFadeStep).SetEase(Ease.Linear));
            unlock.OnComplete(_finishUnlock);
            unlock.SetDelay(delay);
            _opening = unlock.OnBoardClock();
            return _opening;
        }

        // The key leaves the ball's hands (or appears in the slot when there is no ball to take it from).
        private void StartKeyFlight()
        {
            Transform box = _lockBox.transform;
            if (_keyBall != null && _keyBall.TryTakeKey(out Vector3 position, out float width))
            {
                _keyFrom = box.InverseTransformPoint(position);
                _keyFromWidth = width / Mathf.Max(Mathf.Abs(box.lossyScale.x), 0.0001f);
            }
            else
            {
                _keyFrom = _lockBox.SlotPosition;
                _keyFromWidth = _lockBox.SeatedKeyWidth;
            }

            // A quadratic Bezier whose control point sits above the higher end.
            Vector3 to = _lockBox.SlotPosition;
            _keyControl = new Vector3(
                (_keyFrom.x + to.x) * 0.5f, Mathf.Max(_keyFrom.y, to.y) + _animation.KeyArcHeight * _diameter, 0f);
            _lockBox.SetKeyPose(_keyFrom, _keyFromWidth, 1f);
            Beat(LockBeat.KeyFlying);
        }

        private void KeyFlightStep(float u)
        {
            float v = 1f - u;
            Vector3 position = v * v * _keyFrom + 2f * v * u * _keyControl + u * u * _lockBox.SlotPosition;
            _lockBox.SetKeyPose(position, Mathf.Lerp(_keyFromWidth, _lockBox.SeatedKeyWidth, u), 1f);
        }

        private void SeatKey() => Beat(LockBeat.KeySeated);

        private void KeyFadeStep(float alpha) =>
            _lockBox.SetKeyPose(_lockBox.SlotPosition, _lockBox.SeatedKeyWidth, alpha);

        // A damped tilt and squash that dies out by the end of the wobble.
        private void WobbleStep(float t)
        {
            float duration = _animation.BoxWobbleDuration;
            float envelope = Mathf.Exp(-_animation.BoxWobbleDamping * t) * Mathf.Clamp01(1f - t / duration);
            float phase = _animation.BoxWobbleFrequency * t;
            _lockBox.SetWobble(
                _animation.BoxWobbleAngle * Mathf.Sin(phase) * envelope,
                _animation.BoxWobbleSquash * Mathf.Cos(phase) * envelope);
        }

        // The mid gives way to the covers, and the tubes' balls are drawn again behind them.
        private void BeginOpening()
        {
            _lockBox.SetWobble(0f, 0f);
            _lockBox.BeginOpening();
            foreach (TubeView tube in _tubes)
            {
                tube.SetCovered(false, showBottomBall: false);
            }

            Beat(LockBeat.Opening);
        }

        private void DoorStep(float progress) => _lockBox.SetDoors(progress);

        private void BoxFadeStep(float alpha) => _lockBox.SetFade(alpha);

        private void FinishUnlock()
        {
            _opening = null;
            _keyBall = null;
            _keyTube = null;
            ApplyOpen(true);
        }

        // A step a tap skips through (finishing a tube's beats) stays silent.
        private void Beat(LockBeat beat)
        {
            if (!IsSkipped())
            {
                _beat?.Invoke(this, beat);
            }
        }

        private bool IsSkipped()
        {
            if (_keyTube != null && _keyTube.IsFinishing)
            {
                return true;
            }

            foreach (TubeView tube in _tubes)
            {
                if (tube.IsFinishing)
                {
                    return true;
                }
            }

            return false;
        }

        private void KillOpening()
        {
            BoardTweens.KillIfActive(_opening);
            _opening = null;
            _keyBall = null;
            _keyTube = null;
        }

        /// <summary>Ends a running shake, sway or opening: the cover is rebuilt or pooled.</summary>
        public void StopMotion()
        {
            KillOpening();
            BoardTweens.KillIfActive(_sway);
            _sway = null;
            _liftY = 0f;
            _angle = 0f;
            _alpha = 1f;
            ApplyVeilColours();
            BoardTweens.KillIfActive(_shakeTween);
            _shakeX = 0f;
            ApplyOffset();
        }

        private void ShakeStep(float t)
        {
            _shakeX = _animation.InvalidShakeAmount * _diameter *
                BoardTweens.Shake(t, _animation.InvalidShakeFrequency, _animation.InvalidShakeDamping);
            ApplyOffset();
        }

        // The shake and lift move the cover; the sway and tilt turn it about a point on its axis.
        private void ApplyOffset()
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, _angle);
            var pivot = new Vector3(0f, _pivotY, 0f);
            transform.localRotation = rotation;
            transform.localPosition = _home + new Vector3(_shakeX, _liftY, 0f) + pivot - rotation * pivot;
        }

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 1f;

        private static float SpriteHeight(Sprite sprite) => sprite != null ? sprite.bounds.size.y : 1f;

        private void Awake()
        {
            if (_body == null || _icon == null || _lockBox == null)
            {
                Debug.LogError($"{name}: body, icon and lock box must be assigned.", this);
            }

            _shakeStep = ShakeStep;
            _keyFlightStep = KeyFlightStep;
            _keyFadeStep = KeyFadeStep;
            _wobbleStep = WobbleStep;
            _doorStep = DoorStep;
            _boxFadeStep = BoxFadeStep;
            _startKeyFlight = StartKeyFlight;
            _seatKey = SeatKey;
            _beginOpening = BeginOpening;
            _finishUnlock = FinishUnlock;
            _swayStep = SwayStep;
            _liftStep = LiftStep;
            _beginLift = BeginLift;
            _finishLift = FinishLift;
        }

        private void OnDestroy()
        {
            BoardTweens.KillIfActive(_shakeTween);
            BoardTweens.KillIfActive(_opening);
            BoardTweens.KillIfActive(_sway);
        }
    }
}
