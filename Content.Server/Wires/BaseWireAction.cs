using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Wires;

namespace Content.Server.Wires;

/// <summary><see cref="IWireAction" /></summary>
public abstract class BaseWireAction : IWireAction
{
    public IEntityManager EntityManager = default!;
    public WiresSystem WiresSystem = default!;

    // not virtual so implementors are aware that they need a nullable here
    public abstract object? StatusKey { get; }

    // ugly, but IoC doesn't work during deserialization
    public virtual void Initialize()
    {
        EntityManager = IoCManager.Resolve<IEntityManager>();

        WiresSystem = EntityManager.EntitySysManager.GetEntitySystem<WiresSystem>();
    }

    public virtual bool AddWire(Wire wire, int count) => count == 1;
    public abstract bool Cut(EntityUid user, Wire wire);
    public abstract bool Mend(EntityUid user, Wire wire);
    public abstract bool Pulse(EntityUid user, Wire wire);
    public virtual void Update(Wire wire)
    {
        return;
    }
    public abstract StatusLightData? GetStatusLightData(Wire wire);

    // most things that use wires are powered by *something*, so
    //
    // this isn't required by any wire system methods though, so whatever inherits it here
    // can use it
    /// <summary>
    ///     Utility function to check if this given entity is powered.
    /// </summary>
    /// <returns>true if powered, false otherwise</returns>
    protected bool IsPowered(EntityUid uid)
    {
        return WiresSystem.IsPowered(uid, EntityManager);
    }
}
