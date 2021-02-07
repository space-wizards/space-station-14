#nullable enable
using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{

    /// <summary>
    ///     This is a solar panel.
    ///     It generates power from the sun based on coverage.
    /// </summary>
    [RegisterComponent]
    public class SolarPanelComponent : Component, IBreakAct
    {
        public override string Name => "SolarPanel";

        /// <summary>
        /// Maximum supply output by this panel (coverage = 1)
        /// </summary>
        private int _maxSupply = 1500;
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxSupply
        {
            get => _maxSupply;
            set {
                _maxSupply = value;
                UpdateSupply();
            }
        }

        /// <summary>
        /// Current coverage of this panel (from 0 to 1).
        /// This is updated by <see cref='PowerSolarSystem'/>.
        /// </summary>
        private float _coverage = 0;
        [ViewVariables]
        public float Coverage
        {
            get => _coverage;
            set {
                // This gets updated once-per-tick, so avoid updating it if truly unnecessary
                if (_coverage != value) {
                    _coverage = value;
                    UpdateSupply();
                }
            }
        }

        /// <summary>
        /// The game time (<see cref='IGameTiming'/>) of the next coverage update.
        /// This may have a random offset applied.
        /// This is used to reduce solar panel updates and stagger them to prevent lagspikes.
        /// This should only be updated by the PowerSolarSystem but is viewable for debugging.
        /// </summary>
        [ViewVariables]
        public TimeSpan TimeOfNextCoverageUpdate = TimeSpan.MinValue;

        private void UpdateSupply()
        {
            if (Owner.TryGetComponent<PowerSupplierComponent>(out var supplier))
            {
                supplier.SupplyRate = (int) (_maxSupply * _coverage);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<PowerSupplierComponent>();

            UpdateSupply();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _maxSupply, "maxsupply", 1500);
        }

        public void OnBreak(BreakageEventArgs args)
        {
            if (!Owner.TryGetComponent<SpriteComponent>(out var sprite))
                return;

            sprite.LayerSetState(0, "broken");
            MaxSupply = 0;
        }
    }
}
