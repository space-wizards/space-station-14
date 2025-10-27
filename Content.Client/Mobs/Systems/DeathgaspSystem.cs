using Content.Shared.Mobs;

namespace Content.Client.Mobs;

/// <see cref="DeathgaspComponent"/>
public sealed class DeathgaspSystem : SharedDeathgaspSystem
{
    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public override bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        return true;
    }
}
