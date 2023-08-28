using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for tagging a spawn point as a nuke operative spawn point
/// and providing loadout + name for the operative on spawn.
/// TODO: Remove once systems can request spawns from the ghost role system directly.
/// </summary>
[RegisterComponent]
public sealed partial class NukeOperativeSpawnerComponent : Component
{
    [DataField("name", required:true)]
    public string OperativeName = default!;

    [DataField("rolePrototype", customTypeSerializer:typeof(PrototypeIdSerializer<AntagPrototype>), required:true)]
    public string OperativeRolePrototype = default!;

    [DataField("startingGearPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<StartingGearPrototype>), required:true)]
    public string OperativeStartingGear = default!;
}
