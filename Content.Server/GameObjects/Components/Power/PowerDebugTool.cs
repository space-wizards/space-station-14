using System;
using System.Text;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Power;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Map;

namespace Content.Server.GameObjects.Components.Power
{
    public class PowerDebugTool : SharedPowerDebugTool, IAfterAttack
    {
        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (eventArgs.Attacked == null)
            {
                return;
            }

            var builder = new StringBuilder();

            builder.AppendFormat("Entity: {0} ({1})\n", eventArgs.Attacked.Name, eventArgs.Attacked.Uid);

            if (eventArgs.Attacked.TryGetComponent<PowerNodeComponent>(out var node))
            {
                builder.AppendFormat("Power Node:\n");
                if (node.Parent == null)
                {
                    builder.Append("  No Powernet!\n");
                }
                else
                {
                    var net = node.Parent;
                    builder.AppendFormat(@"  Powernet: {0}
  Wires: {1}, Nodes: {2}
  Generators: {3}, Loaders: {4},
  StorageS: {5}, StorageC: {6},
  Load: {7}, Supply: {8},
  LAvail: {9}, LDraw: {10},
  LDemand: {11}, LDemandWS: {12},
",
                        net.Uid,
                        net.NodeList.Count, net.WireList.Count,
                        net.GeneratorCount, net.DeviceCount,
                        net.PowerStorageSupplierCount, net.PowerStorageConsumerCount,
                        net.Load, net.Supply,
                        net.LastTotalAvailable, net.LastTotalDraw,
                        net.LastTotalDemand, net.LastTotalDemandWithSuppliers);
                }
            }

            if (eventArgs.Attacked.TryGetComponent<PowerDeviceComponent>(out var device))
            {
                builder.AppendFormat(@"Power Device:
  Load: {0} W
  Priority: {1}
  Drawtype: {2}, Connected: {3}
  Powered: {4}
", device.Load, device.Priority, device.DrawType, device.Connected, device.Powered);

                if (device.Connected == DrawTypes.Provider || device.Connected == DrawTypes.Both)
                {
                    builder.Append("    Providers:\n");
                    foreach (var provider in device.AvailableProviders)
                    {
                        var providerTransform = provider.Owner.GetComponent<ITransformComponent>();
                        builder.AppendFormat("      {0} ({1}) @ {2}", provider.Owner.Name, provider.Owner.Uid, providerTransform.GridPosition);
                        if (device.Provider == provider)
                        {
                            builder.Append(" (CURRENT)");
                        }
                        builder.Append('\n');
                    }
                }
            }

            if (eventArgs.Attacked.TryGetComponent<PowerStorageNetComponent>(out var storage))
            {
                var stateSeconds = (DateTime.Now - storage.LastChargeStateChange).TotalSeconds;
                builder.AppendFormat(@"Power Storage:
  Capacity: {0}, Charge: {1}, ChargeRate: {2}, DistributionRate: {3}, ChargePowernet: {4}
  LastChargeState: {5} ({6}), LastChargeStateChange: {7:0.00} seconds ago.
", storage.Capacity, storage.Charge, storage.ChargeRate, storage.DistributionRate, storage.ChargePowernet, storage.LastChargeState, storage.GetChargeState(), stateSeconds);
            }

            if (eventArgs.Attacked.TryGetComponent<PowerTransferComponent>(out var transfer))
            {
                builder.AppendFormat(@"Power Transfer:
  Powernet: {0}
", transfer.Parent.Uid);
            }

            OpenDataWindowClientSide(eventArgs.User, builder.ToString());
        }

        private void OpenDataWindowClientSide(IEntity user, string data)
        {
            if (!user.TryGetComponent<IActorComponent>(out var actor))
            {
                return;
            }

            SendNetworkMessage(new OpenDataWindowMsg(data), actor.playerSession.ConnectedClient);
        }
    }
}
