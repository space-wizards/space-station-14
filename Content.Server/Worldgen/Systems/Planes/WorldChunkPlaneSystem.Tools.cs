namespace Content.Server.Worldgen.Systems.Planes;

public abstract partial class WorldChunkPlaneSystem<TChunk, TConfig>
{
    [Dependency] private readonly PoissonDiskSampler _sampler = default!;

    /// <summary>
    /// Generates random points of the given density.
    /// </summary>
    /// <param name="pointDensity">
    /// The density, some number less than 1.
    /// Easy way to think of this is as `1/r`, where r is the radius of the objects you're trying to distribute in world-space.
    /// You may have to get a bit clever with this value for significant warping, as the underlying algorithm doesn't work within the distorted space.
    /// </param>
    /// <returns>Random world-coordinate points within the chunk.</returns>
    protected List<Vector2> GeneratePoissonDiskPointsInChunk(float pointDensity, Vector2i chunk)
    {
        var offs = (float)((1.0 - pointDensity / 2) / 2); // Edge avoidance, so we don't clip into other chunks.
        var topLeft = (-offs, -offs);
        var lowerRight = (offs, offs);
        var debrisPoints = _sampler.SampleRectangle(topLeft, lowerRight, pointDensity);
        for (var i = 0; i < debrisPoints.Count; i++)
        {
            var point = debrisPoints[i];
            debrisPoints[i] = ChunkSpaceToWorld(point + chunk + new Vector2(0.5f, 0.5f));
        }

        return debrisPoints;
    }
}
