using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.MassDriver.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MassDriverComponent : Component
{
    /// <summary>
    /// Current Mass Driver Throw Speed
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentThrowSpeed = 10.0f;

    /// <summary>
    /// Max Mass Driver Throw Speed which can be setted by console.
    /// </summary>
    [DataField]
    public float MaxThrowSpeed = 15.0f;

    /// <summary>
    /// Min Mass Driver Throw Speed which can be setted by console.
    /// </summary>
    [DataField]
    public float MinThrowSpeed = 5.0f;

    /// <summary>
    /// Current Mass Driver Throw Distance
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentThrowDistance = 5.0f;

    /// <summary>
    /// Max Mass Driver Throw Distance which can be setted by console.
    /// </summary>
    [DataField]
    public float MaxThrowDistance = 10.0f;

    /// <summary>
    /// Min Mass Driver Throw Distance which can be setted by console.
    /// </summary>
    [DataField]
    public float MinThrowDistance = 2.0f;

    /// <summary>
    /// Determines how much speed or distance will be subtracted from each additional entity
    /// Example: ThrowSpeed = 10.0f, EntityCount = 2, RealThrowSpeed = 10.0f - 0.5f = 9.5f
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThrowCountDelta = 0.5f;

    /// <summary>
    /// How moch time we need to wait for throw?
    /// </summary>
    [DataField]
    public TimeSpan ThrowDelay = TimeSpan.FromSeconds(2);
}