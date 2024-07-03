using Content.Client.Storage.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Prying;

public sealed class PryingSystem : SharedPryingSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityStorageComponent, DoorPryDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, EntityStorageComponent storage, DoorPryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;

        TryComp<PryingComponent>(args.Used, out var comp);

        if (!CanPry(uid, args.User, out var message, comp))
        {
            if (!string.IsNullOrWhiteSpace(message))
                _popup.PopupClient(Loc.GetString(message), uid, args.User);
            return;
        }

        if (args.Used != null && comp != null)
        {
            _audioSystem.PlayPredicted(comp.UseSound, args.Used.Value, args.User);
        }
    }
}
