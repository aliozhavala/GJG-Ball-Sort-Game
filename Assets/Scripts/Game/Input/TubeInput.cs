using System;
using System.Collections.Generic;
using BallSort.Core.Model;
using BallSort.Core.Session;
using BallSort.Game.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BallSort.Game.Input
{
    /// <summary>
    /// Turns taps on the board into selections and moves, through <see cref="LevelSession"/> only.
    /// </summary>
    /// <remarks>
    /// Selection rules (CLAUDE.md): tap a tube to select it, tap it again to deselect, tap another
    /// tube to move there. An illegal move clears the selection and raises <see cref="InvalidMove"/>;
    /// the tapped tube is never auto-selected. Tapping empty space just deselects.
    /// <para>
    /// The selection is also dropped whenever the board changes under it — undo, an added tube
    /// (booster or Play on), restart, entering or leaving the stuck state, and the win — so a
    /// lifted run never refers to a stale board.
    /// </para>
    /// </remarks>
    public sealed class TubeInput : MonoBehaviour
    {
        /// <summary>Value of <see cref="SelectedTube"/> when nothing is selected.</summary>
        public const int NoTube = -1;

        [SerializeField] private Camera _camera;

        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private PointerEventData _pointerData;
        private EventSystem _pointerDataOwner;
        private LevelSession _session;
        private Func<bool> _isModalOpen;

        /// <summary>Index of the selected tube, or <see cref="NoTube"/>.</summary>
        public int SelectedTube { get; private set; } = NoTube;

        /// <summary>Taps are ignored while false, e.g. during the win sequence.</summary>
        public bool IsAcceptingTaps { get; set; }

        /// <summary>Raised when the selection changes, with the new tube index or <see cref="NoTube"/>.</summary>
        public event Action<int> SelectionChanged;

        /// <summary>
        /// Raised with the tapped tube when a tap could not be acted on: the tube cannot be selected,
        /// or the move onto it is illegal.
        /// </summary>
        public event Action<int> InvalidMove;

        /// <param name="session">The session to play moves on.</param>
        /// <param name="isModalOpen">True while a popup covers the board; taps are then ignored.</param>
        public void Initialize(LevelSession session, Func<bool> isModalOpen)
        {
            Unsubscribe();
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _isModalOpen = isModalOpen ?? throw new ArgumentNullException(nameof(isModalOpen));

            _session.MoveUndone += OnMoveUndone;
            _session.TubeAdded += OnTubeAdded;
            _session.Shuffled += ClearSelection;
            _session.Restarted += ClearSelection;
            _session.StuckChanged += OnStuckChanged;
            _session.LevelCompleted += ClearSelection;
        }

        public void ClearSelection() => SetSelection(NoTube);

        private void Update()
        {
            // Input polling only; the decisions are made by the session.
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (_session == null || !IsAcceptingTaps || _isModalOpen())
            {
                return;
            }

            Vector2 screenPosition = pointer.position.ReadValue();
            if (IsOverUi(screenPosition))
            {
                return;
            }

            HandleTap(FindTube(screenPosition));
        }

        private void HandleTap(int tubeIndex)
        {
            if (tubeIndex == NoTube || tubeIndex == SelectedTube)
            {
                ClearSelection();
                return;
            }

            if (SelectedTube == NoTube)
            {
                if (_session.CanSelect(tubeIndex))
                {
                    SetSelection(tubeIndex);
                }
                else
                {
                    InvalidMove?.Invoke(tubeIndex);
                }

                return;
            }

            // Move first and clear after, so the views move the lifted balls from where they are.
            bool moved = _session.TryMove(SelectedTube, tubeIndex, out _);
            ClearSelection();
            if (!moved)
            {
                InvalidMove?.Invoke(tubeIndex);
            }
        }

        private void SetSelection(int tubeIndex)
        {
            if (tubeIndex == SelectedTube)
            {
                return;
            }

            SelectedTube = tubeIndex;
            SelectionChanged?.Invoke(tubeIndex);
        }

        private int FindTube(Vector2 screenPosition)
        {
            Vector2 world = _camera.ScreenToWorldPoint(screenPosition);
            Collider2D hit = Physics2D.OverlapPoint(world);
            return hit != null && hit.TryGetComponent(out TubeView tube) ? tube.TubeIndex : NoTube;
        }

        /// <summary>
        /// Raycasts the UI directly instead of asking the EventSystem about the pointer, whose
        /// answer can lag a frame behind a new touch.
        /// </summary>
        private bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_pointerDataOwner != eventSystem)
            {
                _pointerData = new PointerEventData(eventSystem);
                _pointerDataOwner = eventSystem;
            }

            _pointerData.position = screenPosition;
            _uiHits.Clear();
            eventSystem.RaycastAll(_pointerData, _uiHits);
            return _uiHits.Count > 0;
        }

        private void OnMoveUndone(MoveEventArgs _) => ClearSelection();

        private void OnTubeAdded(TubeAddedEventArgs _) => ClearSelection();

        private void OnStuckChanged(bool _) => ClearSelection();

        private void Unsubscribe()
        {
            if (_session == null)
            {
                return;
            }

            _session.MoveUndone -= OnMoveUndone;
            _session.TubeAdded -= OnTubeAdded;
            _session.Shuffled -= ClearSelection;
            _session.Restarted -= ClearSelection;
            _session.StuckChanged -= OnStuckChanged;
            _session.LevelCompleted -= ClearSelection;
            _session = null;
        }

        private void Awake()
        {
            if (_camera == null)
            {
                Debug.LogError($"{name}: camera must be assigned.", this);
                enabled = false;
            }
        }

        private void OnDestroy() => Unsubscribe();
    }
}
