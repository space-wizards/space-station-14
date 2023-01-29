using Content.Shared.Explosion;
using Robust.Client.Graphics;

namespace Content.Client.Explosion;

[RegisterComponent]
[ComponentReference(typeof(SharedExplosionVisualsComponent))]
public sealed class ExplosionVisualsComponent : SharedExplosionVisualsComponent
{
    public EntityUid LightEntity;
    /// <summary>
    ///     How long have we been drawing this explosion, starting from the time the explosion was fully drawn.
    /// </summary>
    public float Lifetime;

    public float IntensityPerState;

    /// <summary>
    ///     The textures used for the explosion fire effect. Each fire-state is associated with an explosion
    ///     intensity range, and each stat itself has several textures.
    /// </summary>
    public List<Texture[]> FireFrames = new();

    public Color? FireColor;
}
