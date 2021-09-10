using Robust.Shared.GameObjects;

namespace Content.Server.Lock
{
    public class LockToggledEvent : EntityEventArgs
    {
        public readonly bool Locked;

        public LockToggledEvent(bool locked)
        {
            Locked = locked;
        }
    }
}
