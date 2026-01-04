
namespace Content.Shared.Mobs;

/// <see cref="DeathgaspComponent"/>
public abstract class SharedDeathgaspSystem : EntitySystem
{
    /// <summary>
    /// Causes an entity to perform their deathgasp emote, if they have one.
    /// Returns true if successfull.
    /// Always returns true on the client.
    /// </summary>
    public virtual bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        return true;
    }
}
