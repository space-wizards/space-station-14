using Content.Server.GameTicking.Rules;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Map;
using Content.Server.Maps;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule for simple antagonists that have fixed objectives.
/// </summary>
[RegisterComponent, Access(typeof(WizardRuleSystem))]
public sealed partial class WizardRuleComponent : Component
{

    public EntityUid  WizardShuttle = EntityUid.Invalid;
    public EntityUid? WizardOutpost;

    [DataField]
    public List<ICommonSession> Wizards = new();

    [DataField]
    public Dictionary<string, string> WizardNames = new();

    [DataField]
    public ProtoId<StartingGearPrototype> GearProto = "WizardBlueGear";

    [DataField]
    public int MaxWizards = 1;

    [DataField]
    public int MaxObjectiveDifficulty = 3;

    [DataField]
    public ProtoId<AntagPrototype> WizardPrototypeId = "Wizard";

    [DataField]
    public ProtoId<GameMapPrototype> OutpostMapPrototype = "WizardOutpost";

    [DataField]
    public EntProtoId SpawnPointProto = "SpawnPointWizards";

    [DataField]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/wizard_greeting.ogg");


}
