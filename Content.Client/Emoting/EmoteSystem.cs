using Content.Client.UserInterface.Systems.Radial;
using Content.Client.UserInterface.Systems.Radial.Controls;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Emoting;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Emoting;

public sealed class EmoteSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

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

        SubscribeNetworkEvent<RequestEmoteMenuEvent>(HandleEmoteMenuEvent);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        _openedMenu?.Dispose();
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        _openedMenu?.Dispose();
    }

    private void HandleEmoteMenuEvent(RequestEmoteMenuEvent args)
    {
        if (_openedMenu != null)
            return;

        _openedMenu = _userInterfaceManager.GetUIController<RadialUiController>()
            .CreateRadialContainer();

        foreach (var protoId in args.Prototypes)
        {
            if (_proto.TryIndex<EmotePrototype>(protoId, out var prototype))
            {
                var actionName = prototype.ChatMessages[_random.Next(prototype.ChatMessages.Count)];
                var texturePath = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new ResPath(DefaultIcon)));

                if (prototype.Icon != null)
                    texturePath = _spriteSystem.Frame0(prototype.Icon);

                var emoteButton = _openedMenu.AddButton(actionName, texturePath);
                emoteButton.Opacity = 210;
                emoteButton.Tooltip = null;
                emoteButton.Controller.OnPressed += (_) =>
                {
                    var ev = new SelectEmoteEvent(args.Target, protoId);
                    RaiseNetworkEvent(ev);
                    _openedMenu.Dispose();
                };
            }
        }

        _openedMenu.OnClose += (_) =>
        {
            _openedMenu = null;
        };

        _openedMenu.OpenAttached(args.Target);
    }
}
