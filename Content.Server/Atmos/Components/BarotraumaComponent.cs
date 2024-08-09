using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BarotraumaComponent : Component
    {
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("maxDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MaxDamage = 200;

        /// <summary>
        ///     Used to keep track of when damage starts/stops. Useful for logs.
        /// </summary>
        public bool TakingDamage = false;

        /// <summary>
        ///     These are the inventory slots that are checked for pressure protection. If a slot is missing protection, no protection is applied.
        /// </summary>
        [DataField("protectionSlots")]
        public List<string> ProtectionSlots = new() { "head", "outerClothing" };

        /// <summary>
        /// Cached pressure protection values
        /// </summary>
        [ViewVariables]
        public float HighPressureMultiplier = 1f;
        [ViewVariables]
        public float HighPressureModifier = 0f;
        [ViewVariables]
        public float LowPressureMultiplier = 1f;
        [ViewVariables]
        public float LowPressureModifier = 0f;

        /// <summary>
        /// Whether the entity is immuned to pressure (i.e possess the PressureImmunity component)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool HasImmunity = false;

        [DataField]
        public ProtoId<AlertPrototype> HighPressureAlert = "HighPressure";

        [DataField]
        public ProtoId<AlertPrototype> LowPressureAlert = "LowPressure";

        [DataField]
        public ProtoId<AlertCategoryPrototype> PressureAlertCategory = "Pressure";
    }
}
