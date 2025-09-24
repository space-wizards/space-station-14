using Content.Shared.Medical.Disease;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Server.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;

namespace Content.Server.Medical.Disease.Systems;

/// <summary>
/// Handles using a disease sample swab on mobs to collect their active diseases.
/// </summary>
public sealed class DiseaseSwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <inheritdoc/>
	public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiseaseSampleComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DiseaseSampleComponent, DiseaseSwabDoAfterEvent>(OnDoAfter);
    }

    private const float SwabDelaySeconds = 2f;

    /// <summary>
    /// Starts a timed swab action on a living mob when the swab is used.
    /// </summary>
    private void OnAfterInteract(EntityUid uid, DiseaseSampleComponent swab, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not EntityUid target)
            return;

        // Only allow swabbing living mobs
        if (!TryComp<MobStateComponent>(target, out var mobState) || mobState.CurrentState == Content.Shared.Mobs.MobState.Dead)
            return;

        // Don't allow swabbing machines like diagnoser here
        if (HasComp<DiseaseDiagnoserComponent>(target))
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, SwabDelaySeconds, new DiseaseSwabDoAfterEvent(), uid, target: target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// On do-after completion: records the target's active diseases and basic identity info into the swab.
    /// </summary>
    private void OnDoAfter(EntityUid uid, DiseaseSampleComponent swab, DiseaseSwabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        // Read diseases from carrier and overwrite sample
        swab.Diseases.Clear();
        swab.Stages.Clear();
        swab.HasSample = true;
        swab.SubjectName = Identity.Name(target, EntityManager);
        swab.SubjectDNA = null;

        if (TryComp<DnaComponent>(target, out var dna) && !string.IsNullOrWhiteSpace(dna.DNA))
            swab.SubjectDNA = dna.DNA;

        if (TryComp<DiseaseCarrierComponent>(target, out var carrier) && carrier.ActiveDiseases.Count > 0)
        {
            foreach (var (diseaseId, stage) in carrier.ActiveDiseases)
            {
                if (!_prototypes.HasIndex<DiseasePrototype>(diseaseId))
                    continue;
                swab.Diseases.Add(diseaseId);
                swab.Stages[diseaseId] = stage;
            }

            _popup.PopupEntity(Loc.GetString("diagnoser-disease-swab-collected-popup"), target, args.Args.User);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("diagnoser-disease-swab-no-diseases-popup"), target, args.Args.User);
        }

        args.Handled = true;
    }
}
