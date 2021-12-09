using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Server.Power.Pow3r;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Tools;
using Content.Shared.Wires;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Localization;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed class CableMultitoolSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly PowerNetSystem _pnSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CableComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, CableComponent component, ExaminedEvent args)
        {
            // Must be in details range to try this.
            // Theoretically there should be a separate range at which a multitool works, but this does just fine.
            if (args.IsInDetailsRange)
            {
                // Determine if they are holding a multitool.
                if (EntityManager.TryGetComponent<HandsComponent?>(args.Examiner, out var hands) && hands.TryGetActiveHand(out var hand))
                {
                    var held = hand.HeldEntity;
                    // Pulsing is hardcoded here because I don't think it needs to be more complex than that right now.
                    // Update if I'm wrong.
                    if ((held != null) && _toolSystem.HasQuality(held, "Pulsing"))
                    {
                        args.PushMarkup(GenerateCableMarkup(uid));
                        // args.PushFancyUpdatingPowerGraphs(uid);
                    }
                }
            }
        }

        private string GenerateCableMarkup(EntityUid uid, NodeContainerComponent? nodeContainer = null)
        {
            if (!Resolve(uid, ref nodeContainer))
                return Loc.GetString("cable-multitool-system-internal-error-missing-component");

            foreach (var node in nodeContainer.Nodes)
            {
                if (!(node.Value.NodeGroup is IBasePowerNet))
                    continue;
                var p = (IBasePowerNet) node.Value.NodeGroup;
                var ps = _pnSystem.GetNetworkStatistics(p.NetworkNode);

                float storageRatio = ps.InStorageCurrent / Math.Max(ps.InStorageMax, 1.0f);
                float outStorageRatio = ps.OutStorageCurrent / Math.Max(ps.OutStorageMax, 1.0f);
                return Loc.GetString("cable-multitool-system-statistics",
                    ("supplyc", ps.SupplyCurrent),
                    ("supplyb", ps.SupplyBatteries),
                    ("supplym", ps.SupplyTheoretical),
                    ("consumption", ps.Consumption),
                    ("storagec", ps.InStorageCurrent),
                    ("storager", storageRatio),
                    ("storagem", ps.InStorageMax),
                    ("storageoc", ps.OutStorageCurrent),
                    ("storageor", outStorageRatio),
                    ("storageom", ps.OutStorageMax)
                );
            }
            return Loc.GetString("cable-multitool-system-internal-error-no-power-node");
        }
    }
}
