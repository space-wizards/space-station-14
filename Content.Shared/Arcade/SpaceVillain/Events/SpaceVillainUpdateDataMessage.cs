using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.SpaceVillain.Events;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class SpaceVillainUpdateDataMessage(int playerHP, int playerMP, string villainName, int villainHP, int villainMP, string playerStatus, string villainStatus) : BoundUserInterfaceMessage
{
    /// <summary>
    ///
    /// </summary>
    public int PlayerHP = playerHP;

    /// <summary>
    ///
    /// </summary>
    public int PlayerMP = playerMP;

    /// <summary>
    ///
    /// </summary>
    public string VillainName = villainName;

    /// <summary>
    ///
    /// </summary>
    public int VillainHP = villainHP;

    /// <summary>
    ///
    /// </summary>
    public int VillainMP = villainMP;

    /// <summary>
    ///
    /// </summary>
    public string PlayerStatus = playerStatus;

    /// <summary>
    ///
    /// </summary>
    public string VillainStatus = villainStatus;
}
