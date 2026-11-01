namespace BallSort.Game.UI
{
    /// <summary>The Win panel's steps that feedback follows (docs/animation-spec.md, 5.2; SFX guide 6.1).</summary>
    public enum WinStep
    {
        /// <summary>The screen dims and the win picture pops in.</summary>
        Overlay,

        /// <summary>The title starts to pop in, letter by letter.</summary>
        Card,

        /// <summary>The title's last letter lands.</summary>
        TitleStamp,

        /// <summary>The reward line pops in (only when the win pays a reward).</summary>
        Reward,
    }
}
