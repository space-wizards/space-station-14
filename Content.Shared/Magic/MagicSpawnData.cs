namespace Content.Shared.Magic;

[ImplicitDataDefinitionForInheritors]
public abstract partial class MagicSpawnData
{

}

/// <summary>
/// Spawns 1 at the caster's feet.
/// </summary>
public sealed partial class TargetCasterPos : MagicSpawnData {}

/// <summary>
/// Targets the 3 tiles in front of the caster.
/// </summary>
public sealed partial class TargetInFront : MagicSpawnData
{
    [DataField("width")] public int Width = 3;
}
