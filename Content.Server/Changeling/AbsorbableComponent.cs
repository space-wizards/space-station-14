namespace Content.Server.Changeling;

/// <summary>
/// Marks an entity as eligible to be extracted or absorbed by a changeling.
/// Its name, physical appearance, fingerprints and dna will be copied to a transformation.
/// </summary>
[RegisterComponent]
[Access(typeof(ChangelingSystem))]
public sealed class AbsorbableComponent : Component { }
