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
        /// Validates <paramref name="level"/> and builds its board and row layout.
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

            int[] rows = LevelLayout.Resolve(level.Tubes.Count, level.Layout?.Rows);
            return new LoadedLevel(levelName, level, CreateBoard(level), rows);
        }

        /// <summary>
        /// Builds a fresh board from level data, with every hidden ball hidden again. Used by
        /// <see cref="Load"/> and by restart.
        /// </summary>
        /// <remarks>Assumes <paramref name="level"/> has passed <see cref="LevelValidator"/>.</remarks>
        public static BoardState CreateBoard(LevelDto level)
        {
            var tubes = new Tube[level.Tubes.Count];
            for (int i = 0; i < tubes.Length; i++)
            {
                TubeDto dto = level.Tubes[i];
                var balls = new Ball[dto.Balls.Count];
                for (int slot = 0; slot < balls.Length; slot++)
                {
                    bool hidden = dto.Hidden != null && dto.Hidden[slot];
                    balls[slot] = new Ball(dto.Balls[slot], hidden);
                }

                tubes[i] = new Tube(dto.Capacity, balls);
            }

            return new BoardState(tubes);
        }
    }
}
