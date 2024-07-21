using Content.Shared.Ghost.Roles;
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

        _ghostRoleRadioMenu = new GhostRoleRadioMenu(Owner, this);
        _ghostRoleRadioMenu.OnClose += Close;

        // Open menu centered wherever the mouse currently is
        _ghostRoleRadioMenu.OpenCentered();
    }

    public void SendGhostRoleRadioMessage(ProtoId<GhostRolePrototype> protoId)
    {
        SendMessage(new GhostRoleRadioMessage(protoId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _ghostRoleRadioMenu?.Dispose();
    }
}
