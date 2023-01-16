using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Wires;

namespace Content.Server.Wires;

/// <summary><see cref="IWireAction" /></summary>
public abstract class BaseWireAction : IWireAction
{
    private ISharedAdminLogManager _adminLogger = default!;
    protected virtual string Text
    {
        get => GetType().Name.Replace("WireAction", "");
        set { }
    }

    public IEntityManager EntityManager = default!;
    public WiresSystem WiresSystem = default!;

    // not virtual so implementors are aware that they need a nullable here
    public abstract object? StatusKey { get; }

    // ugly, but IoC doesn't work during deserialization
    public virtual void Initialize()
    {
        EntityManager = IoCManager.Resolve<IEntityManager>();
        _adminLogger = IoCManager.Resolve<ISharedAdminLogManager>();

        WiresSystem = EntityManager.EntitySysManager.GetEntitySystem<WiresSystem>();
    }

    public virtual bool AddWire(Wire wire, int count) => count == 1;
    public virtual bool Cut(EntityUid user, Wire wire)
    {
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{EntityManager.ToPrettyString(user):player} cut {wire.Color.Name()} {Text} in {EntityManager.ToPrettyString(wire.Owner)}");
        return false;
    }
    public virtual bool Mend(EntityUid user, Wire wire)
    {
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{EntityManager.ToPrettyString(user):player} mended {wire.Color.Name()} {Text} in {EntityManager.ToPrettyString(wire.Owner)}");
        return false;
    }
    public virtual bool Pulse(EntityUid user, Wire wire)
    {
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{EntityManager.ToPrettyString(user):player} pulsed {wire.Color.Name()} {Text} in {EntityManager.ToPrettyString(wire.Owner)}");
        return false;
    }
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
