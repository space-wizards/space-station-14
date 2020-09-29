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
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Max(value, 0);
                Air.Volume = _volume;
            }
        }
        private float _volume;

        [ViewVariables]
        public bool Anchored => !Owner.TryGetComponent<ICollidableComponent>(out var collidable) || collidable.Anchored;

        [ViewVariables]
        public GasCanisterPortComponent ConnectedPort { get; private set; }

        [ViewVariables]
        public bool ConnectedToPort => ConnectedPort != null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            Air = new GasMixture();
            serializer.DataField(this, x => Volume, "volume", 10);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                AnchorUpdate();
                collidable.AnchoredChanged += AnchorUpdate;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                collidable.AnchoredChanged -= AnchorUpdate;
            }
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
