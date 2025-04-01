using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Whenever this item is used upon by an entity, with a tag or component within a whitelist, in the hand of a user, play a sound
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmitSoundOnInteractUsingComponent : BaseEmitSoundComponent
{
    /// <summary>
    /// The <see cref="EntityWhitelist"/> for the entities that can use this item.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();
}
