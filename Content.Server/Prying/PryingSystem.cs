using Content.Server.Storage.Components;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Prying;

public sealed class PryingSystem : SharedPryingSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityStorageComponent, PryDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, EntityStorageComponent storage, PryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;

        if (args.Used != null && TryComp<PryingComponent>(args.Used, out var comp))
        {
            _audioSystem.PlayPvs(comp.UseSound, args.Used.Value);
        }

        var ev = new PriedEvent(args.User);
        RaiseLocalEvent(uid, ref ev);
    }
}
