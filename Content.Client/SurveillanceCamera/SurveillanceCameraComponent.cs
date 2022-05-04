using Content.Shared.SurveillanceCamera;
using Robust.Client.Graphics;

namespace Content.Client.SurveillanceCamera;

public sealed class SurveillanceCameraComponent : SharedSurveillanceCameraComponent
{
    // a little sus but it should be OK
    // just OK
    public IEye Eye { get; } = new FixedEye();
}
