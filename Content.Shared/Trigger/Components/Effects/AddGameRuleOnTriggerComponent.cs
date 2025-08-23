using Content.Shared.GameTicking.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Adds and starts a new game rule on a trigger.
/// The user is always logged alongside the game rule and this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddGameRuleOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The game rule that will be added. Entity requires <see cref="GameRuleComponent"/> to work.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId GameRule = "CockroachMigration";

    /// <summary>
    /// Whether to also start the game rule when adding it.
    /// You almost always want this to be true.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool StartRule = true;
}
