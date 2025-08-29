using Content.Client.UserInterface.Systems.Character;
using Content.Shared._Starlight.Character.Info;
using Robust.Client.UserInterface;

namespace Content.Client._Starlight.Character.Info;

public sealed class SLCharacterInfoSystem : SLSharedCharacterInfoSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private CharacterUIController _controller => _ui.GetUIController<CharacterUIController>();

    protected override void OpenCharacterWindow(EntityUid target, EntityUid requester)
    {
        _controller.OpenInspectCharacterWindow(target, requester);
    }
}