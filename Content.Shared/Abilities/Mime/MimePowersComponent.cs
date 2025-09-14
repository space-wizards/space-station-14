using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Abilities.Mime;

/// <summary>
/// Lets its owner entity use mime powers, like placing invisible walls.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class MimePowersComponent : Component
{
    /// <summary>
    /// Whether this component is active or not.
    /// </summarY>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The wall prototype to use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId WallPrototype = "WallInvisible";

    [DataField]
    public EntProtoId? InvisibleWallAction = "ActionMimeInvisibleWall";

    [DataField, AutoNetworkedField]
    public EntityUid? InvisibleWallActionEntity;

    // The vow zone lies below
    [DataField, AutoNetworkedField]
    public bool VowBroken = false;

    /// <summary>
    /// Whether this mime is ready to take the vow again.
    /// Note that if they already have the vow, this is also false.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReadyToRepent = false;

    /// <summary>
    /// Time when the mime can repent their vow
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan VowRepentTime = TimeSpan.Zero;

    /// <summary>
    /// How long it takes the mime to get their powers back
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan VowCooldown = TimeSpan.FromMinutes(5);

    [DataField]
    public ProtoId<AlertPrototype> VowAlert = "VowOfSilence";

    [DataField]
    public ProtoId<AlertPrototype> VowBrokenAlert = "VowBroken";

    /// <summary>
    /// Does this component prevent the mime from writing on paper while their vow is active?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreventWriting = false;

    /// <summary>
    /// What message is displayed when the mime fails to write?
    /// </summary>
    [DataField]
    public LocId FailWriteMessage = "paper-component-illiterate-mime";

    public override bool SendOnlyToOwner => true;
}
