using Content.Client.UserInterface.Controls;
using Content.Shared.Ghost.Roles;
using Content.Shared.Ghost.Roles.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.Ghost;

public sealed partial class GhostRoleRadioMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public event Action<ProtoId<GhostRolePrototype>>? SendGhostRoleRadioMessageAction;

    public EntityUid Entity { get; set; }

    public GhostRoleRadioMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;
        RefreshUI();
    }

    private void RefreshUI()
    {
        // The main control that will contain all the clickable options
        var main = FindControl<RadialContainer>("Main");

        // The purpose of this radial UI is for ghost role radios that allow you to select
        // more than one potential option, such as with kobolds/lizards.
        // This means that it won't show anything if SelectablePrototypes is empty.
        if (!_entityManager.TryGetComponent<GhostRoleMobSpawnerComponent>(Entity, out var comp))
            return;

        foreach (var ghostRoleProtoString in comp.SelectablePrototypes)
        {
            // For each prototype we find we want to create a button that uses the name of the ghost role
            // as the hover tooltip, and the icon is taken from either the ghost role entityprototype
            // or the indicated icon entityprototype.
            if (!_prototypeManager.TryIndex<GhostRolePrototype>(ghostRoleProtoString, out var ghostRoleProto))
                continue;

            var button = new GhostRoleRadioMenuButton()
            {
                SetSize = new Vector2(64, 64),
                ToolTip = Loc.GetString(ghostRoleProto.Name),
                ProtoId = ghostRoleProto.ID,
            };

            var entProtoView = new EntityPrototypeView()
            {
                SetSize = new Vector2(48, 48),
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Stretch = SpriteView.StretchMode.Fill
            };

            // pick the icon if it exists, otherwise fallback to the ghost role's entity
            if (_prototypeManager.TryIndex(ghostRoleProto.IconPrototype, out var iconProto))
                entProtoView.SetPrototype(iconProto);
            else
                entProtoView.SetPrototype(ghostRoleProto.EntityPrototype);

            button.AddChild(entProtoView);
            main.AddChild(button);
            AddGhostRoleRadioMenuButtonOnClickActions(main);
        }
    }

    private void AddGhostRoleRadioMenuButtonOnClickActions(Control control)
    {
        var mainControl = control as RadialContainer;

        if (mainControl == null)
            return;

        foreach (var child in mainControl.Children)
        {
            var castChild = child as GhostRoleRadioMenuButton;

            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendGhostRoleRadioMessageAction?.Invoke(castChild.ProtoId);
                Close();
            };
        }
    }
}

public sealed class GhostRoleRadioMenuButton : RadialMenuTextureButtonWithSector
{
    public ProtoId<GhostRolePrototype> ProtoId { get; set; }
}
