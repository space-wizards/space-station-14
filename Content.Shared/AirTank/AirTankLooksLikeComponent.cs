namespace Content.Shared.AirTank;

/// <summary>
///     Specifies what a certain tank APPEARS to contain. Is not actually what it contains.
/// </summary>
[RegisterComponent]
public sealed partial class AirTankLooksLikeComponent : Component
{
    /// <summary>
    ///     What this air tank would normally contain
    /// </summary>
    /// <returns></returns>
    [DataField]
    public AirTankLooksLike Contains = AirTankLooksLike.NotAir;
}

public enum AirTankLooksLike : byte
{
    Invalid,
    NotAir,
    RegularAir,
    Oxygen,
    Nitrogen,
    Plasma,
}
