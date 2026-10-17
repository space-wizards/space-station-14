using Content.Shared.Wires;

namespace Content.Shared.EntityEffects.Effects.Construction;

public sealed partial class SetWiresPanelSecurityEntityEffectSystem : EntityEffectSystem<WiresPanelSecurityComponent, SetWiresPanelSecurity>
{
    [Dependency] private EntityManager _entMan = default!;

    protected override void Effect(Entity<WiresPanelSecurityComponent> entity, ref EntityEffectEvent<SetWiresPanelSecurity> args)
    {
        var ev = new WiresPanelSecurityEvent(args.Effect.Examine, args.Effect.WiresAccessible);
        _entMan.EventBus.RaiseLocalEvent(entity.Owner, ev);
    }
}

public sealed partial class SetWiresPanelSecurity : EntityEffectBase<SetWiresPanelSecurity>
{
    /// <summary>
    ///  Sets the Examine field on the entity's <see cref="WiresPanelSecurityComponent"/>
    /// </summary>
    [DataField]
    public string Examine = string.Empty;

    /// <summary>
    ///  Sets the WiresAccessible field on the entity's <see cref="WiresPanelSecurityComponent"/>
    /// </summary>
    [DataField]
    public bool WiresAccessible = true;
}
