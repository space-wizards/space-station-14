using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.RussStation.Surgery.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.RussStation.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryDrapedComponent, ComponentShutdown>(OnDrapedShutdown);
        SubscribeLocalEvent<ActiveSurgeryComponent, ExaminedEvent>(OnActiveSurgeryExamined);
        SubscribeLocalEvent<SurgeryDrapedComponent, ExaminedEvent>(OnDrapedExamined);
    }

    private void OnActiveSurgeryExamined(Entity<ActiveSurgeryComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ProcedureId != null &&
            ProtoManager.TryIndex<SurgeryProcedurePrototype>(ent.Comp.ProcedureId.Value, out var proto))
        {
            args.PushMarkup(Loc.GetString("surgery-examine-active",
                ("target", ent.Owner), ("procedure", Loc.GetString(proto.Name))));
        }
    }

    private void OnDrapedExamined(Entity<SurgeryDrapedComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<ActiveSurgeryComponent>(ent))
            args.PushMarkup(Loc.GetString("surgery-examine-draped", ("target", ent.Owner)));
    }

    private void OnDrapedShutdown(Entity<SurgeryDrapedComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, "SurgeryDraped");

        // Drop bedsheet when draping is removed
        if (ent.Comp.Bedsheet is not { } bedsheet || !Exists(bedsheet))
            return;

        var xformSys = EntityManager.System<SharedTransformSystem>();
        xformSys.DropNextTo(bedsheet, ent.Owner);
    }

    /// <summary>
    /// Checks if the given tool entity has the required tag for a surgery step.
    /// </summary>
    public bool ToolMatchesStep(EntityUid tool, SurgeryStep step)
    {
        return _tag.HasTag(tool, step.Tag);
    }

    /// <summary>
    /// Gets the speed modifier from the surface the patient is buckled to, if any.
    /// </summary>
    public float GetSurfaceSpeedModifier(EntityUid patient)
    {
        if (!TryComp<BuckleComponent>(patient, out var buckle) || buckle.BuckledTo is not { } strap)
            return 1f;

        if (!TryComp<SurgerySurfaceComponent>(strap, out var surface))
            return 1f;

        return surface.SpeedModifier;
    }

    /// <summary>
    /// Gets the effective DoAfter duration for a surgery step.
    /// </summary>
    public TimeSpan GetStepDuration(SurgeryStep step, EntityUid patient)
    {
        return TimeSpan.FromSeconds(step.Duration * GetSurfaceSpeedModifier(patient));
    }

    /// <summary>
    /// Checks if the tool has the Cautery tag for universal close.
    /// </summary>
    public bool IsCauteryTool(EntityUid tool)
    {
        return _tag.HasTag(tool, "Cautery");
    }
}
