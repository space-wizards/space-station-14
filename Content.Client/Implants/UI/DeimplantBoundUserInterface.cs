using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants.UI;

public sealed partial class DeimplantBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;

    [ViewVariables]
    private DeimplantChoiceWindow? _window;

    public DeimplantBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DeimplantChoiceWindow>();

        _window.OnImplantChange += implant => SendPredictedMessage(new DeimplantChangeVerbMessage(implant));
    }

    public override void Update()
    {
        if (!EntMan.TryGetComponent<ImplanterComponent>(Owner, out var implanterComp))
            return;

        // TODO: Don't use protoId for deimplanting
        // and especially not raw strings!
        Dictionary<string, string> implants = new();
        foreach (var implant in implanterComp.DeimplantWhitelist)
        {
            if (_proto.Resolve(implant, out var proto))
                implants.Add(proto.ID, proto.Name);
        }
        if (_window != null)
        {
            _window.UpdateImplantList(implants);
            _window.UpdateState(implanterComp.DeimplantChosen);
        }
    }
}
