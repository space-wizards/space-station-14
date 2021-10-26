using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Server.Power.NodeGroups;
using Content.Shared.Localizations;
using Content.Shared.Localizations.Units;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Power
{
    public class PowerStats
    {
        public static string FormatPowerStats(NodeContainerComponent ncc)
        {
            var buf = "";
            foreach (var node in ncc.Nodes)
            {
                if (node.Value is not CableNode || node.Value.NodeGroup is null)
                    continue;

                switch (node.Value.NodeGroup)
                {
                    case PowerNet ng:
                        {
                            var sup = ng.Suppliers.Sum(s => s.CurrentSupply);
                            var dmd = ng.Consumers.Sum(c => c.DrawRate);

                            var chgdmd = ng.Chargers
                                .Sum((c) =>
                                {
                                    c.Owner.TryGetComponent<PowerNetworkBatteryComponent>(out var x);
                                    return x?.CurrentReceiving ?? 0f;
                                });

                            var batdischg = ng.Dischargers
                                .Select((d) =>
                                {
                                    d.Owner.TryGetComponent<BatteryComponent>(out var x);
                                    return x;
                                })
                                .Where((x) => x is not null);

                            var batstat = batdischg
                                .Aggregate(
                                    (tot: 0f, max: 0f),
                                    (a, b) => (a.tot + (b?.CurrentCharge ?? 0), a.max + (b?.MaxCharge ?? 0))
                                );

                            Units.Power.TryGetUnit(batstat.max, out var bmu);
                            DebugTools.AssertNotNull(bmu);
                            buf += Loc.GetString("power-measure-report",
                                    ("outlet", node.Key),
                                    ("supply", Units.Power.Format(sup, "N2")),
                                    ("demand", Units.Power.Format(dmd, "N2")),
                                    ("chgdmd", Units.Power.Format(chgdmd, "N2")),
                                    ("batprc", (batstat.tot / batstat.max).ToString("P1")),
                                    ("battot", (batstat.tot * bmu!.Factor).ToString("N2")),
                                    ("batmax", Units.Power.Format(batstat.max, "N2"))
                            ) + "\n";
                        }
                        break;

                    case ApcNet ng:
                        {
                            var sup = ng.Apcs.Sum(a => a.Battery?.CurrentCharge ?? 0);

                            var dmd = ng.Providers
                                .Aggregate(
                                    new List<ApcPowerReceiverComponent>(),
                                    (a, p) => { a.AddRange(p.LinkedReceivers); return a; }
                                )
                                .Sum(c => c.NetworkLoad.DesiredPower);

                            var batstat = ng.Apcs
                                .Select(a => a.Battery)
                                .Aggregate(
                                    (tot: 0f, max: 0f),
                                    (a, b) => (a.tot + b?.CurrentCharge ?? 0, a.max + b?.MaxCharge ?? 0)
                                );

                            Units.Power.TryGetUnit(batstat.max, out var bmu);
                            DebugTools.AssertNotNull(bmu);
                            buf += Loc.GetString("power-measure-report-apc",
                                    ("outlet", node.Key),
                                    ("supply", Units.Power.Format(sup, "N2")),
                                    ("demand", Units.Power.Format(dmd, "N2")),
                                    ("batprc", (batstat.tot / batstat.max).ToString("P1")),
                                    ("battot", (batstat.tot * bmu!.Factor).ToString("N2")),
                                    ("batmax", Units.Power.Format(batstat.max, "N2"))
                            ) + "\n";
                        }
                        break;
                }
            }
            return buf;
        }
    }
}
