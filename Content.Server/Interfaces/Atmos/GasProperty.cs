using Content.Server.Atmos;

namespace Content.Server.Interfaces.Atmos
{
    public struct GasProperty
    {
        /// <summary>
        /// The type of this gas
        /// </summary>
        public Gas Gas;

        /// <summary>
        /// The volume, in mols, of the gas
        /// </summary>
        public float Volume;

        /// <summary>
        /// The partial pressure of this gas, in kilopascals
        /// </summary>
        public float PartialPressure;
    }
}
