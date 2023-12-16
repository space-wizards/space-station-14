namespace Content.Shared.Magic;

// TODO: If still needed, move to magic component
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

public sealed partial class TargetInFrontSingle : MagicSpawnData
{
}
