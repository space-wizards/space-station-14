using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class MeleeChemicalInjectorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(1);

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }

        [DataField("transferEfficiency")]
        private float _transferEfficiency = 1f;

        /// <summary>
        /// Whether this will inject through hardsuits or not.
        /// </summary>
        [DataField("pierceArmor"), ViewVariables(VVAccess.ReadWrite)]
        public bool PierceArmor = true;

        /// <summary>
        ///     Solution to inject from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
