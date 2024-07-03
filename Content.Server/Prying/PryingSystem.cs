using Content.Server.Storage.Components;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;

namespace Content.Server.Prying;

public sealed class PryingSystem : SharedPryingSystem
{
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

        var ev = new PriedEvent(args.User);
        RaiseLocalEvent(uid, ref ev);
    }
}
