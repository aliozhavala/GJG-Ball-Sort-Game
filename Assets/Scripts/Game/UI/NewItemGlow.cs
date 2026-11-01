using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The light behind a New Item card's picture: warm rays turning slowly over a soft lilac glow, with
    /// four-pointed sparkles twinkling around it (the prototype's New Item card). A uGUI mesh, so it needs
    /// no art; it grows in each time it is enabled.
    /// </summary>
    /// <remarks>Real time, so it turns while the board is paused under the popup.</remarks>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NewItemGlow : MaskableGraphic
    {
        [SerializeField, Range(3, 24)] private int _rays = 10;
        [Tooltip("Half the width of each ray, in radians.")]
        [SerializeField, Range(0.02f, 0.5f)] private float _rayHalfWidth = 0.13f;
        [Tooltip("Radians per second.")]
        [SerializeField] private float _turnSpeed = 0.22f;
        [SerializeField] private Color _rayColour = new Color(1f, 0.925f, 0.745f, 0.5f);
        [SerializeField] private Color _glowColour = new Color(0.784f, 0.588f, 1f, 0.45f);
        [Tooltip("The glow's radius as a share of the rays'.")]
        [SerializeField, Range(0f, 1f)] private float _glowRadius = 0.6f;
        [Tooltip("Seconds the rays take to grow to full length (OutCubic).")]
        [SerializeField, Min(0.01f)] private float _growDuration = 0.5f;

        [Header("Sparkles")]
        [SerializeField] private Color _sparkleColour = new Color(1f, 0.886f, 0.478f, 1f);
        [Tooltip("Sparkles born per second, anywhere in the rect.")]
        [SerializeField, Min(0f)] private float _sparkleRate = 16f;
        [SerializeField] private Vector2 _sparkleSize = new Vector2(8f, 18f);
        [SerializeField] private Vector2 _sparkleLife = new Vector2(0.5f, 1.1f);

        private const int GlowSegments = 32;

        private struct Sparkle
        {
            public Vector2 Position;
            public float Age;
            public float Life;
            public float Size;
        }

        private readonly List<Sparkle> _sparkles = new List<Sparkle>(32);
        private readonly UIVertex[] _quad = new UIVertex[4];
        private float _time;

        protected override void OnEnable()
        {
            base.OnEnable();
            _time = 0f;
            _sparkles.Clear();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            Rect rect = rectTransform.rect;
            if (Random.value < dt * _sparkleRate)
            {
                _sparkles.Add(new Sparkle
                {
                    Position = new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax)),
                    Life = Random.Range(_sparkleLife.x, _sparkleLife.y),
                    Size = Random.Range(_sparkleSize.x, _sparkleSize.y),
                });
            }

            for (int i = _sparkles.Count - 1; i >= 0; i--)
            {
                Sparkle s = _sparkles[i];
                s.Age += dt;
                if (s.Age >= s.Life)
                {
                    _sparkles.RemoveAt(i);
                    continue;
                }

                _sparkles[i] = s;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 centre = rect.center;
            float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(_time / _growDuration), 3f);
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f * grow;
            if (radius <= 0f)
            {
                return;
            }

            AddGlow(vh, centre, radius * _glowRadius);
            float turn = _time * _turnSpeed;
            for (int i = 0; i < _rays; i++)
            {
                float angle = turn + i * Mathf.PI * 2f / _rays;
                AddRay(vh, centre, radius, angle);
            }

            foreach (Sparkle s in _sparkles)
            {
                AddSparkle(vh, s.Position, s.Size, Mathf.Sin(s.Age / s.Life * Mathf.PI));
            }
        }

        // A wedge from the centre, bright at the middle and fading to nothing at its tip.
        private void AddRay(VertexHelper vh, Vector2 centre, float radius, float angle)
        {
            int start = vh.currentVertCount;
            vh.AddVert(centre, Tint(_rayColour, 1f), Vector4.zero);
            vh.AddVert(centre + Direction(angle - _rayHalfWidth) * radius, Tint(_rayColour, 0f), Vector4.zero);
            vh.AddVert(centre + Direction(angle + _rayHalfWidth) * radius, Tint(_rayColour, 0f), Vector4.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        private void AddGlow(VertexHelper vh, Vector2 centre, float radius)
        {
            int start = vh.currentVertCount;
            vh.AddVert(centre, Tint(_glowColour, 1f), Vector4.zero);
            for (int i = 0; i <= GlowSegments; i++)
            {
                vh.AddVert(centre + Direction(i * Mathf.PI * 2f / GlowSegments) * radius, Tint(_glowColour, 0f), Vector4.zero);
            }

            for (int i = 0; i < GlowSegments; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 2 + i);
            }
        }

        // Two thin crossed diamonds: a four-pointed star.
        private void AddSparkle(VertexHelper vh, Vector2 at, float size, float alpha)
        {
            Color32 tint = Tint(_sparkleColour, alpha);
            float thin = size * 0.28f;
            SetQuad(at + new Vector2(0f, size), at + new Vector2(thin, 0f), at + new Vector2(0f, -size), at + new Vector2(-thin, 0f), tint);
            vh.AddUIVertexQuad(_quad);
            SetQuad(at + new Vector2(size, 0f), at + new Vector2(0f, -thin), at + new Vector2(-size, 0f), at + new Vector2(0f, thin), tint);
            vh.AddUIVertexQuad(_quad);
        }

        private void SetQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 tint)
        {
            SetVertex(0, a, tint);
            SetVertex(1, b, tint);
            SetVertex(2, c, tint);
            SetVertex(3, d, tint);
        }

        private void SetVertex(int index, Vector2 position, Color32 tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            _quad[index] = vertex;
        }

        private Color32 Tint(Color colour, float alpha)
        {
            Color tinted = colour * color;
            tinted.a *= alpha;
            return tinted;
        }

        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
