using System.Linq;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Whitelist;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Implants.UI;

public sealed partial class DeimplantBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IEntityManager _entityManager = default!;
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
                _entityManager.GetNetEntity(target),
                _entityManager.GetNetEntity(user))
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
        List<EntityPrototype> validImplanters = new();

        foreach (var implanter in implantedComp.ImplantContainer.ContainedEntities)
        {
            if (!_whitelist.IsValid(implanterComp.DeimplantWhitelist, implanter))
                continue;

            if (!EntMan.TryGetComponent<MetaDataComponent>(implanter, out var metaComp) || metaComp.EntityPrototype == null)
                continue;

            if (!_proto.TryIndex(metaComp.EntityPrototype.ID, out var proto))
                continue;

            validImplanters.Add(proto);
        }

        var sortedImplanters = validImplanters
            .OrderBy(proto => proto.Name)
            .Select(proto => new EntProtoId(proto.ID))
            .ToList();

        implanterComp.DeimplantChosen ??= sortedImplanters.FirstOrNull();

        foreach (var implant in sortedImplanters)
        {
            if(_proto.Resolve(implant, out var proto))
                implants.Add(proto.ID, proto.Name);
        }

        if (_window != null)
        {
            _window.UpdateImplantList(implants);
            _window.UpdateState(implanterComp.DeimplantChosen, implanterComp.TargetToDrawImplant, implanterComp.UserTrigger);
        }
    }
}
