using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid.Components;

/// <summary>
///     This is added to a marker entity in order to spawn a randomized
///     humanoid ingame.
/// </summary>
[RegisterComponent]
public sealed class RandomHumanoidComponent : Component
{
    [DataField("settings")] public string RandomSettingsId = default!;
}

