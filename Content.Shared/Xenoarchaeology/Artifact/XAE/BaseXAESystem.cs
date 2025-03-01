namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public abstract class BaseXAESystem<T> : EntitySystem where T : Component
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<T, XenoArtifactNodeActivatedEvent>(OnActivated);
    }

    protected abstract void OnActivated(Entity<T> ent, ref XenoArtifactNodeActivatedEvent args);
}
