using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Serialization;
using System;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasCanisterComponent : Component, IGasMixtureHolder
    {
        public override string Name => "GasCanister";

        public GasMixture Air { get; set; } = new GasMixture();

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

        private bool Anchored => !Owner.TryGetComponent<ICollidableComponent>(out var collidable) || collidable.Anchored;

        public GasCanisterPortComponent ConnectedGasCanisterPort { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
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

        private void AnchorUpdate()
        {
            if (Anchored)
            {
                if (!Owner.TryGetComponent<SnapGridComponent>(out var snapGrid)) return;
                var port = snapGrid.GetLocal()
                    .Select(entity => entity.TryGetComponent<GasCanisterPortComponent>(out var port) ? port : null)
                    .Where(port => port != null)
                    .FirstOrDefault();
                if (port == null) return;
                ConnectedGasCanisterPort = port;
                ConnectedGasCanisterPort.ConnectGasCanister(this);
            }
            else
            {
                ConnectedGasCanisterPort?.DisconnectGasCanister();
            }
        }
    }
}
