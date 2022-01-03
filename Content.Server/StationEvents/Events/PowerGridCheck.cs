using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Server.Power.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class PowerGridCheck : StationEvent
    {
        public override string Name => "PowerGridCheck";
        public override float Weight => WeightNormal;
        public override int? MaxOccurrences => 3;
        public override string StartAnnouncement => Loc.GetString("station-event-power-grid-check-start-announcement");
        protected override string EndAnnouncement => Loc.GetString("station-event-power-grid-check-end-announcement");
        public override string? StartAudio => "/Audio/Announcements/power_off.ogg";

        // If you need EndAudio it's down below. Not set here because we can't play it at the normal time without spamming sounds.

        protected override float StartAfter => 12.0f;

        private CancellationTokenSource? _announceCancelToken;

        private readonly List<HashSet<NodeContainerComponent>> _powerStages = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Announce()
        {
            base.Announce();
            EndAfter = IoCManager.Resolve<IRobustRandom>().Next(60, 120);
        }

        private const int SHUTOFF_TARGET_MS = 8000;
        private const int REBOOT_TARGET_MS = 4000;

        private static bool IsAcceptableNode(NodeGroupID i) => i is NodeGroupID.HVPower or NodeGroupID.MVPower or NodeGroupID.Apc;

        public override void Startup()
        {
            // Everything we've ever seen. Used to avoid loops.
            var seen = new HashSet<NodeContainerComponent>();
            // Current sources. Not like electrical current, just the ones we're using right now.
            var curr = new HashSet<NodeContainerComponent>();
            // Current children, i.e. the next curr
            var next = new HashSet<NodeContainerComponent>();

            // First we get the seed units
            foreach (var (ngComp, pSupComp) in _entityManager.EntityQuery<NodeContainerComponent, PowerSupplierComponent>(true))
            {
                // We start with the HV networks
                if (ngComp.Nodes.Values.Any(n => n.NodeGroupID == NodeGroupID.HVPower))
                {
                    curr.Add(ngComp);
                }
            }

            // Break 'em in to tiers
            while (curr.Count > 0)
            {
                foreach (var cncc in curr)
                {
                    // Seen 'em? Skip em.
                    if (!seen.Add(cncc))
                        continue;

                    // No breaking atmos nets, pls
                    foreach (var n in cncc.Nodes.Values.Where(nn => IsAcceptableNode(nn.NodeGroupID)))
                    {
                        foreach (var rn in n.ReachableNodes)
                        {
                            // WHY DO WE HAVE TO PUT THIS SO MANY LAYERS DOWN
                            // FUCK
                            if (_entityManager.TryGetComponent<NodeContainerComponent>(rn.Owner, out var ncc))
                                next.Add(ncc);
                        }
                    }
                }

                _powerStages.Add(curr);
                curr = next;
                next = new();
            }

            if (next.Count > 0)
                _powerStages.Add(next);

            DoShutoff(0);

            base.Startup();
        }

        private void Toggle(EntityUid owner, bool off)
        {
            if (_entityManager.Deleted(owner))
                return;

            // We try to be as component-agnostic as possible here, so we try everything.

            if (_entityManager.TryGetComponent<PowerSupplierComponent>(owner, out var psc))
                psc.Enabled = !off;

            // Nothing to shut off?
            /*
            if (_entityManager.TryGetComponent<PowerConsumerComponent>(pns.Owner, out var pcc))
                pcc.
            */

            if (_entityManager.TryGetComponent<PowerNetworkBatteryComponent>(owner, out var pnbc))
                pnbc.Enabled = !off;

            // Nothing to shut off here either, I guess
            /*
            if (_entityManager.TryGetComponent<ApcPowerProviderComponent>(pns.Owner, out var appc))
                appc.
            */

            if (_entityManager.TryGetComponent<ApcPowerReceiverComponent>(owner, out var aprc))
                aprc.PowerDisabled = off;
        }

        private void DoShutoff(int tier, bool fast = false)
        {
            if (tier >= _powerStages.Count)
                return;

            foreach (var pns in _powerStages[tier])
                Toggle(pns.Owner, true);

            if (fast)
                DoShutoff(tier+1);
            else
            {
                // The (rough) goal for how long each stage should take to shut down
                var tgt = SHUTOFF_TARGET_MS / _powerStages.Count;

                // The actual next time will be fudged ±50% of the target.
                Timer.Spawn(
                    _random.Next(
                        tgt - (int)(0.5f * tgt),
                        tgt + (int)(0.5f * tgt)
                    ),
                    () => DoShutoff(tier + 1)
                );
            }
        }

        // NOTE: Fast is provided in case we ever add a way to abort an event.
        // It's mostly relevant for this, but I added it to DoShutoff too just for good measure.
        private void DoReboot(int tier, bool fast = false)
        {
            if (tier < 0)
            {
                _powerStages.Clear();
                return;
            }

            foreach (var pns in _powerStages[tier])
                Toggle(pns.Owner, false);

            if (fast)
                DoReboot(tier - 1);
            else
            {
                // Same as the DoShutoff version.
                var tgt = REBOOT_TARGET_MS / _powerStages.Count;

                Timer.Spawn(
                    _random.Next(
                        tgt - (int)(0.5f * tgt),
                        tgt + (int)(0.5f * tgt)
                    ),
                    () => DoReboot(tier - 1)
                );
            }
        }

        public override void Shutdown()
        {
            DoReboot(_powerStages.Count - 1);

            // Can't use the default EndAudio
            _announceCancelToken?.Cancel();
            _announceCancelToken = new CancellationTokenSource();
            Timer.Spawn(3000, () =>
            {
                SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/power_on.ogg");
            }, _announceCancelToken.Token);

            base.Shutdown();
        }
    }
}
