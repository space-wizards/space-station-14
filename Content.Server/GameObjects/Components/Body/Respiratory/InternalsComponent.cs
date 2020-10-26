#nullable enable
using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Respiratory
{
    [RegisterComponent]
    public class InternalsComponent: Component
    {
        public override string Name => "Internals";
        [ViewVariables] public IEntity? GasTankEntity { get; set; }
        [ViewVariables] public IEntity? BreathToolEntity { get; set; }

        public void DisconnectBreathTool()
        {
            BreathToolEntity = null;
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
            GasTankEntity = null;
        }

        public void ConnectTank(IEntity tankEntity)
        {
            if (GasTankEntity != null && GasTankEntity.TryGetComponent(out GasTankComponent? tank))
            {
                tank.DisconnectFromInternals();
            }
            GasTankEntity = tankEntity;
        }

    }
}
