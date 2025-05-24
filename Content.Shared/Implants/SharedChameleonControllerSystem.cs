using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public abstract partial class SharedChameleonControllerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonControllerOpenMenuEvent>(OpenUI);
    }

    private void OpenUI(ChameleonControllerOpenMenuEvent ev)
    {
        var implant = ev.Action.Comp.Container;

        if (!HasComp<ChameleonControllerImplantComponent>(implant))
            return;

        if (!_uiSystem.HasUi(implant.Value, ChameleonControllerKey.Key))
            return;

        _uiSystem.OpenUi(implant.Value, ChameleonControllerKey.Key, ev.Performer);
        _uiSystem.SetUiState(implant.Value, ChameleonControllerKey.Key, new ChameleonControllerBuiState());
    }

    /// <inheritdoc cref="IsValidJob(JobPrototype)"/>
    public bool IsValidJob(ProtoId<JobPrototype> job, out JobPrototype jobProto)
    {
        jobProto = _proto.Index(job);
        return IsValidJob(jobProto);
    }

    /// <summary>
    /// Make sure the job is "valid" to display in the chameleon controller.
    /// It should basically be just round start jobs.
    /// </summary>
    /// <returns>True if the job is displayable by the chameleon implant.</returns>
    public bool IsValidJob(JobPrototype job)
    {
        return job.StartingGear != null && _proto.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID));
    }
}
