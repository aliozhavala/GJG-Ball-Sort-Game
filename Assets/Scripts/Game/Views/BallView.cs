using System;
using BallSort.Core.Model;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One ball on screen. Pooled by <see cref="BoardView"/>; never shows a hidden ball's colour or key.
    /// </summary>
    /// <remarks>
    /// The root is scaled to the ball diameter; the body and the marks are normalised to it, so any
    /// sprite size works. All are SpriteRenderers so they scale and sort together.
    /// <para>
    /// Every change of position goes through <see cref="MoveTo(Vector3)"/> and its overloads (snap,
    /// tween, flight), and every change of the key mark through <see cref="UpdateKeyMark"/>. The root's
    /// position is the moved position plus the bounce offsets (hop, bump, bob); squash and stretch
    /// scale the shape, whose face child turns back upright so the art never tilts. All motion runs
    /// on the <see cref="BoardClock"/>, and <see cref="FinishMotion"/> ends it at once, so a ball
    /// touched mid-animation is where the model says before it moves again.
    /// </para>
    /// <para>
    /// Its face goes through <see cref="SetExpression(BallExpression, ColorPalette)"/> alone: a
    /// resting face (open, or laughing in a completed tube) and a face held for a while (shock).
    /// A revealed ball resting with its open face blinks when the board asks (<see cref="TryBlink"/>:
    /// half-open, closed, half-open, docs/animation-spec.md, 6.1); a blink never shows over another
    /// face, and touching the ball does not cut it short. Balls of one tube draw in slot order, upper balls in front, so
    /// appendages reaching into the slot above tuck behind that ball.
    /// </para>
    /// <para>
    /// Where it draws follows from where it is (<see cref="SetMouth"/>): inside its tube, between the
    /// rim's back and front halves (under the clay and the cork); above every tube from the moment its
    /// bottom clears the mouth it is in, so a flight never passes behind another tube's rim. A flight
    /// measures against the source's mouth while it rises, is above every tube through the arc, and
    /// measures against the target's mouth as it drops in.
    /// </para>
    /// <para>
    /// The ball keeps its own copy of its key: by the time a reveal is announced the board has
    /// already found the key (docs/mechanics.md), so the mark must show on the reveal and only go
    /// when <see cref="RemoveKey"/> follows.
    /// </para>
    /// </remarks>
    public sealed class BallView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("The \"?\" drawn over the body while the ball is hidden.")]
        [SerializeField] private SpriteRenderer _hiddenMark;
        [Tooltip("The key drawn on a revealed ball that carries one.")]
        [SerializeField] private SpriteRenderer _keyMark;
        [Tooltip("Turned and scaled for squash and stretch; parent of the face.")]
        [SerializeField] private Transform _shape;
        [Tooltip("Child of the shape, turned back upright; parent of the body and marks.")]
        [SerializeField] private Transform _face;
        [Tooltip("Follows the ball through the arc and drop.")]
        [SerializeField] private TrailRenderer _trail;

        private int _bodySortingLayer;
        private int _bodySortingOrder;
        private int _markSortingLayer;
        private int _markSortingOrder;
        private int _keySortingLayer;
        private int _keySortingOrder;
        private int _ordersPerBall = 1;
        private int _raisedSortingLayer;
        private int _raisedSortingOrder;

        // The ball draws above every tube while its centre is at or above this height (parent space).
        private float _clearHeight = float.PositiveInfinity;
        private float _targetClearHeight;

        private AnimationConfig _animation;
        private ColorPalette _palette;
        private Action<BallView> _departed;
        private Action<BallView> _landed;

        private bool _isHidden;
        private int _colorId;
        private BallExpression _restingExpression;
        private BallExpression _expression;
        private int _keyLockId = Ball.NoKey;

        // Shuffle's dimming (1 = as drawn), and the tints it multiplies.
        private float _shade = 1f;
        private Color _bodyTint = Color.white;
        private float _diameter = 1f;

        // Position = moved position + bounce offsets, all in the parent's (board) units.
        private Vector3 _position;
        private float _hop;
        private float _bump;
        private float _bob;

        private Tween _motion;
        private Tween _hopTween;
        private Tween _bumpTween;
        private Tween _squashTween;
        private Tween _bobTween;
        private Tween _faceTween;
        private Tween _blinkTween;
        private Tween _wiggleTween;
        private Tween _flipTween;

        // Where the current blink is: 0 waiting, 1 closing, 2 closed, 3 opening.
        private int _blinkPhase;

        // The flight in progress.
        private Vector3 _arcFrom;
        private Vector3 _arcTo;
        private float _arcHeight;
        private float _screenWidth;
        private Vector3 _lastPosition;
        private float _lastElapsed;
        private bool _isFlight;
        private bool _hasDeparted;
        private bool _hasLanded;

        // Cached so starting a tween allocates no delegate.
        private DOGetter<Vector3> _getPosition;
        private DOSetter<Vector3> _setPosition;
        private TweenCallback<float> _arcStep;
        private TweenCallback<float> _hopStep;
        private TweenCallback<float> _bumpStep;
        private TweenCallback<float> _squashStep;
        private TweenCallback<float> _bobStep;
        private TweenCallback<float> _wiggleStep;
        private TweenCallback _endWiggle;
        private TweenCallback<float> _flipStep;
        private TweenCallback _endFlip;
        private int _flipColour;
        private bool _flipTurned;
        private TweenCallback _onDeparted;
        private TweenCallback _onArcEnded;
        private TweenCallback _onLanded;
        private TweenCallback _onFlightUpdate;
        private TweenCallback _restoreFace;
        private TweenCallback _blinkStep;

        /// <summary>Board index of the tube holding this ball.</summary>
        public int TubeIndex { get; private set; } = -1;

        /// <summary>Slot within that tube, 0 = bottom.</summary>
        public int Slot { get; private set; } = -1;

        /// <summary>True while drawn above every tube: out of its tube's mouth (lifted or flying).</summary>
        public bool IsRaised { get; private set; }

        /// <summary>The ball's colour id (meaningful once revealed).</summary>
        public int ColorId => _colorId;

        public bool IsHidden => _isHidden;

        /// <summary>True from the start of a flight until it lands.</summary>
        public bool IsFlying => _isFlight && _motion != null && _motion.IsActive();

        /// <summary>The tube the current or last flight left.</summary>
        public int FlightSource { get; private set; } = -1;

        /// <summary>Why the current or last flight flew.</summary>
        public FlightKind FlightKind { get; private set; }

        /// <summary>True when the current or last flight lands without a sound (undo, shuffle).</summary>
        public bool LandsSilently => FlightKind != FlightKind.Move;

        /// <summary>
        /// Gives a pooled ball its config and the board's flight callbacks, once, when it is created.
        /// </summary>
        /// <param name="departed">A flight has left its source tube (end of the rise).</param>
        /// <param name="landed">A flight has reached its slot.</param>
        public void Initialize(AnimationConfig animation, Action<BallView> departed, Action<BallView> landed)
        {
            _animation = animation;
            _departed = departed;
            _landed = landed;
        }

        /// <summary>Shows <paramref name="ball"/> as the board holds it: colour or hidden, with or without its key.</summary>
        public void Show(Ball ball, ColorPalette palette, LockPalette locks, float diameter)
        {
            StopMotion();
            _palette = palette;
            _diameter = diameter;

            _hiddenMark.sprite = palette.HiddenMarkSprite;
            _hiddenMark.transform.localScale = Vector3.one * (palette.HiddenMarkSize / SpriteWidth(palette.HiddenMarkSprite));

            // The key is the lock's own art (no tint), held in the monster's hands.
            Sprite key = ball.KeyLockId != Ball.NoKey ? locks.GetKey(ball.KeyLockId) : null;
            _keyMark.sprite = key;
            _keyMark.transform.localScale = Vector3.one * (palette.KeyMarkSize / SpriteWidth(key));
            _keyMark.transform.localPosition = palette.KeyMarkOffset;
            _shade = 1f;
            _hiddenMark.color = Color.white;
            _keyMark.color = Color.white;
            _keyLockId = ball.KeyLockId;

            _restingExpression = BallExpression.Open;
            _expression = BallExpression.Open;
            ShowColour(ball.ColorId, ball.IsHidden, palette);
            transform.localScale = Vector3.one * diameter;
            StopBlinking();
        }

        /// <summary>
        /// Turns a hidden ball face up. A key it carries stays, now visible, until <see cref="RemoveKey"/>.
        /// Animated, it turns over like a card (docs/animation-spec.md, 3.1): its width goes to nothing and
        /// back, and the colour (and key) shows half-way.
        /// </summary>
        public void Reveal(int colorId, ColorPalette palette, bool animate)
        {
            FinishReveal();
            if (!animate)
            {
                ShowColour(colorId, false, palette);
                return;
            }

            _palette = palette;
            _flipColour = colorId;
            _flipTurned = false;
            _flipTween = DOVirtual.Float(0f, 1f, _animation.RevealFlipDuration, _flipStep)
                .SetEase(Ease.Linear).OnComplete(_endFlip).OnBoardClock();
        }

        /// <summary>True while the ball turns face up.</summary>
        public bool IsFlipping => _flipTween != null && _flipTween.IsActive();

        /// <summary>Ends a reveal flip at once, face up.</summary>
        public void FinishReveal() => BoardTweens.CompleteIfActive(_flipTween);

        private void FlipStep(float u)
        {
            if (!_flipTurned && u >= 0.5f)
            {
                Turn();
            }

            _face.localScale = new Vector3(Mathf.Abs(Mathf.Cos(Mathf.PI * u)), 1f, 1f);
        }

        private void EndFlip()
        {
            _flipTween = null;
            Turn();
            _face.localScale = Vector3.one;
        }

        // Shows the colour, in whatever face the ball holds now (it may have started laughing meanwhile).
        private void Turn()
        {
            if (_flipTurned)
            {
                return;
            }

            _flipTurned = true;
            ShowColour(_flipColour, false, _palette);
        }

        /// <summary>
        /// The single entry point for the ball's resting face (open, or laughing in a completed tube).
        /// A hidden ball keeps its hidden look and shows the expression once revealed.
        /// </summary>
        public void SetExpression(BallExpression expression, ColorPalette palette)
        {
            BoardTweens.KillIfActive(_faceTween);
            _restingExpression = expression;
            ApplyExpression(expression, palette);
        }

        /// <summary>
        /// Shows <paramref name="expression"/> for <paramref name="seconds"/> of board time (e.g. shock on an
        /// invalid tap), then goes back to the resting face.
        /// </summary>
        public void SetExpression(BallExpression expression, ColorPalette palette, float seconds)
        {
            BoardTweens.KillIfActive(_faceTween);
            ApplyExpression(expression, palette);
            _faceTween = DOVirtual.DelayedCall(seconds, _restoreFace, false).OnBoardClock();
        }

        /// <summary>
        /// Darkens the ball by <paramref name="shade"/> (1 = as drawn), e.g. in a tube the Shuffle
        /// booster cannot pick. Kept until the next <see cref="Show"/> or <see cref="SetShade"/>.
        /// </summary>
        public void SetShade(float shade)
        {
            _shade = shade;
            _body.color = Shaded(_bodyTint);
            _hiddenMark.color = Shaded(Color.white);
            _keyMark.color = Shaded(Color.white);
        }

        private Color Shaded(Color tint) => new Color(tint.r * _shade, tint.g * _shade, tint.b * _shade, tint.a);

        /// <summary>The ball's key has been found; its mark goes.</summary>
        public void RemoveKey()
        {
            _keyLockId = Ball.NoKey;
            UpdateKeyMark();
        }

        /// <summary>
        /// The ball's key leaves its hands for the lock box (docs/animation-spec.md, 3.3): its mark goes,
        /// and where it was drawn is given back so the flying key can start there.
        /// </summary>
        /// <param name="position">The key mark's centre, world space.</param>
        /// <param name="width">Its drawn width, world space.</param>
        /// <returns>False when no key was drawn (the mark still goes).</returns>
        public bool TryTakeKey(out Vector3 position, out float width)
        {
            FinishReveal();
            bool drawn = _keyMark.enabled && gameObject.activeInHierarchy;
            Bounds bounds = _keyMark.bounds;
            position = bounds.center;
            width = bounds.size.x;
            RemoveKey();
            return drawn;
        }

        /// <summary>Records which tube and slot this ball now belongs to.</summary>
        public void AssignSlot(int tubeIndex, int slot)
        {
            TubeIndex = tubeIndex;
            Slot = slot;
        }

        /// <summary>
        /// The mouth of the tube holding the ball: above <paramref name="clearHeight"/> (its centre's
        /// height, in the parent's space, once its bottom clears the rim) the ball draws above every tube
        /// at <paramref name="raisedSortingOrder"/> on <paramref name="raisedSortingLayer"/>; below it,
        /// inside the tube in slot order.
        /// </summary>
        public void SetMouth(float clearHeight, int raisedSortingLayer, int raisedSortingOrder)
        {
            _clearHeight = clearHeight;
            _raisedSortingLayer = raisedSortingLayer;
            _raisedSortingOrder = raisedSortingOrder;
            UpdateDepth(force: true);
        }

        /// <summary>Moves the ball under <paramref name="parent"/> without moving it on screen (a flight starts where it is).</summary>
        public void SetParentKeepingPlace(Transform parent)
        {
            transform.SetParent(parent, true);
            _position = transform.localPosition - new Vector3(0f, _hop + _bump + _bob, 0f);
        }

        /// <summary>Puts the ball at <paramref name="localPosition"/> at once, in its parent's space, ending any motion.</summary>
        public void MoveTo(Vector3 localPosition)
        {
            StopMotion();
            _position = localPosition;
            Apply();
        }

        /// <summary>Moves the ball to <paramref name="localPosition"/> over <paramref name="duration"/> seconds (lift, lower).</summary>
        public void MoveTo(Vector3 localPosition, float duration, Ease ease)
        {
            FinishMotion();
            _isFlight = false;
            _motion = DOTween.To(_getPosition, _setPosition, localPosition, duration).SetEase(ease).OnBoardClock();
        }

        /// <summary>
        /// Flies the ball along <paramref name="flight"/>: rise, arc and drop, stretching along its
        /// velocity, with a trail through the arc and drop. Lands with a squash and a hop. Any motion
        /// still running finishes first.
        /// </summary>
        public void MoveTo(in BallFlight flight)
        {
            FinishMotion();
            FlightSource = flight.SourceTube;
            FlightKind = flight.Kind;
            _isFlight = true;
            _hasDeparted = false;
            _hasLanded = false;
            _arcFrom = flight.RisePoint;
            _arcTo = flight.ArcEnd;
            _arcHeight = flight.ArcHeight;
            _screenWidth = Mathf.Max(flight.ScreenWidth, 0.001f);
            _clearHeight = flight.SourceClearHeight;
            _targetClearHeight = flight.TargetClearHeight;
            UpdateDepth(force: true);
            _lastPosition = _position;
            _lastElapsed = 0f;

            _motion = DOTween.Sequence()
                .AppendInterval(flight.Delay)
                .Append(DOTween.To(_getPosition, _setPosition, flight.RisePoint, _animation.RiseDuration)
                    .SetEase(_animation.RiseEase))
                .AppendCallback(_onDeparted)
                .Append(DOVirtual.Float(0f, 1f, flight.ArcDuration, _arcStep).SetEase(_animation.ArcEase))
                .AppendCallback(_onArcEnded)
                .Append(DOTween.To(_getPosition, _setPosition, flight.Slot, _animation.DropDuration)
                    .SetEase(_animation.DropEase))
                .OnUpdate(_onFlightUpdate)
                .OnComplete(_onLanded)
                .OnBoardClock();
            _motion.timeScale = Mathf.Max(flight.Speed, 0.01f);
        }

        /// <summary>Starts or stops the held ball's bob.</summary>
        public void SetBobbing(bool bobbing)
        {
            BoardTweens.KillIfActive(_bobTween);
            _bob = 0f;
            Apply();
            if (bobbing && _animation.LiftBob > 0f)
            {
                _bobTween = DOVirtual.Float(0f, 1f, _animation.LiftBobPeriod, _bobStep)
                    .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).OnBoardClock();
            }
        }

        /// <summary>The hop bounce (landing, tube-complete dance), after <paramref name="delay"/> seconds.</summary>
        public void Hop(float delay)
        {
            BoardTweens.KillIfActive(_hopTween);
            float duration = _animation.HopBounceDuration;
            _hopTween = DOVirtual.Float(0f, duration, duration, _hopStep)
                .SetEase(Ease.Linear).SetDelay(delay).OnComplete(ClearHop).OnBoardClock();
        }

        /// <summary>
        /// A small damped tilt to and fro, after <paramref name="delay"/> seconds: a ball that keeps its
        /// slot in a shuffle (docs/animation-spec.md, 4).
        /// </summary>
        public void Wiggle(float delay)
        {
            BoardTweens.KillIfActive(_wiggleTween);
            float duration = _animation.ShuffleWiggleDuration;
            _wiggleTween = DOVirtual.Float(0f, duration, duration, _wiggleStep)
                .SetEase(Ease.Linear).SetDelay(delay).OnComplete(_endWiggle).OnBoardClock();
        }

        /// <summary>A small bump up and back, when a ball lands above this one, after <paramref name="delay"/> seconds.</summary>
        public void Bump(float delay)
        {
            BoardTweens.KillIfActive(_bumpTween);
            float duration = _animation.BallBumpDuration;
            _bumpTween = DOVirtual.Float(0f, duration, duration, _bumpStep)
                .SetEase(Ease.Linear).SetDelay(delay).OnComplete(ClearBump).OnBoardClock();
        }

        /// <summary>
        /// Ends every running motion at once, where it would have ended: a flight lands (its callbacks
        /// fire), bounces settle, the bob stops.
        /// </summary>
        public void FinishMotion()
        {
            BoardTweens.CompleteIfActive(_motion);
            BoardTweens.CompleteIfActive(_squashTween);
            BoardTweens.CompleteIfActive(_hopTween);
            BoardTweens.CompleteIfActive(_bumpTween);
            BoardTweens.CompleteIfActive(_wiggleTween);
            FinishReveal();
            if (_bobTween != null && _bobTween.IsActive())
            {
                SetBobbing(false);
            }
        }

        /// <summary>Kills every motion without its callbacks and resets the offsets: the ball is going back to the pool.</summary>
        public void StopMotion()
        {
            // A reveal is information, not motion: it ends face up.
            FinishReveal();
            KillTweens();
            _hop = 0f;
            _bump = 0f;
            _bob = 0f;
            _isFlight = false;
            ResetShape();
            StopTrail();
            Apply();
        }

        /// <summary>Draws the ball above every tube or inside its own, when that changes (or always when forced).</summary>
        private void UpdateDepth(bool force = false)
        {
            bool raised = _position.y >= _clearHeight;
            if (raised == IsRaised && !force)
            {
                return;
            }

            IsRaised = raised;
            int slotOrder = Mathf.Max(Slot, 0) * _ordersPerBall;
            if (raised)
            {
                // Keep the parts' order: body, then the marks on top of it; the trail just behind.
                int baseOrder = _raisedSortingOrder -
                    Mathf.Min(_bodySortingOrder, Mathf.Min(_markSortingOrder, _keySortingOrder)) + slotOrder;
                Apply(_body, _raisedSortingLayer, baseOrder + _bodySortingOrder);
                Apply(_hiddenMark, _raisedSortingLayer, baseOrder + _markSortingOrder);
                Apply(_keyMark, _raisedSortingLayer, baseOrder + _keySortingOrder);
                Apply(_trail, _raisedSortingLayer, baseOrder + _bodySortingOrder - 1);
            }
            else
            {
                Apply(_body, _bodySortingLayer, _bodySortingOrder + slotOrder);
                Apply(_hiddenMark, _markSortingLayer, _markSortingOrder + slotOrder);
                Apply(_keyMark, _keySortingLayer, _keySortingOrder + slotOrder);
                Apply(_trail, _bodySortingLayer, _bodySortingOrder + slotOrder - 1);
            }
        }

        private void ShowColour(int colorId, bool hidden, ColorPalette palette)
        {
            _isHidden = hidden;
            _colorId = colorId;
            Sprite sprite = hidden ? palette.HiddenSprite : palette.GetSprite(colorId, _expression);
            _body.sprite = sprite;
            _bodyTint = hidden ? palette.HiddenTint : palette.GetSpriteTint(colorId);
            _body.color = Shaded(_bodyTint);
            _hiddenMark.enabled = hidden && _hiddenMark.sprite != null;
            float size = hidden ? palette.HiddenSpriteSize : palette.GetSpriteSize(colorId);
            _body.transform.localScale = Vector3.one * (size / SpriteWidth(sprite));
            UpdateKeyMark();
        }

        private void ApplyExpression(BallExpression expression, ColorPalette palette)
        {
            _expression = expression;
            if (!_isHidden)
            {
                _body.sprite = palette.GetSprite(_colorId, expression);
            }
        }

        private void RestoreFace() => ApplyExpression(_restingExpression, _palette);

        /// <summary>
        /// True when a blink would show: the ball is on screen, revealed, resting with its open face,
        /// holding no other face (shock) and not already blinking.
        /// </summary>
        public bool CanBlink =>
            isActiveAndEnabled && _palette != null && _blinkPhase == 0 && !IsFlipping && CanShowBlinkFrame;

        private bool CanShowBlinkFrame =>
            !_isHidden && _restingExpression == BallExpression.Open && (_faceTween == null || !_faceTween.IsActive());

        /// <summary>
        /// Blinks once (half-open, closed, half-open, then the resting face: docs/animation-spec.md, 6.1),
        /// if <see cref="CanBlink"/>. The board picks which ball blinks and when.
        /// </summary>
        /// <returns>True when the blink started.</returns>
        public bool TryBlink()
        {
            if (!CanBlink)
            {
                return false;
            }

            BlinkStep();
            return true;
        }

        /// <summary>Ends a blink where it is: the eyes open again if they were closing.</summary>
        private void StopBlinking()
        {
            BoardTweens.KillIfActive(_blinkTween);
            bool midBlink = _blinkPhase != 0;
            _blinkPhase = 0;
            if (midBlink && _palette != null && (_faceTween == null || !_faceTween.IsActive()))
            {
                RestoreFace();
            }
        }

        private void ScheduleBlinkStep(float delay) =>
            _blinkTween = DOVirtual.DelayedCall(delay, _blinkStep, false).OnBoardClock();

        /// <summary>
        /// One frame of the blink. A frame shows only while the ball may show it (a shock that starts
        /// mid-blink wins); the blink ends on the resting face.
        /// </summary>
        private void BlinkStep()
        {
            _blinkPhase = (_blinkPhase + 1) % 4;
            bool canShow = CanShowBlinkFrame;

            switch (_blinkPhase)
            {
                case 1:
                    ShowBlinkFrame(canShow, BallExpression.HalfOpen);
                    ScheduleBlinkStep(_animation.BlinkCloseDuration);
                    break;
                case 2:
                    ShowBlinkFrame(canShow, BallExpression.Closed);
                    ScheduleBlinkStep(_animation.BlinkClosedDuration);
                    break;
                case 3:
                    ShowBlinkFrame(canShow, BallExpression.HalfOpen);
                    ScheduleBlinkStep(_animation.BlinkOpenDuration);
                    break;
                default:
                    if (canShow)
                    {
                        RestoreFace();
                    }

                    break;
            }
        }

        private void ShowBlinkFrame(bool canShow, BallExpression frame)
        {
            if (canShow)
            {
                ApplyExpression(frame, _palette);
            }
        }

        /// <summary>
        /// The single entry point for the key mark: drawn while the ball is revealed and still carries
        /// its key, so a key on a hidden ball stays invisible. Snaps for now.
        /// </summary>
        private void UpdateKeyMark() =>
            _keyMark.enabled = _keyLockId != Ball.NoKey && !_isHidden && _keyMark.sprite != null;

        private void Apply()
        {
            transform.localPosition = _position + new Vector3(0f, _hop + _bump + _bob, 0f);
            UpdateDepth();
        }

        private void SetPosition(Vector3 position)
        {
            _position = position;
            Apply();
        }

        private void ArcStep(float k)
        {
            _position = Vector3.LerpUnclamped(_arcFrom, _arcTo, k) + new Vector3(0f, _arcHeight * 4f * k * (1f - k), 0f);
            Apply();
        }

        /// <summary>Stretches the ball along its velocity, measured over the flight's last step of board time.</summary>
        private void OnFlightUpdate()
        {
            float elapsed = _motion.Elapsed(false);
            float dt = elapsed - _lastElapsed;
            if (dt <= 1e-5f)
            {
                return;
            }

            Vector3 velocity = (_position - _lastPosition) / dt;
            _lastPosition = _position;
            _lastElapsed = elapsed;

            float speed = velocity.magnitude / _screenWidth;
            if (speed < _animation.StretchMinSpeed)
            {
                ResetShape();
                return;
            }

            float stretch = Mathf.Min(speed / _animation.StretchFullSpeed, _animation.StretchMax) * _animation.Squash;
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            _shape.localRotation = Quaternion.Euler(0f, 0f, angle);
            _shape.localScale = new Vector3(1f + stretch, 1f - stretch, 1f);
            _face.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void OnDeparted()
        {
            if (_hasDeparted)
            {
                return;
            }

            _hasDeparted = true;

            // Between the mouths the ball is above every tube, whatever the arc's height.
            _clearHeight = float.NegativeInfinity;
            UpdateDepth();
            StartTrail();
            _departed?.Invoke(this);
        }

        private void OnArcEnded()
        {
            _clearHeight = _targetClearHeight;
            UpdateDepth();
        }

        private void OnLanded()
        {
            if (_hasLanded)
            {
                return;
            }

            // A flight finished early passes its departure and the arc's end on the way.
            OnDeparted();
            OnArcEnded();
            _hasLanded = true;
            _isFlight = false;
            ResetShape();
            StopTrail();
            Squash();
            Hop(0f);
            _landed?.Invoke(this);
        }

        private void Squash()
        {
            BoardTweens.KillIfActive(_squashTween);
            float duration = _animation.LandSquashDuration;
            _squashTween = DOVirtual.Float(0f, duration, duration, _squashStep)
                .SetEase(Ease.Linear).OnComplete(ResetShape).OnBoardClock();
        }

        private void SquashStep(float t)
        {
            float amount = _animation.LandSquashAmount * _animation.Squash *
                BoardTweens.Wobble(t, _animation.LandSquashFrequency, _animation.LandSquashDamping);
            _shape.localScale = new Vector3(1f + amount, 1f - amount, 1f);
        }

        private void HopStep(float t)
        {
            _hop = _animation.HopBounceHeight * _diameter *
                BoardTweens.Hop(t, _animation.HopBounceDuration, _animation.HopBounceDamping);
            Apply();
        }

        private void BumpStep(float t)
        {
            _bump = _animation.BumpHeight * _diameter * BoardTweens.HalfSine(t, _animation.BallBumpDuration);
            Apply();
        }

        private void BobStep(float k)
        {
            _bob = _animation.LiftBob * _diameter * Mathf.Sin(2f * Mathf.PI * k);
            Apply();
        }

        private void WiggleStep(float t)
        {
            float angle = _animation.ShuffleWiggleAngle *
                BoardTweens.Shake(t, _animation.ShuffleWiggleFrequency, _animation.ShuffleWiggleDamping);
            _shape.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void EndWiggle() => _shape.localRotation = Quaternion.identity;

        private void ClearHop()
        {
            _hop = 0f;
            Apply();
        }

        private void ClearBump()
        {
            _bump = 0f;
            Apply();
        }

        private void ResetShape()
        {
            _shape.localRotation = Quaternion.identity;
            _shape.localScale = Vector3.one;
            _face.localRotation = Quaternion.identity;
        }

        private void StartTrail()
        {
            if (!_animation.Trail || _isHidden)
            {
                return;
            }

            Color colour = _palette.GetColor(_colorId);
            _trail.time = _animation.TrailTime;
            _trail.startColor = new Color(colour.r, colour.g, colour.b, _animation.TrailAlpha);
            _trail.endColor = new Color(colour.r, colour.g, colour.b, 0f);
            _trail.startWidth = _animation.TrailWidth * 0.5f * transform.lossyScale.x;
            _trail.endWidth = 0f;
            _trail.Clear();
            _trail.emitting = true;
        }

        private void StopTrail()
        {
            _trail.emitting = false;
            _trail.Clear();
        }

        private void KillTweens()
        {
            BoardTweens.KillIfActive(_motion);
            BoardTweens.KillIfActive(_squashTween);
            BoardTweens.KillIfActive(_hopTween);
            BoardTweens.KillIfActive(_bumpTween);
            BoardTweens.KillIfActive(_bobTween);
            BoardTweens.KillIfActive(_faceTween);
            BoardTweens.KillIfActive(_wiggleTween);
        }

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 1f;

        private static void Apply(Renderer renderer, int layer, int order)
        {
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
        }

        private void Awake()
        {
            if (_body == null || _hiddenMark == null || _keyMark == null || _shape == null || _face == null || _trail == null)
            {
                Debug.LogError($"{name}: body, hidden mark, key mark, shape, face and trail must be assigned.", this);
                return;
            }

            _bodySortingLayer = _body.sortingLayerID;
            _bodySortingOrder = _body.sortingOrder;
            _markSortingLayer = _hiddenMark.sortingLayerID;
            _markSortingOrder = _hiddenMark.sortingOrder;
            _keySortingLayer = _keyMark.sortingLayerID;
            _keySortingOrder = _keyMark.sortingOrder;
            _ordersPerBall = Mathf.Max(_bodySortingOrder, Mathf.Max(_markSortingOrder, _keySortingOrder)) -
                Mathf.Min(_bodySortingOrder, Mathf.Min(_markSortingOrder, _keySortingOrder)) + 1;

            _getPosition = () => _position;
            _setPosition = SetPosition;
            _arcStep = ArcStep;
            _hopStep = HopStep;
            _bumpStep = BumpStep;
            _squashStep = SquashStep;
            _bobStep = BobStep;
            _wiggleStep = WiggleStep;
            _endWiggle = EndWiggle;
            _flipStep = FlipStep;
            _endFlip = EndFlip;
            _onDeparted = OnDeparted;
            _onArcEnded = OnArcEnded;
            _onLanded = OnLanded;
            _onFlightUpdate = OnFlightUpdate;
            _restoreFace = RestoreFace;
            _blinkStep = BlinkStep;
            _trail.emitting = false;
        }

        // Blinking is not motion (a snap into a slot keeps it), but a ball pooled or hidden under a cover
        // stops mid-blink with its eyes open.
        private void OnDisable() => StopBlinking();

        private void OnDestroy()
        {
            KillTweens();
            BoardTweens.KillIfActive(_blinkTween);
        }
    }
}
