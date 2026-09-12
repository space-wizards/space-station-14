using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that starts Foam chemical reaction with random-ish reagents inside.
/// </summary>
public sealed partial class XAEFoamSystem : BaseXAESystem<XAEFoamComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEFoamComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, XAEFoamComponent component, MapInitEvent args)
    {
        if (component.SelectedReagent != null)
            return;

        if (component.Reagents.Count == 0)
            return;

        component.SelectedReagent = _random.Pick(component.Reagents);

        if (component.ReplaceDescription)
        {
            var reagent = ProtoMan.Index<ReagentPrototype>(component.SelectedReagent);
            var newEntityDescription = Loc.GetString("xenoarch-effect-foam", ("reagent", reagent.LocalizedName));
            _metaData.SetEntityDescription(uid, newEntityDescription);
        }
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEFoamComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        XAEFoamComponent component = ent;
        var foamAmount = component.DefaultFoamAmount;
        var foamAmountRestrictions = component.FoamAmountRestrictions;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var amountModifier))
        {
            foamAmount = Math.Clamp(amountModifier.Modify(foamAmount), foamAmountRestrictions.Min, foamAmountRestrictions.Max);
        }

        var range = component.DefaultRange;
        var rangeRestrictions = component.RangeRestrictions;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Range, out var rangeModifier))
        {
            range = Math.Clamp(rangeModifier.Modify(foamAmount), rangeRestrictions.Min, rangeRestrictions.Max);
        }

        var duration = component.DefaultDuration;
        var durationRestrictions = component.DurationRestrictions;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Duration, out var durationModifier))
        {
            duration = Math.Clamp(durationModifier.Modify(duration), durationRestrictions.Min, durationRestrictions.Max);
        }

        if (component.SelectedReagent == null)
            return;

        var sol = new Solution();
        sol.AddReagent(component.SelectedReagent, foamAmount);
        var foamEnt = Spawn(ChemicalReactionSystem.FoamReaction, args.Coordinates);

        _smoke.StartSmoke(foamEnt, sol, duration, (int)(range * 4));
    }
}
