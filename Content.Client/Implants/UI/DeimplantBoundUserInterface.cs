using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Whitelist;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants.UI;

public sealed partial class DeimplantBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

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

        _window.OnStartDeimplant += (target, user) =>
        {
            SendPredictedMessage(new DeimplantTargetStartVerbMessage(
                EntMan.GetNetEntity(target),
                EntMan.GetNetEntity(user))
            );

            _window.Close();
        };
    }

    public override void Update()
    {
        if (!EntMan.TryGetComponent<ImplanterComponent>(Owner, out var implanterComp)
            || implanterComp.TargetToDrawImplant == null
            || implanterComp.UserTrigger == null)
            return;

        if (!EntMan.TryGetComponent<ImplantedComponent>(implanterComp.TargetToDrawImplant, out var implantedComp))
            return;

        Dictionary<string, string> implants = new();

        foreach (var implanter in implantedComp.ImplantContainer.ContainedEntities)
        {
            if (!_whitelist.IsValid(implanterComp.DeimplantWhitelist, implanter))
                continue;

            var metaDataComponent = EntMan.GetComponent<MetaDataComponent>(implanter);

            if (metaDataComponent.EntityPrototype == null)
                continue;

            var prototype = _proto.Index<EntityPrototype>(metaDataComponent.EntityPrototype.ID);

            implants.Add(prototype.ID, prototype.Name);
        }

        if (_window != null)
        {
            _window.UpdateImplantList(implants);
            _window.UpdateState(implanterComp.DeimplantChosen, implanterComp.TargetToDrawImplant, implanterComp.UserTrigger);
        }
    }
}
