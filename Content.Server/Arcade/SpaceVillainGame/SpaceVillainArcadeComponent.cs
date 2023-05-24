using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Arcade;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

// TODO: ECS.

namespace Content.Server.Arcade.SpaceVillain;

[RegisterComponent]
public sealed class SpaceVillainArcadeComponent : SharedSpaceVillainArcadeComponent
{
    [ViewVariables] public bool OverflowFlag;
    [ViewVariables] public bool PlayerInvincibilityFlag;
    [ViewVariables] public bool EnemyInvincibilityFlag;
    [ViewVariables] public SpaceVillainGame Game = null!;

    [DataField("newGameSound")]
    public SoundSpecifier NewGameSound = new SoundPathSpecifier("/Audio/Effects/Arcade/newgame.ogg");
    [DataField("playerAttackSound")]
    public SoundSpecifier PlayerAttackSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_attack.ogg");
    [DataField("playerHealSound")]
    public SoundSpecifier PlayerHealSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_heal.ogg");
    [DataField("playerChargeSound")]
    public SoundSpecifier PlayerChargeSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_charge.ogg");
    [DataField("winSound")]
    public SoundSpecifier WinSound = new SoundPathSpecifier("/Audio/Effects/Arcade/win.ogg");
    [DataField("gameOverSound")]
    public SoundSpecifier GameOverSound = new SoundPathSpecifier("/Audio/Effects/Arcade/gameover.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleFightVerbs")]
    public List<string> PossibleFightVerbs = new List<string>()
        {"Defeat", "Annihilate", "Save", "Strike", "Stop", "Destroy", "Robust", "Romance", "Pwn", "Own"};

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleFirstEnemyNames")]
    public List<string> PossibleFirstEnemyNames = new List<string>(){
        "the Automatic", "Farmer", "Lord", "Professor", "the Cuban", "the Evil", "the Dread King",
        "the Space", "Lord", "the Great", "Duke", "General"
    };

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleLastEnemyNames")]
    public List<string> PossibleLastEnemyNames = new List<string>()
    {
        "Melonoid", "Murdertron", "Sorcerer", "Ruin", "Jeff", "Ectoplasm", "Crushulon", "Uhangoid",
        "Vhakoid", "Peteoid", "slime", "Griefer", "ERPer", "Lizard Man", "Unicorn"
    };

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleRewards", customTypeSerializer:typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> PossibleRewards = new List<string>()
    {
        "ToyMouse", "ToyAi", "ToyNuke", "ToyAssistant", "ToyGriffin", "ToyHonk", "ToyIan",
        "ToyMarauder", "ToyMauler", "ToyGygax", "ToyOdysseus", "ToyOwlman", "ToyDeathRipley",
        "ToyPhazon", "ToyFireRipley", "ToyReticence", "ToyRipley", "ToySeraph", "ToyDurand", "ToySkeleton",
        "FoamCrossbow", "RevolverCapGun", "PlushieLizard", "PlushieAtmosian", "PlushieSpaceLizard",
        "PlushieNuke", "PlushieCarp", "PlushieRatvar", "PlushieNar", "PlushieSnake", "Basketball", "Football",
        "PlushieRouny", "PlushieBee", "PlushieSlime", "BalloonCorgi", "ToySword", "CrayonBox", "BoxDonkSoftBox", "BoxCartridgeCap",
        "HarmonicaInstrument", "OcarinaInstrument", "RecorderInstrument", "GunpetInstrument", "BirdToyInstrument", "PlushieXeno"
    };

    [DataField("rewardMinAmount")]
    public int RewardMinAmount;

    [DataField("rewardMaxAmount")]
    public int RewardMaxAmount;

    [ViewVariables(VVAccess.ReadWrite)]
    public int RewardAmount = 0;
}
