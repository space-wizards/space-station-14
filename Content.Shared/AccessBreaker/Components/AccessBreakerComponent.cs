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
}
