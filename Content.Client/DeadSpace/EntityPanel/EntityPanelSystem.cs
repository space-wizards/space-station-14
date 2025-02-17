// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.UserInterface.Systems.Radial;
using Content.Client.UserInterface.Systems.Radial.Controls;
using Content.Shared.DeadSpace.EntityPanel;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.EntityPanel;

public sealed class EntityPanelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    /// <summary>
    /// We should enable radial for single target
    /// </summary>
    private RadialContainer? _openedMenu;

    private const string DefaultIcon = "/Textures/Interface/AdminActions/play.png";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RequestEntityMenuEvent>(HandleEntityMenuEvent);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        _openedMenu?.Dispose();
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        _openedMenu?.Dispose();
    }

    /// <summary>
    /// Draws RadialUI.
    /// <seealso cref="Content.Client.UserInterface.Systems.Radial.RadialUiController"/>
    /// </summary>
    private void HandleEntityMenuEvent(RequestEntityMenuEvent args)
    {
        if (_openedMenu != null)
            return;

        _openedMenu = _userInterfaceManager.GetUIController<RadialUiController>()
            .CreateRadialContainer();

        foreach (var protoId in args.Prototypes)
        {
            if (_proto.TryIndex<EntityPrototype>(protoId, out var prototype))
            {
                var actionName = prototype.Name;
                var texturePath = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new ResPath(DefaultIcon)));

                if (prototype != null)
                    texturePath = _spriteSystem.Frame0(prototype);

                var emoteButton = _openedMenu.AddButton(actionName, texturePath);
                emoteButton.Opacity = 210;
                emoteButton.Tooltip = null;
                emoteButton.Controller.OnPressed += (_) =>
                {
                    var ev = new SelectEntityEvent(args.Target, protoId, args.IsUseEvolutionSystem, args.IsUseSpawnPointSystem);
                    RaiseNetworkEvent(ev);
                    _openedMenu.Dispose();
                };
            }
        }

        _openedMenu.OnClose += (_) =>
        {
            _openedMenu = null;
        };
        if (_playerMan.LocalEntity != null)
            _openedMenu.OpenAttached((EntityUid)_playerMan.LocalEntity);

    }
}
