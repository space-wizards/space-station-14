using Content.Shared.Atmos;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Atmos
{
    [Serializable, NetSerializable]
    public struct GasProperty
    {
        /// <summary>
        /// The type of this gas
        /// </summary>
        public string GasId;

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
