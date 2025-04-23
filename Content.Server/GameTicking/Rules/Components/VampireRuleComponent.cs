using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Vampire.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(VampireRuleSystem))]
public sealed partial class VampireRuleComponent : Component
{
    public readonly List<EntityUid> VampireMinds = new();
    

    public readonly List<ProtoId<EntityPrototype>> BaseObjectives = new()
    {
        "VampireKillRandomPersonObjective",
        "VampireDrainObjective"
    };
    
    public readonly List<ProtoId<EntityPrototype>> EscapeObjectives = new()
    {
        "VampireSurviveObjective",
        "VampireEscapeObjective"
    };
    
    public readonly List<ProtoId<EntityPrototype>> StealObjectives = new()
    {
        "CMOHyposprayVampireStealObjective",
        "RDHardsuitVampireStealObjective",
        "EnergyShotgunVampireStealObjective",
        "MagbootsVampireStealObjective",
        "ClipboardVampireStealObjective",
        "CaptainIDVampireStealObjective",
        "CaptainJetpackVampireStealObjective",
        "CaptainGunVampireStealObjective"
    };
}