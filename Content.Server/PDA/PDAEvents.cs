using Robust.Shared.GameObjects;

namespace Content.Server.PDA
{
    public class TrySetPDAOwner : EntityEventArgs
    {
        public string OwnerName;

        public TrySetPDAOwner(string name)
        {
            OwnerName = name;
        }
    }
}
