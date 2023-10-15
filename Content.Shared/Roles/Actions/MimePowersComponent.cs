using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles.Actions;

/// <summary>
/// Lets its owner entity use mime powers, like placing invisible walls.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MimePowersComponent : Component
{
    /// <summary>
    /// Whether this component is active or not.
    /// </summarY>
    [DataField("enabled"), AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The wall prototype to use.
    /// </summary>
    [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WallPrototype = "WallInvisible";

    [DataField("invisibleWallAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? InvisibleWallAction = "ActionMimeInvisibleWall";

    [DataField("invisibleWallActionEntity"), AutoNetworkedField]
    public EntityUid? InvisibleWallActionEntity;

    // The vow zone lies below
    public bool VowBroken = false;

    /// <summary>
    /// Whether this mime is ready to take the vow again.
    /// Note that if they already have the vow, this is also false.
    /// </summary>
    public bool ReadyToRepent = false;

    /// <summary>
    /// Time when the mime can repent their vow
    /// </summary>
    [DataField("vowRepentTime", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan VowRepentTime = TimeSpan.Zero;

    /// <summary>
    /// How long it takes the mime to get their powers back
    /// </summary>
    [DataField("vowCooldown"), AutoNetworkedField]
    public TimeSpan VowCooldown = TimeSpan.FromMinutes(5);
}
