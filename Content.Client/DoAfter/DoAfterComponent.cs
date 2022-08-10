using Content.Shared.DoAfter;

namespace Content.Client.DoAfter
{
    [RegisterComponent, Access(typeof(DoAfterSystem))]
    public sealed class DoAfterComponent : SharedDoAfterComponent
    {
        public readonly Dictionary<byte, ClientDoAfter> DoAfters = new();

        public readonly Dictionary<byte, ClientDoAfter> CancelledDoAfters = new();
    }
}
