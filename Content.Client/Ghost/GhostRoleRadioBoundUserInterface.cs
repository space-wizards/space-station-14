using Content.Shared.Ghost.Roles;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Ghost;

public sealed class GhostRoleRadioBoundUserInterface : BoundUserInterface
{
    private GhostRoleRadioMenu? _ghostRoleRadioMenu;

    public GhostRoleRadioBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _ghostRoleRadioMenu = this.CreateWindow<GhostRoleRadioMenu>();
        _ghostRoleRadioMenu.SetEntity(Owner);
        _ghostRoleRadioMenu.SendGhostRoleRadioMessageAction += SendGhostRoleRadioMessage;
    }

    private void SendGhostRoleRadioMessage(ProtoId<GhostRolePrototype> protoId)
    {
        SendMessage(new GhostRoleRadioMessage(protoId));
    }
}
