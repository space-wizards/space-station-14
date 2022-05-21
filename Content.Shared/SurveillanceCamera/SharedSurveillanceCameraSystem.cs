using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

[Serializable, NetSerializable]
public enum SurveillanceCameraVisuals
{
    Key,
    Active,
    InUse,
    Disabled,
    // Reserved for future use
    Xray,
    Emp
}
