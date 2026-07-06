using Content.Shared.Light.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Used for the Impaired Mobility disability trait.
/// Applies a base movement speed reduction as determined by the SpeedModifier field.
/// Also increases the time it takes to stand up after falling, as determined by the StandUpTimeModifier field.
/// When an entity holds an item with the MobilityAidComponent, the speed penalty is nullified.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedNyctophobiaSystem))]
public sealed partial class NyctophobiaComponent : Component, ILightLevelDependent
{
    [DataField, AutoNetworkedField]
    public bool InDarkness = false;

    [DataField, AutoNetworkedField]
    public float DarknessThreshold = 0.2f;
    /// <summary>
    /// The movement speed modifier applied to the player (0.4 is 40% slower)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.25f;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateCooldown { get; set; } = TimeSpan.FromSeconds(3);
}
