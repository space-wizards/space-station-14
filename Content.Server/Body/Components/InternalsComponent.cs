using Content.Server.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public class InternalsComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "Internals";
        [ViewVariables] public EntityUid? GasTankEntity { get; set; }
        [ViewVariables] public EntityUid? BreathToolEntity { get; set; }

        public void DisconnectBreathTool()
        {
            var old = BreathToolEntity;
            BreathToolEntity = null;

            if (_entMan.TryGetComponent(old, out BreathToolComponent? breathTool) )
            {
                breathTool.DisconnectInternals();
                DisconnectTank();
            }
        }

        public void ConnectBreathTool(EntityUid toolEntity)
        {
            if (_entMan.TryGetComponent(BreathToolEntity, out BreathToolComponent? tool))
            {
                tool.DisconnectInternals();
            }

            BreathToolEntity = toolEntity;
        }

        public void DisconnectTank()
        {
            if (_entMan.TryGetComponent(GasTankEntity, out GasTankComponent? tank))
            {
                tank.DisconnectFromInternals(Owner);
            }

            GasTankEntity = null;
        }

        public bool TryConnectTank(EntityUid tankEntity)
        {
            if (BreathToolEntity == null)
                return false;

            if (_entMan.TryGetComponent(GasTankEntity, out GasTankComponent? tank))
            {
                tank.DisconnectFromInternals(Owner);
            }

            GasTankEntity = tankEntity;
            return true;
        }

        public bool AreInternalsWorking()
        {
            return _entMan.TryGetComponent(BreathToolEntity, out BreathToolComponent? breathTool) &&
                   breathTool.IsFunctional &&
                   _entMan.TryGetComponent(GasTankEntity, out GasTankComponent? gasTank) &&
                   gasTank.Air != null;
        }

    }
}
