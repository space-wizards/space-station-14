using Content.Shared.Doors.Prying.Systems;
using Content.Shared.Doors.Prying.Components;
using Content.Shared.Doors.Components;

namespace Content.Client.Doors.Prying.Systems;

// Why does this exist you may be asking? Since the server prying system exists
// for issues listed here, in order for the tool's prying sound to be triggered
// we need this.
public sealed class DoorPryingSystem : SharedDoorPryingSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    protected override void OnDoAfter(EntityUid uid, DoorComponent door, DoorPryDoAfterEvent args){
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;

        DoorPryingComponent? comp = null;

        if (args.Used != null && Resolve(args.Used.Value, ref comp))
            _audioSystem.PlayPredicted(comp.UseSound, args.Used.Value, args.User, comp.UseSound.Params.WithVariation(0.175f).AddVolume(-5f));
    }
}
