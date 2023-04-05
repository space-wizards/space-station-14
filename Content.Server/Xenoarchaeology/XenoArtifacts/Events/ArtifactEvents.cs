namespace Content.Server.Xenoarchaeology.XenoArtifacts.Events;

/// <summary>
///     Invokes when artifact was successfully activated.
///     Used to start attached effects.
/// </summary>
public sealed class ArtifactActivatedEvent : EntityEventArgs
{
    /// <summary>
    ///     Entity that activate this artifact.
    ///     Usually player, but can also be another object.
    /// </summary>
    public EntityUid? Activator;
}

/// <summary>
///     Force to randomize artifact triggers.
/// </summary>
public sealed class ArtifactNodeEnteredEvent : EntityEventArgs
{
    /// <summary>
    /// An entity-specific seed that can be used to
    /// generate random values.
    /// </summary>
    public readonly int RandomSeed;

    public ArtifactNodeEnteredEvent(int randomSeed)
    {
        RandomSeed = randomSeed;
    }
}
