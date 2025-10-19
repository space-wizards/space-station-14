namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// Base class for 
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseXAESystem<T> : EntitySystem where T : Component
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<T, XenoArtifactNodeActivatedEvent>(OnActivated);
    }

    /// <summary>
    /// Handler for node activation.
    /// </summary>
    /// <param name="ent">Entity (node) that got activated.</param>
    /// <param name="args">Activation event (containing artifact and other useful info).</param>
    protected abstract void OnActivated(Entity<T> ent, ref XenoArtifactNodeActivatedEvent args);
}
