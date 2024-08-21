using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// A simple clumsy tag-component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClumsyComponent : Component
{
    /// <summary>
    /// Damage dealt to a clumsy character when they try to fire a gun.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier ClumsyDamage = default!;

    /// <summary>
    /// Sound to play when clumsy interactions fail.
    /// </summary>
    [DataField]
    public SoundSpecifier ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");
}
