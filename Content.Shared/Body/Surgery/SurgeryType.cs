using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Surgery
{
    /// <summary>
    ///     Types of surgery operations that can be performed.
    /// </summary>
    // TODO BODY Move this to YAML?
    [Serializable, NetSerializable]
    public enum SurgeryType
    {
        None = 0,
        Incision,
        Retraction,
        Cauterization,
        VesselCompression,
        Drilling,
        Amputation
    }
}
