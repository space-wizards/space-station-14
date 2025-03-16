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
    
    public void UpdateState(Dictionary<string, string> implantList, string? implant)
    {
        if (_window != null)
        {
            _window.UpdateImplantList(implantList);
            _window.UpdateState(implant);
        }
    }
}
