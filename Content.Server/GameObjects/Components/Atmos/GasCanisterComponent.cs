using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasCanisterComponent : Component, IGasMixtureHolder
    {
        public override string Name => "GasCanister";

        [ViewVariables]
        public GasMixture Air { get; set; }

        [ViewVariables]
        public bool Anchored => !Owner.TryGetComponent<IPhysicsComponent>(out var physics) || physics.Anchored;

        [ViewVariables]
        public GasCanisterPortComponent ConnectedPort { get; private set; }

        [ViewVariables]
        public bool ConnectedToPort => ConnectedPort != null;

        private const float DefaultVolume = 10;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => Air, "gasMixture", new GasMixture(DefaultVolume));
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent<IPhysicsComponent>(out var physics))
            {
                AnchorUpdate();
                physics.AnchoredChanged += AnchorUpdate;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent<IPhysicsComponent>(out var physics))
            {
                physics.AnchoredChanged -= AnchorUpdate;
            }
            DisconnectFromPort();
        }


        public void TryConnectToPort()
        {
            if (!Owner.TryGetComponent<SnapGridComponent>(out var snapGrid)) return;
            var port = snapGrid.GetLocal()
                .Select(entity => entity.TryGetComponent<GasCanisterPortComponent>(out var port) ? port : null)
                .Where(port => port != null)
                .Where(port => !port.ConnectedToCanister)
                .FirstOrDefault();
            if (port == null) return;
            ConnectedPort = port;
            ConnectedPort.ConnectGasCanister(this);
        }

        public void DisconnectFromPort()
        {
            ConnectedPort?.DisconnectGasCanister();
            ConnectedPort = null;
        }

        private void AnchorUpdate()
        {
            if (Anchored)
            {
                TryConnectToPort();
            }
            else
            {
                DisconnectFromPort();
            }
        }
    }
}
