using System;
using System.Collections;
using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// A one-shot particle effect (sparkles, ring, confetti, droplets), pooled by <see cref="VfxPool"/>.
    /// Counts, sizes, lifetimes, gravity and sprites live on the prefab's particle systems; the burst
    /// only gives them a place, a colour and the chain.
    /// </summary>
    public sealed class VfxBurst : MonoBehaviour
    {
        [Tooltip("Plays its child systems too; the burst goes back to the pool once all their particles are gone.")]
        [SerializeField] private ParticleSystem _root;
        [Tooltip("Systems whose start colour becomes the burst's colour.")]
        [SerializeField] private ParticleSystem[] _tinted = new ParticleSystem[0];
        [Tooltip("Systems whose particles each take white, the burst's colour or the accent, at random.")]
        [SerializeField] private ParticleSystem[] _mixed = new ParticleSystem[0];
        [SerializeField] private Color _accent = new Color(1f, 0.82f, 0.3f);
        [Tooltip("Optional: its first burst's count and its start speed grow with the chain.")]
        [SerializeField] private ParticleSystem _chainScaled;
        [SerializeField, Min(0)] private int _countPerChain = 3;
        [Tooltip("Start speed added per chain step, as a fraction of the prefab's speed.")]
        [SerializeField, Min(0f)] private float _speedPerChain = 0.08f;

        private readonly GradientColorKey[] _mixKeys = new GradientColorKey[3];
        private readonly GradientAlphaKey[] _mixAlpha = { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
        private Gradient _mix;
        private Action<VfxBurst> _release;
        private float _baseCount;
        private float _baseSpeed;

        /// <summary>Called once by the pool that owns the burst.</summary>
        public void Initialize(Action<VfxBurst> release) => _release = release;

        /// <summary>Plays the effect at <paramref name="position"/> (world space) in <paramref name="colour"/>.</summary>
        /// <param name="chain">Tubes completed before this one in the level; 0 for no escalation.</param>
        public void Play(Vector3 position, Color colour, int chain)
        {
            transform.position = position;

            foreach (ParticleSystem system in _tinted)
            {
                ParticleSystem.MainModule main = system.main;
                main.startColor = colour;
            }

            if (_mixed.Length > 0)
            {
                _mixKeys[1] = new GradientColorKey(colour, 0.67f);
                _mix.SetKeys(_mixKeys, _mixAlpha);
                var startColor = new ParticleSystem.MinMaxGradient(_mix) { mode = ParticleSystemGradientMode.RandomColor };
                foreach (ParticleSystem system in _mixed)
                {
                    ParticleSystem.MainModule main = system.main;
                    main.startColor = startColor;
                }
            }

            if (_chainScaled != null && _chainScaled.emission.burstCount > 0)
            {
                ParticleSystem.EmissionModule emission = _chainScaled.emission;
                ParticleSystem.Burst burst = emission.GetBurst(0);
                burst.count = _baseCount + _countPerChain * chain;
                emission.SetBurst(0, burst);
                ParticleSystem.MainModule main = _chainScaled.main;
                main.startSpeedMultiplier = _baseSpeed * (1f + _speedPerChain * chain);
            }

            _root.Play(true);
            StartCoroutine(ReleaseWhenDone());
        }

        /// <summary>Stops the effect at once (the pool is taking it back).</summary>
        public void Stop()
        {
            StopAllCoroutines();
            _root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private IEnumerator ReleaseWhenDone()
        {
            // Wait a frame so the burst has emitted before asking whether it is alive.
            yield return null;
            while (_root.IsAlive(true))
            {
                yield return null;
            }

            _release?.Invoke(this);
        }

        private void Awake()
        {
            if (_root == null)
            {
                Debug.LogError($"{name}: root particle system must be assigned.", this);
                return;
            }

            // Fixed mode: each particle gets exactly one of the three colours, not a blend.
            _mix = new Gradient { mode = GradientMode.Fixed };
            _mixKeys[0] = new GradientColorKey(Color.white, 0.33f);
            _mixKeys[2] = new GradientColorKey(_accent, 1f);

            if (_chainScaled != null)
            {
                _baseCount = _chainScaled.emission.burstCount > 0 ? _chainScaled.emission.GetBurst(0).count.constant : 0f;
                _baseSpeed = _chainScaled.main.startSpeedMultiplier;
            }
        }
    }
}
