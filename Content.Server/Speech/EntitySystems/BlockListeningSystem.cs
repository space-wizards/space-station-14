using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class BlockListeningSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockListeningComponent, ListenAttemptEvent>(OnListenAttempt);
    }

    private void OnListenAttempt(EntityUid uid, BlockListeningComponent component, ListenAttemptEvent args)
    {
        args.Cancel();
    }
}
