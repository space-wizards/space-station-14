using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class BlockListeningSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnListenAttempt(Entity<BlockListeningComponent> ent, ref ListenAttemptEvent args)
    {
        args.Cancel();
    }
}
