using Content.Shared.Implants;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants.UI;

public sealed class DeimplantBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protomanager = default!;

    [ViewVariables]
    private DeimplantChoiceWindow? _window;

    public DeimplantBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DeimplantChoiceWindow>();

        _window.OnImplantChange += implant => SendMessage(new DeimplantChangeVerbMessage(implant));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not DeimplantBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateImplantList(cast.ImplantList);
        _window.UpdateState(cast.Implant);
    }
}
