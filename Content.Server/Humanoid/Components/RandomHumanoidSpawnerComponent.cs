using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Humanoid.Components;

/// <summary>
///   This is added to a marker entity in order to spawn a randomized
///   humanoid ingame.
///   If you are using a GhostRoleSpawnerComponent, replace it with GhostRoleRandomSpawnerComponent instead of this.
/// </summary>
[RegisterComponent]
public sealed class RandomHumanoidSpawnerComponent : Component
{
    [DataField("settings", customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>))]
    public string SettingsPrototypeId = default!;
}
