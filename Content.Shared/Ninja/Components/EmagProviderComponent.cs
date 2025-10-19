using Content.Shared.Emag.Systems;
using Content.Shared.Ninja.Systems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for emagging things on click.
/// No charges but checks against a whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EmagProviderSystem))]
public sealed partial class EmagProviderComponent : Component
{
    /// <summary>
    /// The tag that marks an entity as immune to emagging.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> AccessBreakerImmuneTag = "AccessBreakerImmune";

    /// <summary>
    /// Whitelist that entities must be on to work.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// What type of emag this will provide.
    /// </summary>
    [DataField]
    public EmagType EmagType = EmagType.Access;

    /// <summary>
    /// What sound should the emag play when used
    /// </summary>
    [DataField]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");
}
