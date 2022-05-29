namespace Content.Server.Lock
{
    public sealed class LockToggledEvent : EntityEventArgs
    {
        public readonly bool Locked;

        public LockToggledEvent(bool locked)
        {
            Locked = locked;
        }
    }
}
