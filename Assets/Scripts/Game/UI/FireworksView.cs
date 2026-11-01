using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// Fireworks behind the Win panel (docs/animation-spec.md, 5.2): rockets rise from below the screen
    /// and burst into falling sparks with a flash. The big bursts land on the beats given to
    /// <see cref="Play"/> (the fanfare's bangs), small ones fill the time between.
    /// </summary>
    /// <remarks>
    /// A uGUI mesh, so it draws on the Overlay canvas between the dim and the panel, where particle
    /// systems cannot. Ported from the prototype's fireworks: sizes and speeds are in the prototype's
    /// pixels (a phone <see cref="_referenceWidth"/> wide) and scaled to this rect's width. Real time,
    /// not <see cref="Views.AnimationConfig.Speed"/>: the beats belong to the sound. Disabling it (the
    /// panel closing) clears it.
    /// </remarks>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FireworksView : MaskableGraphic
    {
        [Tooltip("Rocket and spark colours; each burst mixes its rocket's colour with one more.")]
        [SerializeField] private Color[] _colours =
        {
            new Color32(255, 210, 58, 255), new Color32(255, 255, 255, 255), new Color32(127, 214, 255, 255),
            new Color32(255, 126, 182, 255), new Color32(181, 140, 255, 255), new Color32(255, 169, 77, 255),
            new Color32(125, 255, 154, 255),
        };

        [Tooltip("The prototype phone's width: every size and speed below is in its pixels, scaled to this rect's width.")]
        [SerializeField, Min(1f)] private float _referenceWidth = 390f;

        [Header("Timing")]
        [Tooltip("The show lasts the last beat + this, kept between the two limits below (without beats: the lower limit).")]
        [SerializeField, Min(0f)] private float _tailAfterLastBeat = 0.45f;
        [SerializeField, Min(0f)] private float _minDuration = 2.6f;
        [SerializeField, Min(0f)] private float _maxDuration = 3.6f;
        [Tooltip("A rocket's climb, at most (shorter when its burst is due sooner).")]
        [SerializeField, Min(0.01f)] private float _maxFuse = 0.45f;

        [Header("Bursts")]
        [SerializeField, Min(1)] private int _bigSparks = 70;
        [SerializeField, Min(1)] private int _smallSparks = 38;
        [SerializeField, Min(0f)] private float _bigSpeed = 240f;
        [SerializeField, Min(0f)] private float _smallSpeed = 170f;
        [Tooltip("Pull on the sparks, in reference pixels per second².")]
        [SerializeField] private float _gravity = 150f;
        [Tooltip("Air drag on the sparks, per second.")]
        [SerializeField, Min(0f)] private float _drag = 1.6f;
        [SerializeField, Min(0f)] private float _bigFlashRadius = 110f;
        [SerializeField, Min(0f)] private float _smallFlashRadius = 60f;
        [SerializeField, Min(0f)] private float _rocketWidth = 3f;
        [SerializeField, Min(0f)] private float _sparkWidth = 2.4f;

        private const float StreakLength = 2f; // each streak trails three frames' travel at 60 fps, as in the prototype
        private const float ReferenceFrame = 1f / 60f;
        private const float GlintChance = 0.15f;
        private const float FlashAlpha = 0.95f;
        private const float MaxStep = 0.1f;

        private enum Kind
        {
            Rocket,
            Spark,
            Flash,
        }

        private struct Launch
        {
            public float At;
            public float Fuse;
            public bool Big;
        }

        private struct Particle
        {
            public Kind Kind;
            public bool Big;
            public Vector2 Position;
            public Vector2 Previous;
            public Vector2 Velocity;
            public Vector2 From;
            public Vector2 To;
            public float Age;
            public float Life;
            public Color Colour;
        }

        private static readonly Color FlashColour = new Color(1f, 1f, 0.9f, 1f);
        private static Texture2D s_glow;

        private readonly List<Launch> _queue = new List<Launch>(64);
        private readonly List<Particle> _particles = new List<Particle>(1024);
        private readonly UIVertex[] _quad = new UIVertex[4];
        private float _time;
        private bool _playing;

        public override Texture mainTexture => Glow;

        /// <summary>Starts the show; <paramref name="beats"/> are seconds from now when a big burst lands (none: a default pattern).</summary>
        public void Play(IReadOnlyList<float> beats)
        {
            _queue.Clear();
            _particles.Clear();
            _time = 0f;
            bool hasBeats = beats != null && beats.Count > 0;
            if (hasBeats)
            {
                float last = 0f;
                for (int i = 0; i < beats.Count; i++)
                {
                    Queue(beats[i], true);
                    if (i % 2 == 0)
                    {
                        Queue(beats[i] + 0.04f, false);
                    }

                    last = Mathf.Max(last, beats[i]);
                }

                float duration = Mathf.Clamp(last + _tailAfterLastBeat, _minDuration, _maxDuration);
                for (float t = 0.55f; t < duration - 0.2f; t += Random.Range(0.16f, 0.3f))
                {
                    if (!NearBeat(beats, t))
                    {
                        Queue(t, false);
                    }
                }
            }
            else
            {
                Queue(0.45f, true);
                Queue(0.5f, true);
                Queue(0.55f, true);
                for (float t = 0.7f; t < _minDuration - 0.15f; t += Random.Range(0.13f, 0.25f))
                {
                    Queue(t, Random.value < 0.4f);
                }
            }

            _queue.Sort((a, b) => a.At.CompareTo(b.At));
            _playing = true;
            SetVerticesDirty();
        }

        /// <summary>Clears every rocket and spark at once.</summary>
        public void Stop()
        {
            _queue.Clear();
            _particles.Clear();
            _playing = false;
            SetVerticesDirty();
        }

        // A rocket launches a fuse before its burst is due.
        private void Queue(float burstAt, bool big)
        {
            float fuse = Mathf.Clamp(burstAt, 0.12f, _maxFuse);
            _queue.Add(new Launch { At = burstAt - fuse, Fuse = fuse, Big = big });
        }

        private static bool NearBeat(IReadOnlyList<float> beats, float t)
        {
            for (int i = 0; i < beats.Count; i++)
            {
                if (Mathf.Abs(beats[i] - t) < 0.08f)
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            // The clock follows real time so the bursts stay on the sound's beats; after a hitch (the app
            // resuming) rockets whose burst is already past are dropped rather than all fired at once, and
            // the motion steps at most MaxStep.
            _time += Time.unscaledDeltaTime;
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            Vector2 size = ReferenceSize();
            while (_queue.Count > 0 && _queue[0].At <= _time)
            {
                Launch launch = _queue[0];
                _queue.RemoveAt(0);
                if (launch.At + launch.Fuse >= _time - MaxStep)
                {
                    LaunchRocket(launch, size);
                }
            }

            float damping = Mathf.Max(0f, 1f - _drag * dt);
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle p = _particles[i];
                p.Age += dt;
                Vector2 before = p.Position;
                if (p.Kind == Kind.Rocket)
                {
                    float u = Mathf.Min(1f, p.Age / p.Life);
                    float k = 1f - (1f - u) * (1f - u); // OutQuad
                    p.Position = Vector2.LerpUnclamped(p.From, p.To, k);
                    if (u >= 1f)
                    {
                        RemoveAt(i);
                        Burst(p.Position, p.Colour, p.Big);
                        continue;
                    }
                }
                else if (p.Kind == Kind.Spark)
                {
                    p.Velocity.x *= damping;
                    p.Velocity.y = p.Velocity.y * damping + _gravity * dt;
                    p.Position += p.Velocity * dt;
                }

                if (p.Kind != Kind.Rocket && p.Age >= p.Life)
                {
                    RemoveAt(i);
                    continue;
                }

                // Where it was one prototype frame ago at its current speed, so streaks keep their length at any frame rate.
                p.Previous = dt > 0f ? p.Position - (p.Position - before) * (ReferenceFrame / dt) : p.Position;
                _particles[i] = p;
            }

            if (_queue.Count == 0 && _particles.Count == 0)
            {
                _playing = false;
            }

            SetVerticesDirty();
        }

        // In reference pixels, y down from the top (the prototype's canvas): bursts high on the screen,
        // rockets from just below its bottom edge.
        private void LaunchRocket(Launch launch, Vector2 size)
        {
            float x = 40f + Random.value * (size.x - 80f);
            float y = 110f + Random.value * (launch.Big ? 250f : 330f);
            Vector2 from = new Vector2(x + (Random.value - 0.5f) * 90f, size.y + 10f);
            _particles.Add(new Particle
            {
                Kind = Kind.Rocket,
                Big = launch.Big,
                Position = from,
                Previous = from,
                From = from,
                To = new Vector2(x, y),
                Life = launch.Fuse,
                Colour = RandomColour(),
            });
        }

        private void Burst(Vector2 at, Color colour, bool big)
        {
            int count = big ? _bigSparks : _smallSparks;
            float speed = big ? _bigSpeed : _smallSpeed;
            Color second = RandomColour();
            for (int k = 0; k < count; k++)
            {
                float angle = (float)k / count * Mathf.PI * 2f + Random.value * 0.2f;
                float v = speed * Random.Range(0.45f, 1.05f);
                _particles.Add(new Particle
                {
                    Kind = Kind.Spark,
                    Position = at,
                    Previous = at,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * v,
                    Life = (big ? 1.2f : 0.9f) + Random.value * 0.6f,
                    Colour = k % 3 != 0 ? colour : second,
                });
            }

            _particles.Add(new Particle { Kind = Kind.Flash, Big = big, Position = at, Previous = at, Life = big ? 0.3f : 0.2f });
        }

        private Color RandomColour() => _colours == null || _colours.Length == 0 ? Color.white : _colours[Random.Range(0, _colours.Length)];

        private void RemoveAt(int i)
        {
            int last = _particles.Count - 1;
            _particles[i] = _particles[last];
            _particles.RemoveAt(last);
        }

        private float Scale => rectTransform.rect.width / _referenceWidth;

        private Vector2 ReferenceSize()
        {
            Rect rect = rectTransform.rect;
            float scale = Scale;
            return scale > 0f ? new Vector2(rect.width, rect.height) / scale : new Vector2(_referenceWidth, _referenceWidth * 2f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_particles.Count == 0)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            float scale = Scale;
            Vector2 origin = new Vector2(rect.xMin, rect.yMax);
            for (int i = 0; i < _particles.Count; i++)
            {
                Particle p = _particles[i];
                float u = p.Age / p.Life;
                Vector2 position = origin + new Vector2(p.Position.x, -p.Position.y) * scale;
                if (p.Kind == Kind.Flash)
                {
                    float radius = (p.Big ? _bigFlashRadius : _smallFlashRadius) * scale;
                    AddFlash(vh, position, radius, FlashAlpha * (1f - u));
                    continue;
                }

                Vector2 previous = origin + new Vector2(p.Previous.x, -p.Previous.y) * scale;
                Vector2 tail = previous - (position - previous) * StreakLength;
                float alpha = p.Kind == Kind.Rocket ? 1f : Mathf.Max(0f, 1f - u * u);
                float width = (p.Kind == Kind.Rocket ? _rocketWidth : _sparkWidth) * scale;
                AddStreak(vh, tail, position, width, p.Colour, alpha);
                if (p.Kind == Kind.Spark && Random.value < GlintChance)
                {
                    AddStreak(vh, position - new Vector2(scale, 0f), position + new Vector2(scale, 0f), 2f * scale, Color.white, alpha);
                }
            }
        }

        // A streak samples the glow texture's opaque centre, so streaks and flashes share one draw call.
        private void AddStreak(VertexHelper vh, Vector2 from, Vector2 to, float width, Color colour, float alpha)
        {
            Vector2 along = to - from;
            if (along.sqrMagnitude < 1e-6f)
            {
                along = Vector2.up * 0.01f;
            }

            Vector2 side = new Vector2(-along.y, along.x).normalized * (width * 0.5f);
            Color32 tint = Tint(colour, alpha);
            Vector2 centre = new Vector2(0.5f, 0.5f);
            SetVertex(0, from - side, tint, centre);
            SetVertex(1, from + side, tint, centre);
            SetVertex(2, to + side, tint, centre);
            SetVertex(3, to - side, tint, centre);
            vh.AddUIVertexQuad(_quad);
        }

        private void AddFlash(VertexHelper vh, Vector2 centre, float radius, float alpha)
        {
            Color32 tint = Tint(FlashColour, alpha);
            SetVertex(0, centre + new Vector2(-radius, -radius), tint, new Vector2(0f, 0f));
            SetVertex(1, centre + new Vector2(-radius, radius), tint, new Vector2(0f, 1f));
            SetVertex(2, centre + new Vector2(radius, radius), tint, new Vector2(1f, 1f));
            SetVertex(3, centre + new Vector2(radius, -radius), tint, new Vector2(1f, 0f));
            vh.AddUIVertexQuad(_quad);
        }

        private Color32 Tint(Color colour, float alpha)
        {
            Color tinted = colour * color;
            tinted.a *= alpha;
            return tinted;
        }

        private void SetVertex(int index, Vector2 position, Color32 tint, Vector2 uv)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            vertex.uv0 = uv;
            _quad[index] = vertex;
        }

        // White, alpha falling off squared from the centre: the prototype's flash is a linear gradient drawn
        // additively; under plain alpha blending the squared falloff reads as the same glow, not a disc.
        private static Texture2D Glow
        {
            get
            {
                if (s_glow != null)
                {
                    return s_glow;
                }

                const int size = 64;
                s_glow = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "FireworksGlow",
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave,
                };
                var pixels = new Color32[size * size];
                float half = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float r = new Vector2(x - half, y - half).magnitude / half;
                        float fall = Mathf.Clamp01(1f - r);
                        float a = r < 0.06f ? 1f : fall * fall;
                        pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                }

                s_glow.SetPixels32(pixels);
                s_glow.Apply(false, true);
                return s_glow;
            }
        }

        protected override void OnDisable()
        {
            Stop();
            base.OnDisable();
        }
    }
}
