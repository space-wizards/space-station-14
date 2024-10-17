using Content.Shared.Chat.Prototypes;
using Content.Shared.Inventory;

namespace Content.Shared.Emoting
{
    public sealed class EmoteAttemptEvent : CancellableEntityEventArgs
    {
        public EmoteAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
