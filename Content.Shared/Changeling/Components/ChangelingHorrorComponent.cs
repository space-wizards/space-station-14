using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marks an entity as a changeling horror & stores horror-related datafields.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ChangelingHorrorComponent : Component
{
    /// <summary>
    /// Station-wide announcement sound that is played when the changeling enters its horror form.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SpawnAnnouncementSound;

    /// <summary>
    /// Local sound that is played when a changeling enters its horror form.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SpawnSound;

    /// <summary>
    /// The screech vfx to spawn when the changeling turns into an horror
    /// </summary>
    [DataField]
    public EntProtoId SpawnScreech = "EffectScreech";

    /// <summary>
    /// The instant at which the changeling entered horror form
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InitialTime = TimeSpan.Zero;

    /// <summary>
    /// The amount of time it can stay transformed, converted from its DNA
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimeBudget = TimeSpan.Zero;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> TimeAlert = "ChangelingHorrorTime";

    [DataField]
    public EntProtoId SpawnScreechVfx = "EffectScreechChangelingHorrorSpawn";

    /// <summary>
    /// The disarming range of the spawn screech
    /// </summary>
    [DataField]
    public float SpawnScreechRange = 30f;

    /// <summary>
    /// How many seconds you are given for free when transforming (so wholesome!)
    /// </summary>
    [DataField]
    public double GracePeriod = 5d;

    /// <summary>
    /// How many seconds of transformation you are given for each DNA point.
    /// </summary>
    [DataField]
    public double SecondPerDNA = 3d;
}
