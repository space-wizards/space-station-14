using System;
using Content.Server.Power.Components;
using Content.Server.Solar.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Solar.Components
{

    /// <summary>
    ///     This is a solar panel.
    ///     It generates power from the sun based on coverage.
    /// </summary>
    [RegisterComponent]
    public class SolarPanelComponent : Component
    {
        public override string Name => "SolarPanel";

        /// <summary>
        /// Maximum supply output by this panel (coverage = 1)
        /// </summary>
        [DataField("maxSupply")]
        private int _maxSupply = 1500;

        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxSupply
        {
            get => _maxSupply;
            set {
                _maxSupply = value;
                // This should almost never be called, shush.
                EntitySystem.Get<PowerSolarSystem>().UpdateSupply(OwnerUid, this);
            }
        }

        /// <summary>
        /// Current coverage of this panel (from 0 to 1).
        /// This is updated by <see cref='PowerSolarSystem'/>.
        /// DO NOT WRITE WITHOUT CALLING UpdateSupply()!
        /// </summary>
        [ViewVariables]
        public float Coverage { get; set; } = 0;
    }
}
