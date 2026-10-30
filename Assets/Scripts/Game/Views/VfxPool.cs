using UnityEngine;
using UnityEngine.Pool;

namespace BallSort.Game.Views
{
    /// <summary>Pools one <see cref="VfxBurst"/> prefab; bursts return themselves when their particles are gone.</summary>
    public sealed class VfxPool
    {
        private readonly VfxBurst _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<VfxBurst> _pool;

        /// <param name="parent">Where the bursts live; the board root, so they scale with the board.</param>
        public VfxPool(VfxBurst prefab, Transform parent, int capacity)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<VfxBurst>(
                Create,
                burst => burst.gameObject.SetActive(true),
                burst => { burst.Stop(); burst.gameObject.SetActive(false); },
                burst => Object.Destroy(burst.gameObject),
                collectionCheck: true, defaultCapacity: capacity, maxSize: capacity * 4);
        }

        /// <summary>Plays a burst at <paramref name="position"/> (world space).</summary>
        public void Play(Vector3 position, Color colour, int chain) => _pool.Get().Play(position, colour, chain);

        private VfxBurst Create()
        {
            VfxBurst burst = Object.Instantiate(_prefab, _parent);
            burst.Initialize(Release);
            return burst;
        }

        private void Release(VfxBurst burst) => _pool.Release(burst);
    }
}
