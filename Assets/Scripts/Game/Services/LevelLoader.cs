using System;
using BallSort.Core.Levels;
using BallSort.LevelJson;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Turns a level <see cref="TextAsset"/> into a playable <see cref="LoadedLevel"/>: parse,
    /// validate, build the board and resolve the row layout.
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
    }
}
