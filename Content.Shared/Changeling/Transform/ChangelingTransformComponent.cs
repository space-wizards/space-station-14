using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Changeling.Transform;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedChangelingTransformSystem))]
public sealed partial class ChangelingTransformComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ChangelingTransformAction = "ActionChangelingTransform";

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
    public SoundSpecifier? TransformAttemptNoise = new SoundCollectionSpecifier("ChangelingTransformAttempt");

    /// <summary>
    /// The currently active transform in the world
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTransformSound;
}

