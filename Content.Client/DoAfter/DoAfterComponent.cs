using Content.Client.DoAfter.UI;
using Content.Shared.DoAfter;

namespace Content.Client.DoAfter
{
    [RegisterComponent, Friend(typeof(DoAfterSystem))]
    public sealed class DoAfterComponent : SharedDoAfterComponent
    {
        public readonly Dictionary<byte, ClientDoAfter> DoAfters = new();

        public readonly List<(TimeSpan CancelTime, ClientDoAfter Message)> CancelledDoAfters = new();

        public DoAfterGui? Gui { get; set; }
    }
}
