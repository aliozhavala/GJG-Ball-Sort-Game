using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One ball's hop from tube to tube (docs/animation-spec.md, 2.3): rise above the source's mouth, arc
    /// to above the target's mouth, drop into the slot. Points and heights are in the target tube's
    /// ball space.
    /// </summary>
    public readonly struct BallFlight
    {
        public BallFlight(
            int sourceTube, bool isUndo, float delay, Vector3 risePoint, Vector3 arcEnd, Vector3 slot,
            float arcDuration, float arcHeight, float screenWidth, float sourceClearHeight, float targetClearHeight)
        {
            SourceTube = sourceTube;
            IsUndo = isUndo;
            Delay = delay;
            RisePoint = risePoint;
            ArcEnd = arcEnd;
            Slot = slot;
            ArcDuration = arcDuration;
            ArcHeight = arcHeight;
            ScreenWidth = screenWidth;
            SourceClearHeight = sourceClearHeight;
            TargetClearHeight = targetClearHeight;
        }

        /// <summary>Board index of the tube the ball leaves.</summary>
        public int SourceTube { get; }

        /// <summary>True when an undo carries the ball back (it lands without a sound).</summary>
        public bool IsUndo { get; }

        /// <summary>Wait before the ball starts: its place in the move's chain.</summary>
        public float Delay { get; }

        /// <summary>The lift point above the source's mouth.</summary>
        public Vector3 RisePoint { get; }

        /// <summary>The lift point above the target's mouth.</summary>
        public Vector3 ArcEnd { get; }

        /// <summary>The ball's slot in the target.</summary>
        public Vector3 Slot { get; }

        public float ArcDuration { get; }

        /// <summary>Height of the arc above the straight line, in board units.</summary>
        public float ArcHeight { get; }

        /// <summary>Screen width in board units: the stretch is measured in screen widths per second.</summary>
        public float ScreenWidth { get; }

        /// <summary>Above this height the rising ball is out of the source's mouth (<see cref="TubeView.ClearHeight"/>).</summary>
        public float SourceClearHeight { get; }

        /// <summary>Below this height the dropping ball is inside the target's mouth.</summary>
        public float TargetClearHeight { get; }
    }
}
