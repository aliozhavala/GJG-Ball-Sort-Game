using System;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// One tutorial popup: the level that shows it, its text and an optional image. With a name it is a
    /// New Item card (docs/screens.md, Tutorials); without one, the plain popup. Listed in a <see cref="TutorialConfig"/>.
    /// </summary>
    [Serializable]
    public sealed class TutorialEntry
    {
        [Tooltip("Id of the level that shows this popup the first time it is entered (its file sets \"tutorial\": true).")]
        [SerializeField, Min(1)] private int _levelId = 1;

        [Tooltip("The mechanic's name, the New Item card's title (\"Big Tube\"). Empty = the plain popup (level 1).")]
        [SerializeField] private string _name = string.Empty;

        [Tooltip("The main line; rich text, its key word in caps and highlighted.")]
        [SerializeField, TextArea(2, 6)] private string _text = string.Empty;

        [Tooltip("The hint under the main line, in smaller type. Optional.")]
        [SerializeField, TextArea(1, 4)] private string _hint = string.Empty;

        [Tooltip("Optional picture above the text, e.g. the mechanic in action. Hidden when empty.")]
        [SerializeField] private Sprite _image;

        public int LevelId => _levelId;

        /// <summary>The New Item card's title; empty for the plain popup.</summary>
        public string Name => _name;

        /// <summary>The main line.</summary>
        public string Text => _text;

        /// <summary>The hint line; empty for none.</summary>
        public string Hint => _hint;

        /// <summary>Shown above the text; null for none.</summary>
        public Sprite Image => _image;
    }
}
