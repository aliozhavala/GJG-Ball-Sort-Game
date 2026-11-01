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
    /// Every change of a cover's state goes through <see cref="SetOpen"/>, which also hides or shows
    /// the covered tubes' balls, so the animation pass can replace the snap in one place. Hiding the
    /// balls themselves (not just drawing over them) lets the cover art have soft or translucent edges.
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

        public CoverKind Kind { get; private set; }

        /// <summary>The veil's colour id or the lock's id.</summary>
        public int Id { get; private set; }

        /// <summary>The covered tubes; one for a veil, every tube of the lock for a lock box.</summary>
        public IReadOnlyList<TubeView> Tubes => _tubes;

        public bool IsOpen { get; private set; }

        /// <summary>
        /// Makes this a veil that opens on <paramref name="colorId"/>, and forgets any previous tubes.
        /// Add its tube, then fit and close it.
        /// </summary>
        public void SetupVeil(int colorId, TubeVisualConfig config, ColorPalette palette)
        {
            Begin(CoverKind.Veil, colorId);
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
            _lockBox.Setup(config, locks.GetMid(lockId));
        }

        private void Begin(CoverKind kind, int id)
        {
            Kind = kind;
            Id = id;
            _tubes.Clear();
            StopMotion();
        }

        public void AddTube(TubeView tube) => _tubes.Add(tube);

        /// <summary>Darkens the cover by <paramref name="shade"/> (1 = as drawn), e.g. while the Shuffle booster picks a tube.</summary>
        public void SetShade(float shade)
        {
            _body.color = Shade(_bodyTint, shade);
            _icon.color = Shade(_iconTint, shade);
            if (Kind == CoverKind.Lock)
            {
                _lockBox.SetShade(shade);
            }
        }

        private static Color Shade(Color tint, float shade) => new Color(tint.r * shade, tint.g * shade, tint.b * shade, tint.a);

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
            _icon.transform.localPosition = new Vector3(0f, tubeHeight - veilHeight * 0.5f, 0f);
        }

        /// <summary>
        /// The single entry point for a cover's state: closed shows the cover and hides the covered
        /// balls (a veil leaves its tube's bottom ball showing under its hem; a lock box hides them all),
        /// open hides the cover and shows them again. Snaps for now.
        /// </summary>
        public void SetOpen(bool open)
        {
            IsOpen = open;
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

        /// <summary>Ends a running shake: the cover is rebuilt or pooled.</summary>
        public void StopMotion()
        {
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

        private void ApplyOffset() => transform.localPosition = _home + new Vector3(_shakeX, 0f, 0f);

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 1f;

        private static float SpriteHeight(Sprite sprite) => sprite != null ? sprite.bounds.size.y : 1f;

        private void Awake()
        {
            if (_body == null || _icon == null || _lockBox == null)
            {
                Debug.LogError($"{name}: body, icon and lock box must be assigned.", this);
            }

            _shakeStep = ShakeStep;
        }

        private void OnDestroy() => BoardTweens.KillIfActive(_shakeTween);
    }
}
