using Content.Shared.Ninja.Systems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for emagging things on click.
/// No charges but checks against a whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(EmagProviderSystem))]
public sealed partial class EmagProviderComponent : Component
{
    /// <summary>
    /// The tag that marks an entity as immune to emagging.
    /// </summary>
    [DataField("emagImmuneTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string EmagImmuneTag = "EmagImmune";

    /// <summary>
    /// Whitelist that entities must be on to work.
    /// </summary>
    [DataField("whitelist"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityWhitelist? Whitelist = null;
}
