using Content.Shared.Arcade;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Arcade.SpaceVillain;

[RegisterComponent]
public sealed partial class SpaceVillainArcadeComponent : SharedSpaceVillainArcadeComponent
{
    /// <summary>
    /// Unused flag that can be hacked via wires.
    /// Name suggests that it was intended to either make the health/mana values underflow while playing the game or turn the arcade machine into an infinite prize fountain.
    /// </summary>
    [ViewVariables]
    public bool OverflowFlag;

    /// <summary>
    /// The current session of the SpaceVillain game for this arcade machine.
    /// </summary>
    [ViewVariables]
    public SpaceVillainGame? Game = null;

    /// <summary>
    /// The sound played when a new session of the SpaceVillain game is begun.
    /// </summary>
    [DataField("newGameSound")]
    public SoundSpecifier NewGameSound = new SoundPathSpecifier("/Audio/Effects/Arcade/newgame.ogg");

    /// <summary>
    /// The sound played when the player chooses to attack.
    /// </summary>
    [DataField("playerAttackSound")]
    public SoundSpecifier PlayerAttackSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_attack.ogg");

    /// <summary>
    /// The sound played when the player chooses to heal.
    /// </summary>
    [DataField("playerHealSound")]
    public SoundSpecifier PlayerHealSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_heal.ogg");

    /// <summary>
    /// The sound played when the player chooses to regain mana.
    /// </summary>
    [DataField("playerChargeSound")]
    public SoundSpecifier PlayerChargeSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_charge.ogg");

    /// <summary>
    /// The sound played when the player wins.
    /// </summary>
    [DataField("winSound")]
    public SoundSpecifier WinSound = new SoundPathSpecifier("/Audio/Effects/Arcade/win.ogg");

    /// <summary>
    /// The sound played when the player loses.
    /// </summary>
    [DataField("gameOverSound")]
    public SoundSpecifier GameOverSound = new SoundPathSpecifier("/Audio/Effects/Arcade/gameover.ogg");

    /// <summary>
    /// The prefixes that can be used to create the game name.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleFightVerbs")]
    public List<string> PossibleFightVerbs = new()
        {"Defeat", "Annihilate", "Save", "Strike", "Stop", "Destroy", "Robust", "Romance", "Pwn", "Own"};

    /// <summary>
    /// The first names/titles that can be used to construct the name of the villain.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleFirstEnemyNames")]
    public List<string> PossibleFirstEnemyNames = new(){
        "the Automatic", "Farmer", "Lord", "Professor", "the Cuban", "the Evil", "the Dread King",
        "the Space", "Lord", "the Great", "Duke", "General"
    };

    /// <summary>
    /// The last names that can be used to construct the name of the villain.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleLastEnemyNames")]
    public List<string> PossibleLastEnemyNames = new()
    {
        "Melonoid", "Murdertron", "Sorcerer", "Ruin", "Jeff", "Ectoplasm", "Crushulon", "Uhangoid",
        "Vhakoid", "Peteoid", "slime", "Griefer", "ERPer", "Lizard Man", "Unicorn"
    };

    /// <summary>
    /// The prototypes that can be dispensed as a reward for winning the game.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleRewards", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> PossibleRewards = new();

    /// <summary>
    /// The minimum number of prizes the arcade machine can have.
    /// </summary>
    [DataField("rewardMinAmount")]
    public int RewardMinAmount;

    /// <summary>
    /// The maximum number of prizes the arcade machine can have.
    /// </summary>
    [DataField("rewardMaxAmount")]
    public int RewardMaxAmount;

    /// <summary>
    /// The remaining number of prizes the arcade machine can dispense.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int RewardAmount = 0;
}
