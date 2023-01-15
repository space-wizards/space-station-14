using Content.Server.Power.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Wires;

namespace Content.Server.Wires;

/// <summary><see cref="IWireAction" /></summary>
[ImplicitDataDefinitionForInheritors]
public abstract class BaseWireAction : IWireAction
{
    private ISharedAdminLogManager _adminLogger = default!;

    /// <summary>
    ///     Default name that gets returned by <see cref="GetStatusLightData(Wire)"/>. Also used for admin logging.
    /// </summary>
    [DataField("name")]
    public abstract string Name { get; set; }

    /// <summary>
    ///     Default color that gets returned by <see cref="GetStatusLightData(Wire)"/>.
    /// </summary>
    [DataField("color")]
    public abstract Color Color { get; set; }

    /// <summary>
    ///     If true, the default behavior of <see cref="GetStatusLightData(Wire)"/> will return an off-light when the
    ///     machine is not powered.
    /// </summary>
    [DataField("requirePower")]
    public virtual bool RequirePower { get; set; } = true;

    public virtual StatusLightData? GetStatusLightData(Wire wire)
    {
        if (RequirePower && !IsPowered(wire.Owner))
            return new StatusLightData(Color, StatusLightState.Off, Name);

        var state = GetLightState(wire);
        return state == null
            ? null
            : new StatusLightData(Color, state.Value, Name);
    }

    public virtual StatusLightState? GetLightState(Wire wire) => null;

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
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{EntityManager.ToPrettyString(user):player} cut {wire.Color.Name()} {Name} in {EntityManager.ToPrettyString(wire.Owner)}");
        return false;
    }
    public virtual bool Mend(EntityUid user, Wire wire)
    {
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{EntityManager.ToPrettyString(user):player} mended {wire.Color.Name()} {Name} in {EntityManager.ToPrettyString(wire.Owner)}");
        return false;
    }
    public virtual bool Pulse(EntityUid user, Wire wire)
    {
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{EntityManager.ToPrettyString(user):player} pulsed {wire.Color.Name()} {Name} in {EntityManager.ToPrettyString(wire.Owner)}");
        return false;
    }
    public virtual void Update(Wire wire)
    {
    }

    /// <summary>
    ///     Utility function to check if this given entity is powered.
    /// </summary>
    /// <returns>true if powered, false otherwise</returns>
    protected bool IsPowered(EntityUid uid)
    {
        return WiresSystem.IsPowered(uid, EntityManager);
    }
}
