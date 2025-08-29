using Content.Client.UserInterface.Systems.Character;
using Content.Shared._Starlight.Character.Info;
using Robust.Client.UserInterface;

namespace Content.Client._Starlight.Character.Info;

public sealed class SLCharacterInfoSystem : SLSharedCharacterInfoSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private CharacterUIController _controller => _ui.GetUIController<CharacterUIController>();

    public override void Initialize()
    {
        // TODO AFTERLIGHT move to the UI controller when subscriptions there don't get wiped by disconnecting without restarting
        SubscribeLocalEvent<OpenInspectCharacterInfoEvent>(OnOpenCharacterInspect);
    }

    private void OnOpenCharacterInspect(OpenInspectCharacterInfoEvent ev)
    {
        // TODO AFTERLIGHT move to the UI controller when subscriptions there don't get wiped by disconnecting without restarting
        _controller.OpenInspectCharacterWindow(ev);
    }
}