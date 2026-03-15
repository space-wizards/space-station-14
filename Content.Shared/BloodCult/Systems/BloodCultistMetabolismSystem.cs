using System.Linq;
using Content.Shared.BloodCult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.BloodCult;

/// <summary>
/// Changes blood cultists' blood to Unholy Blood.
/// Runs on both client and server for prediction.
/// </summary>
public sealed class BloodCultistMetabolismSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, ComponentShutdown>(OnCultistShutdown);
        SubscribeLocalEvent<BloodCultistComponent, EntityTerminatingEvent>(OnCultistTerminating);
        SubscribeLocalEvent<BloodstreamComponent, ComponentStartup>(OnBloodstreamStartup);
    }

    /// <summary>
    /// Applies unholy blood if the entity is a cultist with a bloodstream. Called from SharedBloodCultistSystem when BloodCultistComponent starts (conversion or admin smite)
    /// </summary>
    public void ApplyUnholyBloodIfCultistWithBloodstream(EntityUid uid)
    {
        if (!TryComp<BloodCultistComponent>(uid, out var cultist) || !TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return;

        ApplyUnholyBlood(uid, cultist, bloodstream);
    }

    /// <summary>
    /// When a bloodstream is set up and the entity is already a cultist (e.g. roundstart with both components), apply unholy blood.
    /// </summary>
    private void OnBloodstreamStartup(Entity<BloodstreamComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<BloodCultistComponent>(ent, out var cultist))
            return;

        ApplyUnholyBlood(ent.Owner, cultist, ent.Comp);
    }

    private void ApplyUnholyBlood(EntityUid uid, BloodCultistComponent component, BloodstreamComponent bloodstream)
    {
        if (string.IsNullOrEmpty(component.OriginalBloodReagent))
        {
            var originalBlood = "Blood";
            if (TryGetPrototypeBloodReagent(uid, out var prototypeBlood, "Blood") && !string.IsNullOrEmpty(prototypeBlood))
                originalBlood = prototypeBlood;
            else if (bloodstream.BloodReferenceSolution.Contents.Count > 0)
                originalBlood = bloodstream.BloodReferenceSolution.Contents.First().Reagent.Prototype.ToString();
            component.OriginalBloodReagent = originalBlood;
        }

        var cultBloodReagent = string.IsNullOrEmpty(component.CultBloodReagent) ? "UnholyBlood" : component.CultBloodReagent;
        try
        {
            var cultBloodSolution = new Solution();
            cultBloodSolution.AddReagent((ProtoId<ReagentPrototype>)cultBloodReagent, FixedPoint2.New(1));
            _bloodstream.ChangeBloodReagents((uid, bloodstream), cultBloodSolution);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to change blood type to {cultBloodReagent} for {ToPrettyString(uid)}: {ex}");
        }
    }

    private void OnCultistTerminating(Entity<BloodCultistComponent> ent, ref EntityTerminatingEvent args)
    {
        RestoreBloodType(ent.Owner, ent.Comp, terminating: true);
    }

    private void OnCultistShutdown(Entity<BloodCultistComponent> ent, ref ComponentShutdown args)
    {
        RestoreBloodType(ent.Owner, ent.Comp, terminating: false);
    }

    private void RestoreBloodType(EntityUid uid, BloodCultistComponent component, bool terminating)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return;

        var restoreReagent = GetRestoreReagent(uid, component);
        try
        {
            var restoreSolution = new Solution();
            restoreSolution.AddReagent((ProtoId<ReagentPrototype>)restoreReagent, FixedPoint2.New(1));
            _bloodstream.ChangeBloodReagents((uid, bloodstream), restoreSolution);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to restore blood type to {restoreReagent} for {ToPrettyString(uid)}{(terminating ? " during termination" : "")}: {ex}");
        }
    }

    private string GetRestoreReagent(EntityUid uid, BloodCultistComponent component)
    {
        var restoreReagent = component.OriginalBloodReagent;
        if (string.IsNullOrEmpty(restoreReagent) && TryGetPrototypeBloodReagent(uid, out var prototypeReagent, "Blood") && !string.IsNullOrEmpty(prototypeReagent))
            restoreReagent = prototypeReagent;
        if (string.IsNullOrEmpty(restoreReagent))
            restoreReagent = "Blood";
        return restoreReagent;
    }

    private bool TryGetPrototypeBloodReagent(EntityUid uid, out string bloodReagent, string? defaultReagent = null)
    {
        bloodReagent = string.IsNullOrEmpty(defaultReagent) ? "Blood" : defaultReagent;

        try
        {
            var meta = MetaData(uid);
            if (meta.EntityPrototype == null)
                return false;

            if (!meta.EntityPrototype.TryGetComponent(_componentFactory.GetComponentName<BloodstreamComponent>(), out BloodstreamComponent? prototypeBloodstream))
                return false;

            if (prototypeBloodstream.BloodReferenceSolution.Contents.Count > 0)
            {
                bloodReagent = prototypeBloodstream.BloodReferenceSolution.Contents.First().Reagent.Prototype.ToString();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning($"Error getting prototype blood reagent for {ToPrettyString(uid)}: {ex}");
            return false;
        }
    }
}
