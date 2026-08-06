using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Examine;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires a specific chemical reagent
/// Variant of normal reactive system differentiated by providing a hint for its specific reagent
/// Means you don't have to have a million specific reactiveeffect triggers for one reagent each
/// </summary>
public sealed partial class XATReactiveSpecificSystem : BaseXATSystem<XATReactiveSpecificComponent>
{
    [Dependency] private FlavorProfileSystem _flavor = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedXenoArtifactSystem _xenoArch = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XATReactiveSpecificComponent, MapInitEvent>(OnMapInit);
        XATSubscribeDirectEvent<ReactionEntityEvent>(OnReaction);
        XATSubscribeDirectEvent<ExaminedEvent>(OnExamine);
    }

    /// <summary>
    /// Decide random reagent
    /// Set tip to specify said random reagent
    /// </summary>
    private void OnMapInit(Entity<XATReactiveSpecificComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Reagents == null || ent.Comp.Reagent != null)
            return;

        ent.Comp.Reagent = ent.Comp.Reagents.ElementAt(_random.Next(ent.Comp.Reagents.Count)); //get random reagent from hashset

        if (TryComp<XenoArtifactNodeComponent>(ent.Owner, out var nodeComp))
        {
            var index = _proto.Index(ent.Comp.Reagent);
            _xenoArch.SetNodeTip(nodeComp, Loc.GetString("xenoarch-trigger-tip-reactive-specific", ("reagent", index.LocalizedName)));
        }
    }

    /// <summary>
    /// Cutdown version of Regular XATReactiveSystem, but only for one reagent
    /// </summary>
    private void OnReaction(Entity<XenoArtifactComponent> artifact, Entity<XATReactiveSpecificComponent, XenoArtifactNodeComponent> node, ref ReactionEntityEvent args)
    {
        var reactiveTriggerComponent = node.Comp1;
        if (!reactiveTriggerComponent.ReactionMethods.Contains(args.Method))
            return;

        if (args.ReagentQuantity.Quantity < reactiveTriggerComponent.MinQuantity)
            return;

        if (_proto.Index(reactiveTriggerComponent.Reagent) != args.Reagent)
            return;

        Trigger(artifact, node);
    }

    /// <summary>
    /// Dynamic hint for specific reagent, gives exact reagent if it is normally recognisable or the user can see all reagents
    /// </summary>
    private void OnExamine(Entity<XenoArtifactComponent> artifact, Entity<XATReactiveSpecificComponent, XenoArtifactNodeComponent> node, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !node.Comp1.Examinable)
            return;

        var index = _proto.Index(node.Comp1.Reagent);
        if (index == null)
            return;

        var scanEvent = new SolutionScanEvent(); //check to see if the examiner can see specific reagents, such as through chemical analysis goggles
        RaiseLocalEvent(args.Examiner, scanEvent);
        if (scanEvent.CanScan || index.Recognizable) //if user has can see reagents, or the reagent is innately recognisable, then show the user directly, otherwise they must figure it out
        {
            args.PushMarkup(Loc.GetString("xenoarch-trigger-examine-reagent-specific-scan", ("color", index.SubstanceColor), ("reagent", index.LocalizedName)));
        }
        else
        {
            var flavor = _flavor.GetLocalizedFlavorsMessage(node.Owner, new Solution(index.ID, 100));
            args.PushMarkup(Loc.GetString("xenoarch-trigger-examine-reagent-specific", ("color", index.SubstanceColor), ("description", index.LocalizedPhysicalDescription), ("flavor", flavor)));
        }


    }
}
