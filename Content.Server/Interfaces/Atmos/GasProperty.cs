using Content.Shared.Atmos;

namespace Content.Server.Interfaces.Atmos
{
    public struct GasProperty
    {
        /// <summary>
        /// The type of this gas
        /// </summary>
        public Gas Gas;

        /// <summary>
        /// The quantity, in mols, of the gas
        /// </summary>
        public float Quantity;

        /// <summary>
        /// The partial pressure of this gas, in kilopascals
        /// </summary>
        public float PartialPressure;
    }
}
