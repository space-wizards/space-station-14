using Robust.Shared.Player;

namespace Content.Shared.Players;

/// <summary>
///     To be used from some systems.
///     Otherwise, use <see cref="ISharedPlayerManager"/>
/// </summary>
public abstract class SharedPlayerSystem : EntitySystem
{
    public abstract ContentPlayerData? ContentData(ICommonSession? session);
}
