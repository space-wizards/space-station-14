using Content.Server._DarkAscent.Temperature.Firebase;
using Robust.Shared.Audio;

namespace Content.Server._DarkAscent.Temperature.Firebase;

[RegisterComponent, Access(typeof(FirebaseSystem))]
public sealed partial class FirebaseComponent : Component
{
    /// <summary>
    /// Current amount of fuel.
    /// </summary>
    [DataField]
    public float Fuel = 5f;

    /// <summary>
    /// Amount of fuel burnt every firebase update.
    /// </summary>
    [DataField]
    public float FuelBurntPerUpdate = 0.5f;

    /// <summary>
    /// Amount to increase or decrease flame intensity.
    /// Used to make sure the flame from the Flammable component is extinguished if we run out of fuel.
    /// </summary>
    [DataField]
    public float FireFadeTick = 0.1f;

    /// <summary>
    /// Frequency with which the firebase is updated.
    /// </summary>
    [DataField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(1f);

    [DataField]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;
}

