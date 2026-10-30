using System.Collections.Generic;
using BallSort.Core.Model;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One cover on screen: a veil over one tube, or a lock's door across all of its tubes. A sliced
    /// body with an icon in its centre (the veil's colour badge or the lock icon).
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
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _icon;

        private readonly List<TubeView> _tubes = new List<TubeView>();

        public CoverKind Kind { get; private set; }

        /// <summary>The veil's colour id or the lock's id.</summary>
        public int Id { get; private set; }

        /// <summary>The covered tubes; one for a veil, every tube of the lock for a door.</summary>
        public IReadOnlyList<TubeView> Tubes => _tubes;

        public bool IsOpen { get; private set; }

        /// <summary>Styles the cover and forgets any previous tubes. Add tubes, then fit and close it.</summary>
        /// <param name="iconSize">Width of the icon in board units.</param>
        public void Setup(CoverKind kind, int id, Sprite body, Color bodyTint, Sprite icon, Color iconTint, float iconSize)
        {
            Kind = kind;
            Id = id;
            _tubes.Clear();

            _body.sprite = body;
            _body.color = bodyTint;
            _body.drawMode = SpriteDrawMode.Sliced;
            _icon.sprite = icon;
            _icon.color = iconTint;
            _icon.transform.localScale = Vector3.one * (iconSize / SpriteWidth(icon));
        }

        public void AddTube(TubeView tube) => _tubes.Add(tube);

        /// <summary>
        /// Fits the body to the covered tubes. A veil hangs from its tube's rim like the art, as wide as
        /// drawn, its hem <see cref="TubeVisualConfig.VeilBottomGap"/> above the base. A door spans from
        /// the left edge of the leftmost tube to the right edge of the rightmost, as tall as the
        /// tallest, standing on their base line.
        /// </summary>
        /// <remarks>A lock's tubes sit next to each other in one row and share a capacity (docs/mechanics.md).</remarks>
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
                float x = tube.transform.localPosition.x;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
                height = Mathf.Max(height, tube.BodyHeight);
            }

            transform.localPosition = new Vector3((left + right) * 0.5f, _tubes[0].transform.localPosition.y, 0f);
            if (Kind == CoverKind.Veil)
            {
                FitVeil(config, height);
                return;
            }

            var centre = new Vector3(0f, height * 0.5f, 0f);
            _body.transform.localScale = Vector3.one;
            _body.size = new Vector2(right - left + config.TubeWidth, height);
            _body.transform.localPosition = centre;
            _icon.transform.localPosition = centre;
        }

        /// <remarks>
        /// Drawn at the tube art's scale, so its sliced top and hem keep their drawn size and only the
        /// stripes between them stretch with the tube's height. The badge sits in the veil's middle.
        /// </remarks>
        private void FitVeil(TubeVisualConfig config, float tubeHeight)
        {
            float scale = config.ArtScale;
            float veilHeight = Mathf.Max(tubeHeight - config.VeilBottomGap, 0.1f);
            _body.transform.localScale = new Vector3(scale, scale, 1f);
            _body.transform.localPosition = new Vector3(0f, tubeHeight, 0f);
            _body.size = new Vector2(SpriteWidth(_body.sprite), veilHeight / scale);
            _icon.transform.localPosition = new Vector3(0f, tubeHeight - veilHeight * 0.5f, 0f);
        }

        /// <summary>
        /// The single entry point for a cover's state: closed shows the cover and hides the covered
        /// balls, open hides the cover and shows them again. Snaps for now.
        /// </summary>
        public void SetOpen(bool open)
        {
            IsOpen = open;
            _body.enabled = !open && _body.sprite != null;
            _icon.enabled = !open && _icon.sprite != null;
            foreach (TubeView tube in _tubes)
            {
                tube.SetBallsVisible(open);
            }
        }

        private static float SpriteWidth(Sprite sprite) => sprite != null ? sprite.bounds.size.x : 1f;

        private void Awake()
        {
            if (_body == null || _icon == null)
            {
                Debug.LogError($"{name}: body and icon must be assigned.", this);
            }
        }
    }
}
