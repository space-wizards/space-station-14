using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.GreyStation.Flipping;

/// <summary>
/// A coin that is currently being flipped.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(FlippableCoinSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class FlippingCoinComponent : Component
{
    /// <summary>
    /// When it is done flipping.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextFlip = TimeSpan.Zero;
}
