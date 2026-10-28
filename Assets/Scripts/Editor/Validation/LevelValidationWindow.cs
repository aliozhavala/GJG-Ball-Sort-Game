using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BallSort.Editor.Tools;
using BallSort.Game.Services;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Validation
{
    /// <summary>
    /// Ball Sort ▸ Validate Levels…: validates and solves every level in a <see cref="LevelDatabase"/>
    /// and can write the proven optimum back into each file's <c>meta.optimalMoves</c>.
    /// </summary>
    /// <remarks>
    /// The solving runs on a worker thread so the Editor stays responsive; a cancelable progress
    /// bar polls it from <see cref="EditorApplication.update"/>. Rows appear as levels finish.
    /// </remarks>
    public sealed class LevelValidationWindow : EditorWindow
    {
        private static readonly GUILayoutOption[] Columns =
        {
            GUILayout.Width(80), GUILayout.Width(100), GUILayout.Width(150), GUILayout.Width(70),
            GUILayout.Width(70), GUILayout.Width(130), GUILayout.Width(110), GUILayout.Width(50),
        };

        private static readonly string[] Headers =
        {
            "Level", "Colours / tubes", "Mechanics", "Solvable", "Optimal", "Nodes (fast / opt)", "Time (fast / opt)", string.Empty,
        };

        [SerializeField] private LevelDatabase _database;
        [SerializeField] private int _fastNodeLimit = LevelValidationRunner.DefaultFastNodeLimit;
        [SerializeField] private int _optimalNodeLimit = LevelValidationRunner.DefaultOptimalNodeLimit;

        // Filled by the worker thread; read under the lock.
        private readonly List<LevelReport> _reports = new List<LevelReport>();
        private Task _run;
        private CancellationTokenSource _cancellation;
        private volatile string _currentLevel;
        private volatile int _finished;
        private int _total;
        private double _startTime;
        private string _status;
        private Vector2 _scroll;

        private bool IsRunning => _run != null;

        [MenuItem("Ball Sort/Validate Levels…")]
        public static void Open() => GetWindow<LevelValidationWindow>("Validate Levels");

        private void OnEnable()
        {
            if (_database == null)
            {
                _database = LevelDatabaseFinder.FindFirst();
            }
        }

        private void OnDisable()
        {
            // Also runs before a domain reload: stop the worker so it does not outlive its assembly.
            _cancellation?.Cancel();
            if (IsRunning)
            {
                EditorApplication.update -= PollRun;
                EditorUtility.ClearProgressBar();
            }
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(IsRunning))
            {
                _database = (LevelDatabase)EditorGUILayout.ObjectField("Level database", _database, typeof(LevelDatabase), false);
                _fastNodeLimit = Mathf.Max(1, EditorGUILayout.IntField("Fast node limit", _fastNodeLimit));
                _optimalNodeLimit = Mathf.Max(1, EditorGUILayout.IntField("Optimal node limit", _optimalNodeLimit));

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(_database == null))
                {
                    if (GUILayout.Button("Validate all levels"))
                    {
                        StartRun();
                    }
                }

                using (new EditorGUI.DisabledScope(!HasOptimalMoves()))
                {
                    if (GUILayout.Button("Write meta.optimalMoves"))
                    {
                        WriteOptimalMoves();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }

            DrawTable();
        }

        private void DrawTable()
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < Headers.Length; i++)
            {
                GUILayout.Label(Headers[i], EditorStyles.boldLabel, Columns[i]);
            }

            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (LevelReport report in SnapshotReports())
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(report.Source.Name, Columns[0]);
                GUILayout.Label(report.IsValid ? $"{report.Colors} / {report.Tubes}" : "–", Columns[1]);
                GUILayout.Label(report.Mechanics, Columns[2]);
                GUILayout.Label(report.SolvableText, Columns[3]);
                GUILayout.Label(report.OptimalText, Columns[4]);
                GUILayout.Label(report.NodesText, Columns[5]);
                GUILayout.Label(report.TimeText, Columns[6]);
                using (new EditorGUI.DisabledScope(
                           IsRunning || EditorApplication.isPlayingOrWillChangePlaymode || report.Source.AssetPath == null))
                {
                    if (GUILayout.Button("Play", Columns[7]))
                    {
                        int levelNumber = report.Source.Number;
                        EditorApplication.delayCall += () => PlayLevelWindow.Play(levelNumber);
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (!report.IsValid)
                {
                    EditorGUILayout.HelpBox(string.Join("\n", report.Errors), MessageType.Error);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void StartRun()
        {
            // Asset text is read here, on the main thread; the worker only sees plain strings.
            List<LevelSource> sources = LevelValidationRunner.ReadSources(_database);
            int fastLimit = _fastNodeLimit;
            int optimalLimit = _optimalNodeLimit;

            lock (_reports)
            {
                _reports.Clear();
            }

            _cancellation = new CancellationTokenSource();
            CancellationToken token = _cancellation.Token;
            _total = sources.Count;
            _finished = 0;
            _status = null;
            _startTime = EditorApplication.timeSinceStartup;

            _run = Task.Run(() =>
            {
                foreach (LevelSource source in sources)
                {
                    token.ThrowIfCancellationRequested();
                    _currentLevel = source.Name;
                    LevelReport report = LevelValidationRunner.Check(source, fastLimit, optimalLimit, token);
                    lock (_reports)
                    {
                        _reports.Add(report);
                    }

                    _finished++;
                }
            }, token);

            EditorApplication.update += PollRun;
        }

        private void PollRun()
        {
            if (!_run.IsCompleted)
            {
                string info = $"{_currentLevel} ({Math.Min(_finished + 1, _total)} of {_total})";
                if (EditorUtility.DisplayCancelableProgressBar("Validate Levels", info, (float)_finished / Math.Max(1, _total)))
                {
                    _cancellation.Cancel();
                }

                Repaint();
                return;
            }

            EditorApplication.update -= PollRun;
            EditorUtility.ClearProgressBar();

            double seconds = EditorApplication.timeSinceStartup - _startTime;
            if (_run.IsCanceled || _cancellation.IsCancellationRequested)
            {
                _status = $"Cancelled after {_finished} of {_total} levels.";
            }
            else if (_run.IsFaulted)
            {
                _status = "Validation failed; see the console.";
                Debug.LogException(_run.Exception?.GetBaseException(), this);
            }
            else
            {
                _status = $"Checked {_total} levels in {seconds:0.0} s.";
            }

            _run = null;
            _cancellation.Dispose();
            _cancellation = null;
            Repaint();
        }

        private void WriteOptimalMoves()
        {
            int written = 0;
            int upToDate = 0;
            var failures = new List<string>();
            foreach (LevelReport report in SnapshotReports())
            {
                if (!report.OptimalMoves.HasValue || report.Source.AssetPath == null)
                {
                    continue;
                }

                string path = Path.GetFullPath(report.Source.AssetPath);
                string json = File.ReadAllText(path);
                if (!OptimalMovesWriter.TryWrite(json, report.Source.Name, report.OptimalMoves.Value, out string updated, out string error))
                {
                    failures.Add(error);
                    continue;
                }

                if (updated == json)
                {
                    upToDate++;
                    continue;
                }

                // Level files are UTF-8 without a byte order mark.
                File.WriteAllText(path, updated, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(report.Source.AssetPath);
                written++;
            }

            _status = $"meta.optimalMoves: {written} file(s) written, {upToDate} already up to date" +
                      (failures.Count > 0 ? $", {failures.Count} refused (see the console)." : ".");
            foreach (string failure in failures)
            {
                Debug.LogError(failure, this);
            }
        }

        private bool HasOptimalMoves()
        {
            foreach (LevelReport report in SnapshotReports())
            {
                if (report.OptimalMoves.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private List<LevelReport> SnapshotReports()
        {
            lock (_reports)
            {
                return new List<LevelReport>(_reports);
            }
        }
    }
}
