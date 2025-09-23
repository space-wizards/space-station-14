using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class CleanBloodstreamEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, CleanBloodstream>
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<CleanBloodstream> args)
    {
        var scale = args.Scale * args.Effect.CleanseRate;

        _bloodstream.FlushChemicals((entity, entity), args.Effect.Excluded, scale);
    }
}

public sealed partial class CleanBloodstream : EntityEffectBase<CleanBloodstream>
{
    /// <summary>
    ///     Amount of reagent we're cleaning out of our bloodstream.
    /// </summary>
    [DataField]
    public FixedPoint2 CleanseRate = 3.0f;

    [DataField]
    public ProtoId<ReagentPrototype>? Excluded;
}
