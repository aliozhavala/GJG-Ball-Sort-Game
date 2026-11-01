using UnityEngine;

namespace BallSort.Game.Views
{
    /// <summary>
    /// One ball's hop from tube to tube (docs/animation-spec.md, 2.3), or within one tube (a shuffle,
    /// 4): rise above the source's mouth, arc to above the target's mouth, drop into the slot. Points and
    /// heights are in the target tube's ball space.
    /// </summary>
    public readonly struct BallFlight
    {
        public BallFlight(
            int sourceTube, FlightKind kind, float delay, Vector3 risePoint, Vector3 arcEnd, Vector3 slot,
            float arcDuration, float arcHeight, float screenWidth, float sourceClearHeight, float targetClearHeight,
            float speed = 1f)
        {
            SourceTube = sourceTube;
            Kind = kind;
            Speed = speed;
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

        /// <summary>Move, undo or shuffle: only a move's landing is heard.</summary>
        public FlightKind Kind { get; }

        /// <summary>Plays the whole flight this many times faster (a booster's flights: 1.7).</summary>
        public float Speed { get; }

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
