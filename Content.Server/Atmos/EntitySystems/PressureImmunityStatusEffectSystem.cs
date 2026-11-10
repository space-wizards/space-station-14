using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Responds to pressure immunity refreshes for the active status effect.
/// </summary>
public sealed partial class PressureImmunityStatusEffectSystem : EntitySystem
{
    public static readonly EntProtoId PressureImmunityEffect = "StatusEffectPressureImmunity";

    [Dependency] private BarotraumaSystem _barotrauma = default!;

    [SubscribeLocalEvent]
    private void OnPressureImmunityStatusApplied(Entity<PressureImmunityStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _barotrauma.RefreshPressureImmunity(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnPressureImmunityStatusRemoved(Entity<PressureImmunityStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _barotrauma.RefreshPressureImmunity(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRefreshPressureImmunity(Entity<PressureImmunityStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshPressureImmunityEvent> args)
    {
        args.Args = args.Args with { IsImmune = true };
    }
}
