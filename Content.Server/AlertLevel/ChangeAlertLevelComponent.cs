using Content.Shared.ChangeAlertLevel;
using Robust.Shared.Audio;

namespace Content.Server.ChangeAlertLevel;

/// <summary>
///     Sets an alert level when activated
/// </summary>
[RegisterComponent]
[Access(typeof(ChangeAlertLevelSystem))]
public abstract partial class ChangeAlertLevelComponent : SharedChangeAlertLevelComponent
{
}
