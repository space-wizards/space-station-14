using Content.Server.Atmos;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => Volume, "volume", 10);
        }

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
