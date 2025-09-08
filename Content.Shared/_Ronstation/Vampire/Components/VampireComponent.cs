using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Ronstation.Vampire.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampireComponent : Component
{

    public override bool SessionSpecific => true;

    /// <summary>
    /// The total amount of Vitae the vampire has.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 Vitae = 20;

    /// <summary>
    /// The entity's current max amount of Vitae. Can be increased
    /// through use of the Feed action.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxVitae")]
    public FixedPoint2 VitaeRegenCap = 20;

    /// <summary>
    /// The amount of vitae passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("vitaePerSecond")]
    public FixedPoint2 VitaePerSecond = 0.5f;

    /// <summary>
    /// The amount of vitae gained with each successful feed do-after.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("vitaePerFeed")]
    public FixedPoint2 VitaeGainOnDoAfter = 1f;

    /// <summary>
    /// The amount of maximum vitae gained on level-up.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("vitaeCapUpgradeAmount")]
    public float VitaeCapUpgradeAmount = 40f;

    /// <summary>
    /// The total amount of vitae stolen from targets using the feed action.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("stolenVitae")]
    public FixedPoint2 StolenVitae = 0f;

    /// <summary>
    /// How much stolen vitae is required before you can 'levelup'.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("levelupValue")]
    public FixedPoint2 LevelUpValue = 40f; // Two full drinks without interruption

    [ViewVariables]
    public float Accumulator = 0;

    [DataField]
    public ProtoId<AlertPrototype> VitaeAlert = "Vitae";

}