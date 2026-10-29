using System;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>One tutorial popup: the level that shows it, its text and an optional image. Listed in a <see cref="TutorialConfig"/>.</summary>
    [Serializable]
    public sealed class TutorialEntry
    {
        [Tooltip("Id of the level that shows this popup the first time it is entered (its file sets \"tutorial\": true).")]
        [SerializeField, Min(1)] private int _levelId = 1;

        [SerializeField, TextArea(2, 6)] private string _text = string.Empty;

        [Tooltip("Optional picture above the text, e.g. the mechanic in action. Hidden when empty.")]
        [SerializeField] private Sprite _image;

        public int LevelId => _levelId;

        public string Text => _text;

        /// <summary>Shown above the text; null for none.</summary>
        public Sprite Image => _image;
    }
}
