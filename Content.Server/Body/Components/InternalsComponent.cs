using Content.Server.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public class InternalsComponent : Component
    {
        public override string Name => "Internals";
        [ViewVariables] public EntityUid GasTankEntity { get; set; }
        [ViewVariables] public EntityUid BreathToolEntity { get; set; }

        public void DisconnectBreathTool()
        {
            var old = BreathToolEntity;
            BreathToolEntity = default;

            if (old != default && IoCManager.Resolve<IEntityManager>().TryGetComponent(old, out BreathToolComponent? breathTool) )
            {
                breathTool.DisconnectInternals();
                DisconnectTank();
            }
        }

        public void ConnectBreathTool(EntityUid toolEntity)
        {
            if (BreathToolEntity != default && IoCManager.Resolve<IEntityManager>().TryGetComponent(BreathToolEntity, out BreathToolComponent? tool))
            {
                tool.DisconnectInternals();
            }

            BreathToolEntity = toolEntity;
        }

        public void DisconnectTank()
        {
            if (GasTankEntity != default && IoCManager.Resolve<IEntityManager>().TryGetComponent(GasTankEntity, out GasTankComponent? tank))
            {
                tank.DisconnectFromInternals(Owner);
            }

            GasTankEntity = default;
        }

        public bool TryConnectTank(EntityUid tankEntity)
        {
            if (BreathToolEntity == default)
                return false;

            if (GasTankEntity != default && IoCManager.Resolve<IEntityManager>().TryGetComponent(GasTankEntity, out GasTankComponent? tank))
            {
                tank.DisconnectFromInternals(Owner);
            }

            GasTankEntity = tankEntity;
            return true;
        }

        public bool AreInternalsWorking()
        {
            return BreathToolEntity != default &&
                   GasTankEntity != default &&
                   IoCManager.Resolve<IEntityManager>().TryGetComponent(BreathToolEntity, out BreathToolComponent? breathTool) &&
                   breathTool.IsFunctional &&
                   IoCManager.Resolve<IEntityManager>().TryGetComponent(GasTankEntity, out GasTankComponent? gasTank) &&
                   gasTank.Air != default;
        }

    }
}
