// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.UserInterface.Systems.Radial;
using Content.Client.UserInterface.Systems.Radial.Controls;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.DeadSpace.Languages;
using System.Linq;

namespace Content.Client.DeadSpace.Languages;

public sealed class LanguagePanelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    private RadialContainer? _openedMenu;
    private const string DefaultIcon = "/Textures/_DeadSpace/LanguageIcons/default.png";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RequestLanguageMenuEvent>(HandleEntityMenuEvent);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        _openedMenu?.Dispose();
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        _openedMenu?.Dispose();
    }

    private void HandleEntityMenuEvent(RequestLanguageMenuEvent args)
    {
        if (_openedMenu != null)
            return;

        _openedMenu = _userInterfaceManager.GetUIController<RadialUiController>()
            .CreateRadialContainer();

        var speakableLanguages = args.KnownLanguages
            .Except(args.CantSpeakLanguages)
            .ToList();

        foreach (var protoId in speakableLanguages)
        {
            if (_proto.TryIndex(protoId, out var prototype))
            {
                if (prototype == null)
                    return;

                var actionName = prototype.Name;
                var texturePath = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new ResPath(DefaultIcon)));

                if (prototype.Icon != null)
                    texturePath = _spriteSystem.Frame0(prototype.Icon);

                var emoteButton = _openedMenu.AddButton(actionName, texturePath);
                emoteButton.Opacity = 210;
                emoteButton.Tooltip = null;
                emoteButton.Controller.OnPressed += (_) =>
                {
                    var ev = new SelectLanguageEvent(args.Target, protoId);
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
