using Content.Client.UserInterface.Controls;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._Goobstation.Heretic.UI;

public sealed partial class HereticRitualRuneRadialMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public event Action<ProtoId<HereticRitualPrototype>>? SendHereticRitualRuneMessageAction;

    public EntityUid Entity { get; set; }

    public HereticRitualRuneRadialMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;
        RefreshUI();
    }

    private void RefreshUI()
    {
        var main = FindControl<RadialContainer>("Main");
        if (main == null)
            return;

        var player = _playerManager.LocalEntity;

        if (!_entityManager.TryGetComponent<HereticComponent>(player, out var heretic))
            return;

        foreach (var ritual in heretic.KnownRituals)
        {
            if (!_prototypeManager.TryIndex(ritual, out var ritualPrototype))
                continue;

            var button = new HereticRitualMenuButton
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = Loc.GetString(ritualPrototype.LocName),
                ProtoId = ritualPrototype.ID
            };

            var texture = new TextureRect
            {
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Texture = _spriteSystem.Frame0(ritualPrototype.Icon),
                TextureScale = new Vector2(2f, 2f)
            };

            button.AddChild(texture);
            main.AddChild(button);
        }

        AddHereticRitualMenuButtonOnClickAction(main);
    }

    private void AddHereticRitualMenuButtonOnClickAction(RadialContainer mainControl)
    {
        if (mainControl == null)
            return;

        foreach(var child in mainControl.Children)
        {
            var castChild = child as HereticRitualMenuButton;

            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendHereticRitualRuneMessageAction?.Invoke(castChild.ProtoId);
                Close();
            };
        }
    }

    public sealed class HereticRitualMenuButton : RadialMenuTextureButton
    {
        public ProtoId<HereticRitualPrototype> ProtoId { get; set; }
    }
}
