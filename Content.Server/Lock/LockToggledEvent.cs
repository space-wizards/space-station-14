using Robust.Shared.GameObjects;

namespace Content.Server.Lock
{
    public class LockToggledEvent : HandledEntityEventArgs
    {
        public readonly bool Locked;

        public LockToggledEvent(bool locked)
        {
            Locked = locked;
        }
    }
}
