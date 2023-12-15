using Content.Server.Power.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Wires;

namespace Content.Server.Wires;

/// <summary><see cref="IWireAction" /></summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseWireAction : IWireAction
{
    private ISharedAdminLogManager _adminLogger = default!;

    /// <summary>
    ///     The loc-string of the text that gets returned by <see cref="GetStatusLightData(Wire)"/>. Also used for admin logging.
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
    ///     wire owner is not powered.
    /// </summary>
    [DataField("lightRequiresPower")]
    public virtual bool LightRequiresPower { get; set; } = true;

    public virtual StatusLightData? GetStatusLightData(Wire wire)
    {
        if (LightRequiresPower && !IsPowered(wire.Owner))
            return new StatusLightData(Color, StatusLightState.Off, Loc.GetString(Name));

        var state = GetLightState(wire);
        return state == null
            ? null
            : new StatusLightData(Color, state.Value, Loc.GetString(Name));
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
    public virtual bool Cut(EntityUid user, Wire wire) => Log(user, wire, "cut");
    public virtual bool Mend(EntityUid user, Wire wire) => Log(user, wire, "mended");
    public virtual void Pulse(EntityUid user, Wire wire) => Log(user, wire, "pulsed");

    private bool Log(EntityUid user, Wire wire, string verb)
    {
        var player = EntityManager.ToPrettyString(user);
        var owner = EntityManager.ToPrettyString(wire.Owner);
        var name = Loc.GetString(Name);
        var color = wire.Color.Name();
        var action = GetType().Name;

        // logs something like "... mended red POWR wire (PowerWireAction) in ...."
        _adminLogger.Add(LogType.WireHacking, LogImpact.Medium, $"{player} {verb} {color} {name} wire ({action}) in {owner}");
        return true;
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
