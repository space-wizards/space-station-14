using Robust.Shared.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;
[Serializable, NetSerializable]
    public sealed class SosiBibyEvent : EntityEventArgs
    {
        public string Message { get; }
        public SosiBibyEvent(string message)
        {
                Message = message;
        }
    }