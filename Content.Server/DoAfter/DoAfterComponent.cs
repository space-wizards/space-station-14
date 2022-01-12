using System.Collections.Generic;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Server.DoAfter
{
    [RegisterComponent]
    public sealed class DoAfterComponent : SharedDoAfterComponent
    {
        public IReadOnlyCollection<DoAfter> DoAfters => _doAfters.Keys;
        private readonly Dictionary<DoAfter, byte> _doAfters = new();

        // So the client knows which one to update (and so we don't send all of the do_afters every time 1 updates)
        // we'll just send them the index. Doesn't matter if it wraps around.
        private byte _runningIndex;

        public override ComponentState GetComponentState()
        {
            var toAdd = new List<ClientDoAfter>();

            foreach (var doAfter in DoAfters)
            {
                // THE ALMIGHTY PYRAMID
                var clientDoAfter = new ClientDoAfter(
                    _doAfters[doAfter],
                    doAfter.UserGrid,
                    doAfter.TargetGrid,
                    doAfter.StartTime,
                    doAfter.EventArgs.Delay,
                    doAfter.EventArgs.BreakOnUserMove,
                    doAfter.EventArgs.BreakOnTargetMove,
                    doAfter.EventArgs.MovementThreshold,
                    doAfter.EventArgs.Target);

                toAdd.Add(clientDoAfter);
            }

            return new DoAfterComponentState(toAdd);
        }

        public void Add(DoAfter doAfter)
        {
            _doAfters.Add(doAfter, _runningIndex);
            _runningIndex++;
            Dirty();
        }

        public void Cancelled(DoAfter doAfter)
        {
            if (!_doAfters.TryGetValue(doAfter, out var index))
                return;

            _doAfters.Remove(doAfter);
#pragma warning disable 618
            SendNetworkMessage(new CancelledDoAfterMessage(index));
#pragma warning restore 618
        }

        /// <summary>
        ///     Call when the particular DoAfter is finished.
        ///     Client should be tracking this independently.
        /// </summary>
        /// <param name="doAfter"></param>
        public void Finished(DoAfter doAfter)
        {
            if (!_doAfters.ContainsKey(doAfter))
                return;

            _doAfters.Remove(doAfter);
        }
    }
}
