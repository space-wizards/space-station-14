using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// Lets a drink burst open when thrown while closed.
/// Requires <see cref="DrinkComponent"/> and <see cref="OpenableComponent"/> to work.
/// </summary>
[RegisterComponent, Access(typeof(DrinkSystem))]
public sealed partial class PressurizedDrinkComponent : Component
{
    /// <summary>
    /// Chance for the drink to burst when thrown while closed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BurstChance = 0.25f;

    /// <summary>
    /// Sound played when the drink bursts.
    /// </summary>
    [DataField]
    public SoundSpecifier BurstSound = new SoundPathSpecifier("/Audio/Effects/flash_bang.ogg")
    {
        Params = AudioParams.Default.WithVolume(-4)
    };
}
