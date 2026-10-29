using System.Collections.Generic;
using BallSort.Core.Model;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Turns validated level data into playable state.
    /// </summary>
    public static class LevelFactory
    {
        /// <summary>
        /// Validates <paramref name="level"/>, every variant included, and wraps it for play.
        /// </summary>
        /// <param name="level">Parsed level data.</param>
        /// <param name="levelName">Display name used in error messages, e.g. <c>level_003</c>.</param>
        /// <exception cref="LevelFormatException">If the level breaks any rule in <see cref="LevelValidator"/>.</exception>
        public static LoadedLevel Load(LevelDto level, string levelName)
        {
            IReadOnlyList<string> errors = LevelValidator.Validate(level, levelName);
            if (errors.Count > 0)
            {
                throw new LevelFormatException(levelName, errors);
            }

            return new LoadedLevel(levelName, level);
        }

        /// <summary>
        /// Builds a fresh board from one variant, with every hidden ball hidden again and every cover
        /// and key back in place. Used for each new try and by the validator.
        /// </summary>
        /// <remarks>Assumes <paramref name="variant"/> has passed <see cref="LevelValidator"/>.</remarks>
        public static BoardState CreateBoard(LevelVariantDto variant)
        {
            var lockOfTube = new Dictionary<int, int>();
            if (variant.Locks != null)
            {
                foreach (LockDto lockDto in variant.Locks)
                {
                    foreach (int tube in lockDto.Tubes)
                    {
                        lockOfTube.Add(tube, lockDto.Id.Value);
                    }
                }
            }

            var tubes = new Tube[variant.Tubes.Count];
            for (int i = 0; i < tubes.Length; i++)
            {
                TubeDto dto = variant.Tubes[i];
                var balls = new Ball[dto.Balls.Count];
                for (int slot = 0; slot < balls.Length; slot++)
                {
                    bool hidden = dto.Hidden != null && dto.Hidden[slot];
                    balls[slot] = new Ball(dto.Balls[slot], hidden);
                }

                if (dto.Keys != null)
                {
                    foreach (KeyDto key in dto.Keys)
                    {
                        Ball ball = balls[key.Slot.Value];
                        balls[key.Slot.Value] = new Ball(ball.ColorId, ball.IsHidden, key.Lock.Value);
                    }
                }

                TubeCover cover = dto.Veil != null ? TubeCover.Veil(dto.Veil.Color.Value)
                    : lockOfTube.TryGetValue(i, out int lockId) ? TubeCover.Lock(lockId)
                    : TubeCover.None;

                tubes[i] = new Tube(dto.Capacity, balls, cover, dto.Clay);
            }

            return new BoardState(tubes);
        }

        /// <summary>
        /// Builds a fresh grid from level data, without added tubes. Every variant has the same tubes, so
        /// one grid fits them all. Used for each new try.
        /// </summary>
        /// <remarks>Assumes <paramref name="level"/> has passed <see cref="LevelValidator"/>.</remarks>
        public static BoardGrid CreateGrid(LevelDto level) => new BoardGrid(level.Layout.Grid, level.Variants[0].Tubes.Count);
    }
}
