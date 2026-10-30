namespace BallSort.Game.Audio
{
    /// <summary>Every sound effect of the game (docs/design/SFX_GUIDE.md); each has a slot in <see cref="SfxConfig"/>.</summary>
    public enum Sfx
    {
        BallSelect,
        BallDeselect,
        BallLand,
        MoveInvalid,
        TubeCompleteChime,
        TubeCork,
        HiddenReveal,
        TowelPull,
        LockboxKeyFly,
        LockboxKeyIn,
        LockboxBreak,
        IceBreak,
        BoosterUndo,
        BoosterShuffle,
        BoosterAddTube,
        WinFanfare,
        RewardJingle,
        WinTitleStamp,
        CoinGain,
        UiButton,
        UiPlay,
        LifeLost,
    }
}
