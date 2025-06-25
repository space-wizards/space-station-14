using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Stealth.Components;

/// <summary>
/// Some systems can make an item temporarily invisible.
/// Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class StealthStatusEffectComponent : Component
{
    /// <summary>
    /// Time to enter invisibility.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeInTime = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// Time to come out of invisibility
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeOutTime = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// The target visibility level that the entity will aim for while under this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetVisibility = -1f;

    /// <summary>
    /// If the entity did not have a <see cref="StealthComponent"/> at the time the component was
    /// received <see cref="StealthStatusEffectComponent"/>, StealthComponent will be removed when this component removed
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveStealth = false;
}
