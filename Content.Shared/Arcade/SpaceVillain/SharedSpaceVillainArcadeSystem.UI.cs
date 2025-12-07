using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.SpaceVillain;

[Serializable, NetSerializable]
public enum SpaceVillainArcadeUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SpaceVillainArcadePlayerActionMessage : BoundUserInterfaceMessage
{
    public readonly SpaceVillainPlayerAction PlayerAction;
    public SpaceVillainArcadePlayerActionMessage(SpaceVillainPlayerAction playerAction)
    {
        PlayerAction = playerAction;
    }
}

[Serializable, NetSerializable]
public sealed class SpaceVillainArcadeMetaDataUpdateMessage : SpaceVillainArcadeDataUpdateMessage
{
    public readonly string GameTitle;
    public readonly string EnemyName;
    public readonly bool ButtonsDisabled;

    public SpaceVillainArcadeMetaDataUpdateMessage(int playerHp,
        int playerMp,
        int enemyHp,
        int enemyMp,
        string playerActionMessage,
        string enemyActionMessage,
        string gameTitle,
        string enemyName,
        bool buttonsDisabled)
        : base(playerHp, playerMp, enemyHp, enemyMp, playerActionMessage, enemyActionMessage)
    {
        GameTitle = gameTitle;
        EnemyName = enemyName;
        ButtonsDisabled = buttonsDisabled;
    }
}

[Serializable, NetSerializable, Virtual]
public class SpaceVillainArcadeDataUpdateMessage : BoundUserInterfaceMessage
{
    public readonly int PlayerHP;
    public readonly int PlayerMP;
    public readonly int EnemyHP;
    public readonly int EnemyMP;
    public readonly string PlayerActionMessage;
    public readonly string EnemyActionMessage;

    public SpaceVillainArcadeDataUpdateMessage(int playerHp,
        int playerMp,
        int enemyHp,
        int enemyMp,
        string playerActionMessage,
        string enemyActionMessage)
    {
        PlayerHP = playerHp;
        PlayerMP = playerMp;
        EnemyHP = enemyHp;
        EnemyMP = enemyMp;
        EnemyActionMessage = enemyActionMessage;
        PlayerActionMessage = playerActionMessage;
    }
}
