using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.AccessBreaker;

[Access(typeof(AccessBreakerSystem))]
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AccessBreakerComponent : Component
{
    /// <summary>
    /// The tag that marks an entity as immune to the access breaker.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    [AutoNetworkedField]
    public string AccessBreakerImmuneTag = "AccessBreakerImmune";

    /// <summary>
    /// The last target of the access breaker.
    /// Used so you cannot accidentally break the access twice on the same target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid LastTarget;
}
