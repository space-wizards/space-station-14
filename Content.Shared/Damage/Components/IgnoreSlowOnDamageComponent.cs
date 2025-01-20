using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// This is used for an effect that nullifies <see cref="SlowOnDamageComponent"/> and adds an alert.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SlowOnDamageSystem))]
public sealed partial class IgnoreSlowOnDamageComponent : Component;
