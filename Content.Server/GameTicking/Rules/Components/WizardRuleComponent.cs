using Content.Server.GameTicking.Rules;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule for simple antagonists that have fixed objectives.
/// </summary>
[RegisterComponent, Access(typeof(WizardRuleComponent))]
public sealed partial class WizardRuleComponent : Component
{

    [DataField]
    public List<EntityUid> Wizards = new();

    [DataField]
    public int MaxWizards = 1;

    [DataField]
    public EntityUid WizardShuttle = EntityUid.Invalid;

    [DataField]
    public ProtoId<AntagPrototype> WizardPrototypeId = "Wizard";


}
