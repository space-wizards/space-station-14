using Content.Shared.Audio;
using Content.Shared.CartridgeLoader;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.CartridgeLoader.Cartridges;
public sealed class DigitalIanCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DigitalIanCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<DigitalIanCartridgeComponent, CartridgeMessageEvent>(OnAction);
    }


    private void OnUiReady(EntityUid uid, DigitalIanCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        _audioSystem.PlayPvs(component.SoundPet, uid);
    }
    private void OnAction(EntityUid uid, DigitalIanCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not DigitalIanUiMessageEvent action)
        {
            return;
        }

        switch (action.Action)
        {
            case DigitalIanUiAction.Feed:
                _audioSystem.PlayPvs(component.SoundFeed, uid);
                break;
            case DigitalIanUiAction.Pet:
                _audioSystem.PlayPvs(component.SoundPet, uid);
                break;
        }
    }
}