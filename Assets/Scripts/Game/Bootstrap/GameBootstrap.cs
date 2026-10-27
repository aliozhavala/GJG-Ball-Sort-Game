using BallSort.Core.Levels;
using BallSort.Game.Services;
using BallSort.Game.Views;
using UnityEngine;

namespace BallSort.Game.Bootstrap
{
    /// <summary>
    /// Composition root: creates the services and wires them to the scene's views.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelDatabase _levelDatabase;
        [SerializeField] private BoardView _boardView;

        // TEMPORARY: replaced by the save service (player) and the editor level jump (developer).
        [Tooltip("Temporary: 1-based level to load on Play, until the editor level jump exists.")]
        [SerializeField, Min(1)] private int _levelNumber = 1;

        private LevelLoader _levelLoader;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            if (_levelDatabase == null || _boardView == null)
            {
                Debug.LogError($"{name}: level database and board view must be assigned.", this);
                enabled = false;
                return;
            }

            _levelLoader = new LevelLoader();
        }

        private void Start()
        {
            LoadLevel(_levelNumber);
        }

        private void LoadLevel(int levelNumber)
        {
            if (levelNumber > _levelDatabase.Count)
            {
                Debug.LogError($"Level {levelNumber} does not exist; the database holds {_levelDatabase.Count}.", this);
                return;
            }

            try
            {
                LoadedLevel level = _levelLoader.Load(_levelDatabase.GetLevel(levelNumber));
                _boardView.Build(level);
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(e.Message, this);
            }
        }
    }
}
