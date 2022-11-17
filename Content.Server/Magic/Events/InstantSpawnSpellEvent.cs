using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

public sealed class InstantSpawnSpellEvent : InstantActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("preventCollide")]
    public bool PreventCollideWithCaster = true;

    /// <summary>
    /// Gets the targeted spawn positons; may lead to multiple entities being spawned.
    /// </summary>
    [DataField("posData")] public MagicSpawnData Pos = new TargetCasterPos();
}

[ImplicitDataDefinitionForInheritors]
public abstract class MagicSpawnData
{

}

/// <summary>
/// Spawns 1 at the caster's feet.
/// </summary>
public sealed class TargetCasterPos : MagicSpawnData {}

/// <summary>
/// Targets the 3 tiles in front of the caster.
/// </summary>
public sealed class TargetInFront : MagicSpawnData
{
    [DataField("width")]
    public int Width = 3;
}
