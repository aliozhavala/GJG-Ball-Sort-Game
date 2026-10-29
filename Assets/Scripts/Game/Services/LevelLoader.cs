using System;
using BallSort.Core.Levels;
using BallSort.LevelJson;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Turns a level <see cref="TextAsset"/> into a playable <see cref="LoadedLevel"/>: parse and
    /// validate every variant; each try then builds its own board.
    /// </summary>
    public sealed class LevelLoader
    {
        /// <exception cref="LevelFormatException">If the file cannot be parsed or breaks a format rule.</exception>
        public LoadedLevel Load(TextAsset levelFile)
        {
            if (levelFile == null)
            {
                throw new ArgumentNullException(nameof(levelFile));
            }

            LevelDto dto = LevelJsonParser.Parse(levelFile.text, levelFile.name);
            return LevelFactory.Load(dto, levelFile.name);
        }

        /// <summary>
        /// The tier a level file declares, read without validating the rest: Home shows it before the
        /// level is played. Regular when the file is missing or unreadable; playing it reports why.
        /// </summary>
        public LevelTier ReadTier(TextAsset levelFile)
        {
            if (levelFile == null)
            {
                return LevelTier.Regular;
            }

            try
            {
                return LevelJsonParser.Parse(levelFile.text, levelFile.name)?.Tier ?? LevelTier.Regular;
            }
            catch (LevelFormatException)
            {
                return LevelTier.Regular;
            }
        }
    }
}
