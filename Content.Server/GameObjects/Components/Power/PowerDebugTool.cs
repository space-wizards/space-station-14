using System;
using System.Text;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Power;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;

namespace Content.Server.GameObjects.Components.Power
{
    public class PowerDebugTool : SharedPowerDebugTool, IAfterAttack
    {
        void IAfterAttack.Afterattack(IEntity user, GridLocalCoordinates clicklocation, IEntity attacked)
        {
            if (attacked == null)
            {
                return;
            }

            var builder = new StringBuilder();

            builder.AppendFormat("Entity: {0} ({1})\n", attacked.Name, attacked.Uid);

            if (attacked.TryGetComponent<PowerNodeComponent>(out var node))
            {
                builder.AppendFormat("Power Node:\n");
                if (node.Parent == null)
                {
                    builder.Append("  No Powernet!\n");
                }
                else
                {
                    builder.AppendFormat("  Powernet: {0}\n", node.Parent.Uid);
                }
            }

            if (attacked.TryGetComponent<PowerDeviceComponent>(out var device))
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
                        var providerTransform = provider.Owner.GetComponent<IServerTransformComponent>();
                        builder.AppendFormat("      {0} ({1}) @ {2}", provider.Owner.Name, provider.Owner.Uid, providerTransform.LocalPosition);
                        if (device.Provider == provider)
                        {
                            builder.Append(" (CURRENT)");
                        }
                        builder.Append('\n');
                    }
                }
            }

            if (attacked.TryGetComponent<PowerStorageComponent>(out var storage))
            {
                var stateSeconds = (DateTime.Now - storage.LastChargeStateChange).TotalSeconds;
                builder.AppendFormat(@"Power Storage:
  Capacity: {0}, Charge: {1}, ChargeRate: {2}, DistributionRate: {3}, ChargePowernet: {4}
  LastChargeState: {5} ({6}), LastChargeStateChange: {7:0.00} seconds ago.
", storage.Capacity, storage.Charge, storage.ChargeRate, storage.DistributionRate, storage.ChargePowernet, storage.LastChargeState, storage.GetChargeState(), stateSeconds);
            }

            if (attacked.TryGetComponent<PowerTransferComponent>(out var transfer))
            {
                builder.AppendFormat(@"Power Transfer:
  Powernet: {0}
", transfer.Parent.Uid);
            }

            OpenDataWindowClientSide(user, builder.ToString());
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
