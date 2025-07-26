using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.AirTank;

/// <summary>
///     Specifies what a certain tank APPEARS to contain. Is not actually what it contains.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AirTankLooksLikeComponent : Component
{
    /// <summary>
    ///     What this air tank would normally contain
    /// </summary>
    [DataField, AutoNetworkedField]
    public AirTankLooksLike Contains = AirTankLooksLike.NotAir;
}

[Serializable, NetSerializable]
public enum AirTankLooksLike : byte
{
    Invalid,
    NotAir,
    RegularAir,
    Oxygen,
    Nitrogen,
    Plasma,
}
