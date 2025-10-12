
namespace Content.Shared.Mobs;

/// <see cref="DeathgaspComponent"/>
public partial class SharedDeathgaspSystem : EntitySystem
{
    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public virtual bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        return true;
    }
}
