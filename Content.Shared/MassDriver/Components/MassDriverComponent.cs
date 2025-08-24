using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.MassDriver.Components;

/// <summary>
/// Stores configuration and state data for a mass driver.
/// </summary>
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
    public float MaxThrowSpeed = 10.0f;

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
    public float MaxThrowDistance = 15.0f;

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

    /// <summary>
    /// Determines which mode is used by mass driver
    /// </summary>
    [DataField, AutoNetworkedField]
    public MassDriverMode Mode = MassDriverMode.Auto;

    /// <summary>
    /// How moch energy we consume when launching?
    /// </summary>
    [DataField]
    public float LaunchPowerLoad = 1000f;

    /// <summary>
    /// How moch energy we consume when just staying?
    /// </summary>
    [DataField]
    public float MassDriverPowerLoad = 100f;

    /// <summary>
    /// Determines which port is used for receive signals for launch.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> LaunchPort = "Launch";

    /// <summary>
    /// Determines whether it has been hacked.
    /// </summary>
    [AutoNetworkedField]
    public bool Hacked = false;

    /// <summary>
    /// Determines how mach speed setted up when hacked.
    /// </summary>
    [DataField]
    public float HackedSpeedRewrite = 20f;
}
