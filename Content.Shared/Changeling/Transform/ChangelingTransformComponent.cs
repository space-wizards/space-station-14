using Content.Shared.Cloning;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Transform;

/// <summary>
/// The component containing information about Changelings Transformation action
/// Like how long their windup is, the sounds as well as the Target Cloning settings for changing between identities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ChangelingTransformSystem))]
public sealed partial class ChangelingTransformComponent : Component
{
    /// <summary>
    /// The action Prototype for Transforming
    /// </summary>
    [DataField]
    public EntProtoId? ChangelingTransformAction = "ActionChangelingTransform";

    /// <summary>
    /// The Action Entity for transforming associated with this Component
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingTransformActionEntity;

    /// <summary>
    /// Time it takes to Transform
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TransformWindup = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The noise used when attempting to transform
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? TransformAttemptNoise = new SoundCollectionSpecifier("ChangelingTransformAttempt", AudioParams.Default.WithMaxDistance(6)); // 6 distance due to the default 15 being hearable all the way across PVS. Changeling is meant to be stealthy. 6 still allows the sound to be hearable, but not across an entire department.

    /// <summary>
    /// The currently active transform in the world
    /// </summary>
    [DataField]
    public EntityUid? CurrentTransformSound;

    /// <summary>
    /// The cloning settings passed to the CloningSystem, contains a list of all components to copy or have handled by their
    /// respective systems.
    /// </summary>
    public ProtoId<CloningSettingsPrototype> TransformCloningSettings = "ChangelingCloningSettings";

    public override bool SendOnlyToOwner => true;
}

