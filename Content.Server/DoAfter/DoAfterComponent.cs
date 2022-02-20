using Content.Shared.DoAfter;

namespace Content.Server.DoAfter
{
    [RegisterComponent, Friend(typeof(DoAfterSystem))]
    public sealed class DoAfterComponent : SharedDoAfterComponent
    {
        public readonly Dictionary<DoAfter, byte> DoAfters = new();

        // So the client knows which one to update (and so we don't send all of the do_afters every time 1 updates)
        // we'll just send them the index. Doesn't matter if it wraps around.
        public byte RunningIndex;
    }
}
