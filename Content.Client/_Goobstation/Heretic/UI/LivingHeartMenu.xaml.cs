using Content.Client.UserInterface.Controls;
using Content.Shared.Heretic;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._Goobstation.Heretic.UI;

public sealed partial class LivingHeartMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _ent = default!;
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public EntityUid Entity { get; private set; }

    public event Action<NetEntity>? SendActivateMessageAction;

    public LivingHeartMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid ent)
    {
        Entity = ent;
        UpdateUI();
    }

    private void UpdateUI()
    {
        var main = FindControl<RadialContainer>("Main");
        if (main == null) return;

        var player = _player.LocalEntity;

        if (!_ent.TryGetComponent<HereticComponent>(player, out var heretic))
            return;

        foreach (var target in heretic.SacrificeTargets)
        {
            if (target == null) continue;

            var ent = _ent.GetEntity(target);
            if (ent == null)
                continue;

            var button = new EmbeddedEntityMenuButton
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = _ent.TryGetComponent<MetaDataComponent>(ent.Value, out var md) ? md.EntityName : "Unknown",
                NetEntity = (NetEntity) target,
            };

            var texture = new SpriteView(ent.Value, _ent)
            {
                OverrideDirection = Direction.South,
                VerticalAlignment = VAlignment.Center,
                SetSize = new Vector2(64, 64),
                VerticalExpand = true,
                Stretch = SpriteView.StretchMode.Fill,
            };
            button.AddChild(texture);

            main.AddChild(button);
        }
        AddAction(main);
    }

    private void AddAction(RadialContainer main)
    {
        if (main == null)
            return;

        foreach (var child in main.Children)
        {
            var castChild = child as EmbeddedEntityMenuButton;
            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendActivateMessageAction?.Invoke(castChild.NetEntity);
                Close();
            };
        }
    }

    public sealed class EmbeddedEntityMenuButton : RadialMenuTextureButton
    {
        public NetEntity NetEntity;
    }
}
