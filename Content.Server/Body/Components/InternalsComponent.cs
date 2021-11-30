using Content.Server.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public class InternalsComponent : Component
    {
        public override string Name => "Internals";
        [ViewVariables] public IEntity? GasTankEntity { get; set; }
        [ViewVariables] public IEntity? BreathToolEntity { get; set; }

        public void DisconnectBreathTool()
        {
            var old = BreathToolEntity;
            BreathToolEntity = null;

            if (old != null && old.TryGetComponent(out BreathToolComponent? breathTool) )
            {
                breathTool.DisconnectInternals();
                DisconnectTank();
            }
        }

        public void ConnectBreathTool(IEntity toolEntity)
        {
            if (BreathToolEntity != null && BreathToolEntity.TryGetComponent(out BreathToolComponent? tool))
            {
                tool.DisconnectInternals();
            }

            BreathToolEntity = toolEntity;
        }

        public void DisconnectTank()
        {
            if (GasTankEntity != null && GasTankEntity.TryGetComponent(out GasTankComponent? tank))
            {
                tank.DisconnectFromInternals(Owner);
            }

            GasTankEntity = null;
        }

        public bool TryConnectTank(IEntity tankEntity)
        {
            if (BreathToolEntity == null)
                return false;

            if (GasTankEntity != null && GasTankEntity.TryGetComponent(out GasTankComponent? tank))
            {
                tank.DisconnectFromInternals(Owner);
            }

            GasTankEntity = tankEntity;
            return true;
        }

        public bool AreInternalsWorking()
        {
            return BreathToolEntity != null &&
                   GasTankEntity != null &&
                   BreathToolEntity.TryGetComponent(out BreathToolComponent? breathTool) &&
                   breathTool.IsFunctional &&
                   GasTankEntity.TryGetComponent(out GasTankComponent? gasTank) &&
                   gasTank.Air != null;
        }

    }
}
