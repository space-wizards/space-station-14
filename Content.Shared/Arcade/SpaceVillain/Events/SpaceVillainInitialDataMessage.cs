using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.SpaceVillain.Events;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class SpaceVillainInitialDataMessage(byte playerHP, byte playerMP, string villainName, byte villainHP, byte villainMP) : BoundUserInterfaceMessage
{
    /// <summary>
    ///
    /// </summary>
    public byte PlayerHP = playerHP;

    /// <summary>
    ///
    /// </summary>
    public byte PlayerMP = playerMP;

    /// <summary>
    ///
    /// </summary>
    public string VillainName = villainName;

    /// <summary>
    ///
    /// </summary>
    public byte VillainHP = villainHP;

    /// <summary>
    ///
    /// </summary>
    public byte VillainMP = villainMP;
}
