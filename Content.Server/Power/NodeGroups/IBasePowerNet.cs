using Content.Server.Power.Components;
using Content.Server.Power.Pow3r;

namespace Content.Server.Power.NodeGroups
{
    public interface IBasePowerNet
    {
        void AddConsumer(PowerConsumerComponent consumer);

        void RemoveConsumer(PowerConsumerComponent consumer);

        void AddSupplier(PowerSupplierComponent supplier);

        void RemoveSupplier(PowerSupplierComponent supplier);

        PowerState.Network NetworkNode { get; }
    }
}
