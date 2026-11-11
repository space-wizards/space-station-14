using Content.Shared.Alert;
using Content.Shared.EntityEffects;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marks an entity as a changeling horror & stores horror-related datafields.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ChangelingHorrorComponent : Component
{
    /// <summary>
    /// Local sound that is played when a changeling enters its horror form.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SpawnSound;

    /// <summary>
    /// The screech vfx to spawn when the changeling turns into an horror
    /// </summary>
    [DataField]
    public EntProtoId SpawnScreech = "AdminInstantEffectScreechLarge";

    /// <summary>
    /// The time at which the changeling will leave horror form.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    /// <summary>
    /// Alert that is displayed to show the amount of time remaining in the horror form
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> TimeAlert = "ChangelingHorrorTime";

    /// <summary>
    /// How many seconds you are given for free when transforming (so wholesome!)
    /// </summary>
    [DataField]
    public TimeSpan GracePeriod = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How many seconds of transformation you are given for each DNA point.
    /// </summary>
    [DataField]
    public TimeSpan SecondPerDNA = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Effects applied to the changeling upon transforming
    /// </summary>
    [DataField]
    public EntityEffect[]? SpawnEffects;

    /// <summary>
    /// The identity that was used before entering the horror form
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LastIdentity;

    /// <summary>
    /// Amount of time you'll get stunned and knocked down if you run out of points
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Minimum amount of DNA needed to transform into this horror form.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinimumDna = 10;

    /// <summary>
    /// Actions that are granted to the horror upon transformation
    /// </summary>
    [DataField]
    public EntProtoId[]? Actions;

    /// <summary>
    /// Actions that are removed upon transformation
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> StoredActions = new();
}
