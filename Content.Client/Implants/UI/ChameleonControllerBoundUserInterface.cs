using System.Linq;
using Content.Shared.Implants;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants.UI;

[UsedImplicitly]
public sealed class ChameleonControllerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [ViewVariables]
    private ChameleonControllerMenu? _menu;

    public ChameleonControllerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ChameleonControllerMenu>();
        _menu.OnIdSelected += OnIdSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ChameleonControllerBuiState)
            return;

        var targets = _prototypeManager.EnumeratePrototypes<JobPrototype>();
        var validList = new List<JobPrototype>();
        foreach (var target in targets)
        {
            if (target.StartingGear == null || !_prototypeManager.HasIndex<RoleLoadoutPrototype>("Job" + target.ID))
                continue;

            validList.Add(target);
        }
        _menu?.UpdateState(validList.AsEnumerable());
    }

    private void OnIdSelected(ProtoId<JobPrototype> selectedJob)
    {
        SendMessage(new ChameleonControllerSelectedJobMessage(selectedJob));
    }
}
