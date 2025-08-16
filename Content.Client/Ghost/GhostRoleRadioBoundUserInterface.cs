using Content.Client.UserInterface.Controls;
using Content.Shared.Ghost.Roles;
using Content.Shared.Ghost.Roles.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Ghost;

public sealed class GhostRoleRadioBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SimpleRadialMenu? _ghostRoleRadioMenu;

    protected override void Open()
    {
        base.Open();

        _ghostRoleRadioMenu = this.CreateWindow<SimpleRadialMenu>();

        // The purpose of this radial UI is for ghost role radios that allow you to select
        // more than one potential option, such as with kobolds/lizards.
        // This means that it won't show anything if SelectablePrototypes is empty.
        if (!EntMan.TryGetComponent<GhostRoleMobSpawnerComponent>(Owner, out var comp))
            return;

        var list = ConvertToButtons(comp.SelectablePrototypes);

        _ghostRoleRadioMenu.SetButtons(list);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(List<ProtoId<GhostRolePrototype>> protoIds)
    {
        var list = new List<RadialMenuOptionBase>();
        foreach (var ghostRoleProtoId in protoIds)
        {
            // For each prototype we find we want to create a button that uses the name of the ghost role
            // as the hover tooltip, and the icon is taken from either the ghost role entityprototype
            // or the indicated icon entityprototype.
            if (!_prototypeManager.Resolve(ghostRoleProtoId, out var ghostRoleProto))
                continue;

            var option = new RadialMenuActionOption<ProtoId<GhostRolePrototype>>(SendGhostRoleRadioMessage, ghostRoleProtoId)
            {
                ToolTip = Loc.GetString(ghostRoleProto.Name),
                // pick the icon if it exists, otherwise fallback to the ghost role's entity
                IconSpecifier = ghostRoleProto.IconPrototype != null
                                && _prototypeManager.Resolve(ghostRoleProto.IconPrototype, out var iconProto)
                    ? RadialMenuIconSpecifier.With(iconProto)
                    : RadialMenuIconSpecifier.With(ghostRoleProto.EntityPrototype)
            };
            list.Add(option);
        }

        return list;
    }

    private void SendGhostRoleRadioMessage(ProtoId<GhostRolePrototype> protoId)
    {
        SendMessage(new GhostRoleRadioMessage(protoId));
    }
}
