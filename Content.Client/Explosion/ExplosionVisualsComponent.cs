using Content.Shared.Explosion;
using Robust.Client.Graphics;

namespace Content.Client.Explosion;

[RegisterComponent]
[ComponentReference(typeof(SharedExplosionVisualsComponent))]
public sealed class ExplosionVisualsComponent : SharedExplosionVisualsComponent
{
    /// <summary>
    ///     Uid of the client-side point light entity for this explosion.
    /// </summary>
    public EntityUid LightEntity;

    /// <summary>
    ///     How intense an explosion needs to be at a given tile in order to progress to the next fire-intensity RSI state. See also <see cref="FireFrames"/>
    /// </summary>
    public float IntensityPerState;

    /// <summary>
    ///     The textures used for the explosion fire effect. Each fire-state is associated with an explosion
    ///     intensity range, and each stat itself has several textures.
    /// </summary>
    public List<Texture[]> FireFrames = new();

    public Color? FireColor;
}
