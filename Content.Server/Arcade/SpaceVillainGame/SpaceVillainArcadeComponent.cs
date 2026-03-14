using Content.Shared.Arcade;
using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

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
    [DataField]
    public SoundSpecifier NewGameSound = new SoundPathSpecifier("/Audio/Effects/Arcade/newgame.ogg");

    /// <summary>
    /// The sound played when the player chooses to attack.
    /// </summary>
    [DataField]
    public SoundSpecifier PlayerAttackSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_attack.ogg");

    /// <summary>
    /// The sound played when the player chooses to heal.
    /// </summary>
    [DataField]
    public SoundSpecifier PlayerHealSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_heal.ogg");

    /// <summary>
    /// The sound played when the player chooses to regain mana.
    /// </summary>
    [DataField]
    public SoundSpecifier PlayerChargeSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_charge.ogg");

    /// <summary>
    /// The sound played when the player wins.
    /// </summary>
    [DataField]
    public SoundSpecifier WinSound = new SoundPathSpecifier("/Audio/Effects/Arcade/win.ogg");

    /// <summary>
    /// The sound played when the player loses.
    /// </summary>
    [DataField]
    public SoundSpecifier GameOverSound = new SoundPathSpecifier("/Audio/Effects/Arcade/gameover.ogg");

    /// <summary>
    /// The prefixes that can be used to create the game name.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PossibleFightVerbs = "SpaceVillainVerbsFight";

    /// <summary>
    /// The first names/titles that can be used to construct the name of the villain.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PossibleFirstEnemyNames = "SpaceVillainNamesEnemyFirst";

    /// <summary>
    /// The last names that can be used to construct the name of the villain.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PossibleLastEnemyNames = "SpaceVillainNamesEnemyLast";

    /// <summary>
    /// The prototypes that can be dispensed as a reward for winning the game.
    /// </summary>
    [DataField]
    public List<EntProtoId> PossibleRewards = new();

    /// <summary>
    /// The minimum number of prizes the arcade machine can have.
    /// </summary>
    [DataField]
    public int RewardMinAmount;

    /// <summary>
    /// The maximum number of prizes the arcade machine can have.
    /// </summary>
    [DataField]
    public int RewardMaxAmount;

    /// <summary>
    /// The remaining number of prizes the arcade machine can dispense.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int RewardAmount = 0;
}
