using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One tube on screen: the tube drawn behind the balls from the rim's back half, a stretched middle
    /// and a bottom part, the balls, then the clay (ice) of clay tubes, the cork of a completed tube and
    /// the rim's front half over them; an optional glass front and the glow of a completed tube. The
    /// transform's origin is the tube's base centre.
    /// </summary>
    /// <remarks>
    /// A ball inside the tube draws between the rim's halves; once it clears the mouth it draws above
    /// every tube (<see cref="BallView.SetMouth"/>), so a flying ball never passes behind a rim.
    /// <para>
    /// Covers are not part of the tube: a <see cref="CoverView"/> may span several tubes, and hides
    /// their balls through <see cref="SetCovered"/>.
    /// </para>
    /// <para>
    /// The layout sets the tube's home (<see cref="Place"/>); the bump and shake are offsets on top of
    /// it, so a re-layout mid-animation never leaves the tube out of place. The tube also counts the
    /// balls flying out of it and into it, so later beats of a move (the reveal, the completion, cover
    /// and key changes) wait for the balls rather than for a timer: <see cref="WhenDeparted"/>,
    /// <see cref="WhenArrived"/>. Touching the tube (<see cref="FinishPending"/>) plays them at once.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TubeView : MonoBehaviour
    {
        [Tooltip("The rim's back half, behind the balls.")]
        [FormerlySerializedAs("_top")]
        [SerializeField] private SpriteRenderer _rimBack;
        [Tooltip("The rim's front half, over the balls inside the tube and the cork.")]
        [SerializeField] private SpriteRenderer _rimFront;
        [SerializeField] private SpriteRenderer _middle;
        [FormerlySerializedAs("_back")]
        [SerializeField] private SpriteRenderer _bottom;
        [Tooltip("Optional glass front over the balls; its sorting layer is also where lifted balls draw.")]
        [SerializeField] private SpriteRenderer _front;
        [Tooltip("The cork on a completed tube.")]
        [SerializeField] private SpriteRenderer _cap;
        [Tooltip("Clay (ice) at the base of a clay tube, over the balls inside it.")]
        [SerializeField] private SpriteRenderer _clay;
        [Tooltip("Glow around a completed tube, in its colour, behind the tube.")]
        [FormerlySerializedAs("_flash")]
        [SerializeField] private SpriteRenderer _glow;
        [Tooltip("Parent for this tube's balls.")]
        [SerializeField] private Transform _ballRoot;
        [SerializeField] private BoxCollider2D _tapTarget;
        [Tooltip("Sorting order of a ball out of its tube's mouth (lifted or flying), on the glass front's layer: " +
            "above every tube's glass, clay, cork, rim front and covers.")]
        [SerializeField] private int _raisedSortingOrder = 10;

        private readonly List<BallView> _balls = new List<BallView>();
        private readonly List<Action> _afterDeparture = new List<Action>();
        private readonly List<Action> _afterArrival = new List<Action>();
        private readonly List<Tween> _beats = new List<Tween>();
        private readonly List<BallView> _reorderBuffer = new List<BallView>();
        private TubeVisualConfig _config;
        private AnimationConfig _animation;
        private int _liftedCount;
        private bool _isClay;
        private int _departures;
        private int _arrivals;
        private TubeHighlight _highlight;

        private Vector3 _home;
        private float _shakeX;
        private float _bumpY;
        private Color _glowColour;
        private Color _capColour = Color.white;

        private Tween _shakeTween;
        private Tween _bumpTween;
        private Tween _corkTween;
        private Tween _glowTween;
        private Tween _iceTween;

        private TweenCallback<float> _shakeStep;
        private TweenCallback<float> _bumpStep;
        private TweenCallback<float> _glowStep;
        private TweenCallback<float> _glowPulseStep;
        private TweenCallback<float> _iceCrackStep;
        private TweenCallback _clearShake;
        private TweenCallback _clearBump;
        private TweenCallback _startGlowPulse;

        /// <summary>Board index of the tube this view shows.</summary>
        public int TubeIndex { get; private set; }

        /// <summary>Balls currently parented to this tube, bottom first.</summary>
        public IReadOnlyList<BallView> Balls => _balls;

        /// <summary>Height of the tube body above its base, in board units.</summary>
        public float BodyHeight { get; private set; }

        /// <summary>Where the layout put the tube, without the bump and shake.</summary>
        public Vector3 HomePosition => _home;

        /// <summary>The space the balls live in.</summary>
        public Transform BallSpace => _ballRoot;

        /// <summary>
        /// The height in ball space above which a ball's centre is out of the tube: its bottom has cleared
        /// the mouth, so it draws above every tube.
        /// </summary>
        public float ClearHeight => BodyHeight + _config.BallSize * 0.5f;

        /// <summary>Where a lifted ball's centre sits, in ball space: above the mouth by the lift height.</summary>
        public Vector3 LiftPoint => new Vector3(0f, BodyHeight + _animation.LiftHeight * _config.BallDiameter, 0f);

        /// <summary>True while the tube's clay (ice) is drawn: a clay tube not completed yet, or cracking.</summary>
        public bool HasIce => _clay.enabled;

        /// <summary>The centre of the ice, in world space (where it bursts).</summary>
        public Vector3 IceCentre => _clay.enabled ? _clay.bounds.center : _ballRoot.position;

        /// <summary>True when no ball is flying out of or into the tube and no beat is waiting.</summary>
        public bool IsSettled => _departures == 0 && _arrivals == 0 && !HasActiveBeat();

        /// <summary>
        /// Sizes the tube for <paramref name="capacity"/> balls. The tap target also covers the lift
        /// area above the rim, so a tap on a lifted ball still hits its tube.
        /// </summary>
        /// <param name="isClay">Shows the clay (ice) until the tube completes (<see cref="SetClayShown"/>).</param>
        public void Setup(int tubeIndex, int capacity, bool isClay, TubeVisualConfig config, AnimationConfig animation)
        {
            StopMotion();
            TubeIndex = tubeIndex;
            _config = config;
            _animation = animation;
            _liftedCount = 0;
            _isClay = isClay;

            float height = config.TubeHeight(capacity);
            BodyHeight = height;
            float scale = config.ArtScale;

            // The bottom stands on the base and the rim hangs from the mouth, both at the art's size;
            // the middle stretches over the gap between them. The rim's halves share one rectangle.
            float middleBottom = HeightAbovePivot(config.BottomSprite) * scale;
            float middleTop = height - HeightBelowPivot(config.RimBackSprite) * scale;
            float middleScale = Mathf.Max(0f, middleTop - middleBottom) /
                Mathf.Max(SpriteHeight(config.MiddleSprite), 0.001f);
            ApplyPart(_bottom, config.BottomSprite, config.TubeTint, 0f, new Vector3(scale, scale, 1f));
            ApplyPart(_middle, config.MiddleSprite, config.TubeTint, middleBottom, new Vector3(scale, middleScale, 1f));
            ApplyPart(_rimBack, config.RimBackSprite, config.TubeTint, height, new Vector3(scale, scale, 1f));
            ApplyPart(_rimFront, config.RimFrontSprite, config.TubeTint, height, new Vector3(scale, scale, 1f));

            ApplyOutline(_front, config.FrontSprite, config.FrontTint, scale, height);

            // The cork's sprite is picked by colour on completion; like the rim it hangs from the mouth.
            ApplyPart(_cap, config.CapSprite, config.CapTint, height, new Vector3(scale, scale, 1f));
            _capColour = config.CapTint;
            _cap.enabled = false;

            // Clay is sized from the tube's drawn width (its rim), so it keeps its place next to the tubes.
            float tubeDrawnWidth = SpriteWidth(config.RimBackSprite) * scale;
            float clayScale = config.ClayScale * tubeDrawnWidth / Mathf.Max(SpriteWidth(config.ClaySprite), 0.001f);
            ApplyPart(_clay, config.ClaySprite, config.ClayTint, 0f, new Vector3(clayScale, clayScale, 1f));
            SetClayShown(isClay);

            // The glow is the tube's size plus its padding all round, sliced so its soft edge keeps its width.
            float padding = config.GlowPadding;
            ApplySprite(_glow, config.GlowSprite, Color.clear,
                new Vector2(config.TubeWidth + 2f * padding, height + 2f * padding), new Vector3(0f, -padding, 0f));
            _glow.enabled = false;

            // A pooled view may come back from a covered or highlighted tube.
            SetCovered(false, false);
            _highlight = TubeHighlight.None;

            float tapHeight = height + _animation.LiftHeight * config.BallDiameter + config.BallDiameter * 0.5f;
            _tapTarget.size = new Vector2(config.TubeWidth + config.HorizontalSpacing, tapHeight);
            _tapTarget.offset = new Vector2(0f, tapHeight * 0.5f);
        }

        /// <summary>Puts the tube at <paramref name="home"/>, in board space; bump and shake stay on top of it.</summary>
        public void Place(Vector3 home)
        {
            _home = home;
            ApplyOffsets();
        }

        /// <summary>
        /// Puts <paramref name="ball"/> in the next free slot at once (building the board). Any lifted
        /// ball drops back first, so a lifted ball is always the top of the tube.
        /// </summary>
        public void AddBall(BallView ball)
        {
            Lower();
            int slot = _balls.Count;
            ball.transform.SetParent(_ballRoot, false);
            ball.AssignSlot(TubeIndex, slot);
            ball.SetMouth(ClearHeight, _front.sortingLayerID, _raisedSortingOrder);
            ball.MoveTo(SlotPosition(slot));
            _balls.Add(ball);
        }

        /// <summary>
        /// Takes <paramref name="ball"/> into the next free slot, keeping it where it is on screen; the
        /// caller flies it in (the flight says where the ball draws). Counts it as arriving until
        /// <see cref="NotifyLanded"/>.
        /// </summary>
        /// <returns>The slot's position in ball space.</returns>
        public Vector3 ReceiveBall(BallView ball)
        {
            Lower();
            int slot = _balls.Count;
            ball.SetParentKeepingPlace(_ballRoot);
            ball.AssignSlot(TubeIndex, slot);
            _balls.Add(ball);
            _arrivals++;
            return SlotPosition(slot);
        }

        /// <summary>
        /// Removes the top <paramref name="count"/> balls and appends them to <paramref name="into"/>,
        /// topmost first, to fly elsewhere; each counts as departing until <see cref="NotifyDeparted"/>.
        /// A ball still landing here lands first.
        /// </summary>
        public void TakeTop(int count, List<BallView> into)
        {
            for (int i = 0; i < count; i++)
            {
                _balls[_balls.Count - 1 - i].FinishMotion();
            }

            for (int i = 0; i < count; i++)
            {
                int top = _balls.Count - 1;
                into.Add(_balls[top]);
                _balls.RemoveAt(top);
            }

            _departures += count;
            _liftedCount = Mathf.Max(0, _liftedCount - count);
        }

        /// <summary>
        /// Lifts the top ball above the mouth (the rest of its run follows during the move) and bobs
        /// it. Anything the tube was still playing finishes first.
        /// </summary>
        /// <returns>False when the tube is empty.</returns>
        public bool Lift()
        {
            Lower();
            FinishPending();
            if (_balls.Count == 0)
            {
                return false;
            }

            _liftedCount = 1;
            BallView top = _balls[_balls.Count - 1];
            top.MoveTo(LiftPoint, _animation.LiftDuration, _animation.LiftEase);
            top.SetBobbing(true);
            return true;
        }

        /// <summary>Drops a lifted ball back into its slot.</summary>
        /// <returns>True when a ball was lifted (it now drops back).</returns>
        public bool Lower()
        {
            if (_liftedCount == 0 || _balls.Count == 0)
            {
                _liftedCount = 0;
                return false;
            }

            _liftedCount = 0;
            BallView top = _balls[_balls.Count - 1];
            int slot = _balls.Count - 1;
            top.SetBobbing(false);
            top.MoveTo(SlotPosition(slot), _animation.LiftDuration, _animation.LiftEase);
            return true;
        }

        /// <summary>
        /// Puts the balls in the order a shuffle gave them: the ball now in slot <c>s</c> is the one that
        /// was in <c>fromSlots[s]</c>. Balls stay where they are on screen; the caller moves them. Each ball
        /// whose slot changed counts as arriving until <see cref="NotifyLanded"/>. Anything the tube was
        /// still playing finishes first.
        /// </summary>
        public void Reorder(IReadOnlyList<int> fromSlots)
        {
            Lower();
            FinishPending();
            _reorderBuffer.Clear();
            _reorderBuffer.AddRange(_balls);
            for (int slot = 0; slot < _balls.Count; slot++)
            {
                BallView ball = _reorderBuffer[fromSlots[slot]];
                _balls[slot] = ball;
                ball.AssignSlot(TubeIndex, slot);
                if (fromSlots[slot] != slot)
                {
                    _arrivals++;
                }
            }

            _reorderBuffer.Clear();
        }

        /// <summary>A ball flying out of this tube has left it (end of its rise).</summary>
        public void NotifyDeparted()
        {
            if (_departures == 0)
            {
                return;
            }

            _departures--;
            if (_departures == 0)
            {
                Run(_afterDeparture);
            }
        }

        /// <summary>
        /// <paramref name="ball"/> has landed in its slot here: the balls below bump in a ripple and the
        /// tube bumps.
        /// </summary>
        public void NotifyLanded(BallView ball)
        {
            for (int slot = 0; slot < ball.Slot && slot < _balls.Count; slot++)
            {
                _balls[slot].Bump((ball.Slot - slot) * _animation.BumpRippleDelay);
            }

            Bump();
            if (_arrivals == 0)
            {
                return;
            }

            _arrivals--;
            if (_arrivals == 0)
            {
                Run(_afterArrival);
            }
        }

        /// <summary>Runs <paramref name="action"/> once every ball flying out of this tube has left it (now, if none is).</summary>
        public void WhenDeparted(Action action) => RunOrQueue(_departures, _afterDeparture, action);

        /// <summary>Runs <paramref name="action"/> once every ball flying into this tube has landed (now, if none is).</summary>
        public void WhenArrived(Action action) => RunOrQueue(_arrivals, _afterArrival, action);

        /// <summary>
        /// Keeps <paramref name="beat"/> (a delayed snap or callback on the board clock) with this tube,
        /// so <see cref="FinishPending"/> plays it at once and a rebuild drops it.
        /// </summary>
        public void AddBeat(Tween beat)
        {
            for (int i = _beats.Count - 1; i >= 0; i--)
            {
                if (!_beats[i].IsActive())
                {
                    _beats.RemoveAt(i);
                }
            }

            _beats.Add(beat);
        }

        /// <summary>
        /// Plays at once everything the tube is still waiting for: its balls land, the beats waiting on
        /// balls that left it run, its pending beats fire. Called before the player acts on the tube.
        /// </summary>
        public void FinishPending()
        {
            for (int i = 0; i < _balls.Count; i++)
            {
                _balls[i].FinishMotion();
            }

            _departures = 0;
            Run(_afterDeparture);
            IsFinishing = true;
            try
            {
                while (_beats.Count > 0)
                {
                    Tween beat = _beats[0];
                    _beats.RemoveAt(0);
                    BoardTweens.CompleteIfActive(beat);
                }
            }
            finally
            {
                IsFinishing = false;
            }
        }

        /// <summary>
        /// True while <see cref="FinishPending"/> plays the tube's beats at once: their steps happen, but
        /// a sound they would have played in time is dropped.
        /// </summary>
        public bool IsFinishing { get; private set; }

        /// <summary>The invalid-move shake (docs/animation-spec.md, 2.5).</summary>
        public void Shake()
        {
            BoardTweens.KillIfActive(_shakeTween);
            float duration = _animation.InvalidShakeDuration;
            _shakeTween = DOVirtual.Float(0f, duration, duration, _shakeStep)
                .SetEase(Ease.Linear).OnComplete(_clearShake).OnBoardClock();
        }

        /// <summary>The tube rises a little and settles, when a ball lands in it.</summary>
        public void Bump()
        {
            BoardTweens.KillIfActive(_bumpTween);
            float duration = _animation.TubeBumpDuration;
            _bumpTween = DOVirtual.Float(0f, duration, duration, _bumpStep)
                .SetEase(Ease.Linear).OnComplete(_clearBump).OnBoardClock();
        }

        /// <summary>
        /// Shows the tube completed in <paramref name="colorId"/>: the colour's cork, glow, laughing
        /// balls, and the clay gone. Animated (docs/animation-spec.md, 2.6), the cork drops, the glow
        /// fades to its idle pulse and the balls hop in turn, and the ice starts to crack: it stays until
        /// <see cref="BreakIce"/> (3.4). Otherwise all of it shows at rest.
        /// </summary>
        public void ShowCompleted(int colorId, ColorPalette palette, bool animate)
        {
            SetBallExpression(BallExpression.Laugh, palette);
            if (animate && HasIce)
            {
                StartIceCrack();
            }
            else
            {
                SetClayShown(false);
            }

            // A colour's own cork is drawn as is; the plain cork is tinted in the colour.
            Color colour = palette.GetColor(colorId);
            Sprite cork = palette.GetCork(colorId);
            Color capTint = cork != null ? Color.white : _config.CapTint * colour;
            _cap.sprite = cork != null ? cork : _config.CapSprite;
            _capColour = new Color(capTint.r, capTint.g, capTint.b, _config.CapTint.a);
            _cap.color = _capColour;
            _cap.enabled = _cap.sprite != null;
            Vector3 seated = new Vector3(0f, BodyHeight, 0f);
            BoardTweens.KillIfActive(_corkTween);
            _cap.transform.localPosition = seated;

            _glowColour = _config.GlowTint * colour;
            _glow.enabled = _animation.Glow && _glow.sprite != null;
            BoardTweens.KillIfActive(_glowTween);

            if (!animate)
            {
                StartGlowPulse();
                return;
            }

            _cap.transform.localPosition = seated + new Vector3(0f, _animation.CorkDropHeight * _config.BallDiameter, 0f);
            _corkTween = _cap.transform.DOLocalMove(seated, _animation.CorkDropDuration)
                .SetEase(_animation.CorkEase).OnBoardClock();

            SetGlowAlpha(1f);
            _glowTween = DOVirtual.Float(1f, _animation.GlowIdleAlpha, _animation.GlowFadeDuration, _glowStep)
                .SetEase(Ease.Linear).OnComplete(_startGlowPulse).OnBoardClock();

            if (_animation.Dance)
            {
                for (int slot = 0; slot < _balls.Count; slot++)
                {
                    _balls[slot].Hop(slot * _animation.DanceStagger);
                }
            }
        }

        /// <summary>
        /// Draws the tube for the Shuffle booster's pick (<see cref="TubeHighlight"/>): an eligible tube
        /// gets the glow in the highlight tint, a dimmed one is darkened with its balls. A snap. Never
        /// asked of a completed tube with <see cref="TubeHighlight.Eligible"/>, so its own glow is only
        /// ever darkened, never replaced.
        /// </summary>
        public void SetHighlight(TubeHighlight highlight)
        {
            if (highlight == _highlight)
            {
                return;
            }

            if (_highlight == TubeHighlight.Eligible)
            {
                _glow.enabled = false;
            }

            _highlight = highlight;
            float shade = highlight == TubeHighlight.Dimmed ? _config.ShuffleDimShade : 1f;
            Shade(_bottom, _config.TubeTint, shade);
            Shade(_middle, _config.TubeTint, shade);
            Shade(_rimBack, _config.TubeTint, shade);
            Shade(_rimFront, _config.TubeTint, shade);
            Shade(_front, _config.FrontTint, shade);
            Shade(_clay, _config.ClayTint, shade);
            Shade(_cap, _capColour, shade);

            foreach (BallView ball in _balls)
            {
                ball.SetShade(shade);
            }

            if (highlight == TubeHighlight.Eligible)
            {
                _glow.color = _config.ShuffleHighlightTint;
                _glow.enabled = _glow.sprite != null;
            }
        }

        /// <summary>
        /// Hides the balls of a covered tube, except the bottom one when <paramref name="showBottomBall"/>
        /// (a veil leaves it showing, hidden "?" or key mark and all), or shows them all again; driven by
        /// <see cref="CoverView.SetOpen"/>. A covered tube's balls never move, so this holds until the
        /// cover opens.
        /// </summary>
        public void SetCovered(bool covered, bool showBottomBall)
        {
            for (int slot = 0; slot < _balls.Count; slot++)
            {
                _balls[slot].gameObject.SetActive(!covered || (showBottomBall && slot == 0));
            }
        }

        /// <summary>
        /// The single entry point for the clay (ice): shown on a clay tube until it completes. Snaps for
        /// now; its melt comes with the animations.
        /// </summary>
        public void SetClayShown(bool shown) => _clay.enabled = shown && _isClay && _clay.sprite != null;

        /// <summary>The cracked ice bursts: gone at once (the shards are VFX, played by the caller).</summary>
        public void BreakIce()
        {
            BoardTweens.KillIfActive(_iceTween);
            ResetIce();
            SetClayShown(false);
        }

        // Shivers and whitens until it breaks (the crack art is missing: docs/art-inventory.md, section 10).
        private void StartIceCrack()
        {
            BoardTweens.KillIfActive(_iceTween);
            float duration = Mathf.Max(_animation.IceCrackTime, 0.001f);
            _iceTween = DOVirtual.Float(0f, duration, duration, _iceCrackStep).SetEase(Ease.Linear).OnBoardClock();
        }

        private void IceCrackStep(float t)
        {
            float shiver = _animation.IceShiverAmount * _config.BallDiameter *
                Mathf.Sin(_animation.IceShiverFrequency * t);
            _clay.transform.localPosition = new Vector3(shiver, 0f, 0f);
            float k = _animation.IceCrackBrighten * Mathf.Clamp01(t / Mathf.Max(_animation.IceCrackTime, 0.001f));
            Color tint = _config.ClayTint;
            _clay.color = new Color(
                Mathf.Lerp(tint.r, 1f, k), Mathf.Lerp(tint.g, 1f, k), Mathf.Lerp(tint.b, 1f, k), tint.a);
        }

        private void ResetIce()
        {
            if (_config == null)
            {
                return;
            }

            _clay.transform.localPosition = Vector3.zero;
            _clay.color = _config.ClayTint;
        }

        /// <summary>Hands every ball back to the caller (for pooling) and empties the tube.</summary>
        public void TakeAllBalls(List<BallView> into)
        {
            _liftedCount = 0;
            into.AddRange(_balls);
            _balls.Clear();
        }

        /// <summary>Gives every ball in the tube <paramref name="expression"/> as its resting face.</summary>
        public void SetBallExpression(BallExpression expression, ColorPalette palette)
        {
            foreach (BallView ball in _balls)
            {
                ball.SetExpression(expression, palette);
            }
        }

        /// <summary>Gives every revealed ball <paramref name="expression"/> for <paramref name="seconds"/> (shock).</summary>
        public void SetBallExpression(BallExpression expression, ColorPalette palette, float seconds)
        {
            foreach (BallView ball in _balls)
            {
                ball.SetExpression(expression, palette, seconds);
            }
        }

        /// <summary>Kills every animation and forgets every waiting beat: the tube is rebuilt or pooled.</summary>
        public void StopMotion()
        {
            KillTweens();
            _beats.Clear();
            _afterDeparture.Clear();
            _afterArrival.Clear();
            _departures = 0;
            _arrivals = 0;
            _shakeX = 0f;
            _bumpY = 0f;
            ApplyOffsets();
        }

        /// <summary>A ball's position in <paramref name="slot"/>, in ball space.</summary>
        public Vector3 SlotPosition(int slot) => new Vector3(0f, _config.BallY(slot), 0f);

        private bool HasActiveBeat()
        {
            foreach (Tween beat in _beats)
            {
                if (beat.IsActive())
                {
                    return true;
                }
            }

            return false;
        }

        private static void RunOrQueue(int waiting, List<Action> queue, Action action)
        {
            if (waiting == 0)
            {
                action();
            }
            else
            {
                queue.Add(action);
            }
        }

        // In order; an action may queue more.
        private static void Run(List<Action> actions)
        {
            while (actions.Count > 0)
            {
                Action action = actions[0];
                actions.RemoveAt(0);
                action();
            }
        }

        private void ApplyOffsets() => transform.localPosition = _home + new Vector3(_shakeX, _bumpY, 0f);

        private void ShakeStep(float t)
        {
            _shakeX = _animation.InvalidShakeAmount * _config.BallDiameter *
                BoardTweens.Shake(t, _animation.InvalidShakeFrequency, _animation.InvalidShakeDamping);
            ApplyOffsets();
        }

        private void BumpStep(float t)
        {
            _bumpY = _animation.BumpHeight * _config.BallDiameter * BoardTweens.HalfSine(t, _animation.TubeBumpDuration);
            ApplyOffsets();
        }

        private void ClearShake()
        {
            _shakeX = 0f;
            ApplyOffsets();
        }

        private void ClearBump()
        {
            _bumpY = 0f;
            ApplyOffsets();
        }

        private void StartGlowPulse()
        {
            BoardTweens.KillIfActive(_glowTween);
            SetGlowAlpha(_animation.GlowIdleAlpha);
            if (_animation.GlowPulse > 0f)
            {
                _glowTween = DOVirtual.Float(0f, 1f, _animation.GlowPulsePeriod, _glowPulseStep)
                    .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).OnBoardClock();
            }
        }

        private void GlowPulseStep(float k) =>
            SetGlowAlpha(_animation.GlowIdleAlpha + _animation.GlowPulse * Mathf.Sin(2f * Mathf.PI * k));

        private void SetGlowAlpha(float alpha)
        {
            _glow.color = new Color(_glowColour.r, _glowColour.g, _glowColour.b, _glowColour.a * alpha);
        }

        private void KillTweens()
        {
            if (_iceTween != null && _iceTween.IsActive())
            {
                _iceTween.Kill();
                ResetIce();
            }

            BoardTweens.KillIfActive(_shakeTween);
            BoardTweens.KillIfActive(_bumpTween);
            BoardTweens.KillIfActive(_corkTween);
            BoardTweens.KillIfActive(_glowTween);
            foreach (Tween beat in _beats)
            {
                BoardTweens.KillIfActive(beat);
            }
        }

        private static void Shade(SpriteRenderer renderer, Color tint, float shade) =>
            renderer.color = new Color(tint.r * shade, tint.g * shade, tint.b * shade, tint.a);

        /// <summary>A piece drawn as is, scaled and placed by its pivot on the tube's centre line.</summary>
        private static void ApplyPart(SpriteRenderer renderer, Sprite sprite, Color tint, float y, Vector3 scale)
        {
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.transform.localPosition = new Vector3(0f, y, 0f);
            renderer.transform.localScale = scale;
            renderer.enabled = sprite != null;
        }

        /// <summary>
        /// A whole-tube sprite pivoted at the base, 9-sliced so its rim and bottom keep the art's size
        /// while it stretches to <paramref name="height"/>.
        /// </summary>
        private static void ApplyOutline(SpriteRenderer renderer, Sprite sprite, Color tint, float scale, float height)
        {
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.enabled = sprite != null;
            if (sprite != null)
            {
                renderer.size = new Vector2(SpriteWidth(sprite), height / scale);
            }
        }

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 0f;

        private static float SpriteHeight(Sprite sprite) => sprite != null ? sprite.bounds.size.y : 0f;

        private static float HeightAbovePivot(Sprite sprite) =>
            sprite != null ? (sprite.rect.height - sprite.pivot.y) / sprite.pixelsPerUnit : 0f;

        private static float HeightBelowPivot(Sprite sprite) =>
            sprite != null ? sprite.pivot.y / sprite.pixelsPerUnit : 0f;

        private static void ApplySprite(
            SpriteRenderer renderer, Sprite sprite, Color tint, Vector2 size, Vector3 localPosition)
        {
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.transform.localPosition = localPosition;
        }

        private void Awake()
        {
            if (_rimBack == null || _rimFront == null || _middle == null || _bottom == null || _front == null ||
                _cap == null || _clay == null || _glow == null || _ballRoot == null || _tapTarget == null)
            {
                Debug.LogError(
                    $"{name}: rim back and front, middle, bottom, front, cap, clay, glow, ball root and tap target " +
                    "must be assigned.", this);
            }

            _shakeStep = ShakeStep;
            _bumpStep = BumpStep;
            _glowStep = SetGlowAlpha;
            _glowPulseStep = GlowPulseStep;
            _iceCrackStep = IceCrackStep;
            _clearShake = ClearShake;
            _clearBump = ClearBump;
            _startGlowPulse = StartGlowPulse;
        }

        private void OnDestroy() => KillTweens();
    }
}
