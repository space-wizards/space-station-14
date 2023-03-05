using Content.Server._Citadel.Worldgen.Components;

namespace Content.Server._Citadel.Worldgen.Systems;

/// <summary>
///     This handles loading in objects based on distance from player, using some metadata on chunks.
/// </summary>
public sealed class LocalityLoaderSystem : BaseWorldSystem
{
    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var e = EntityQueryEnumerator<LocalityLoaderComponent, TransformComponent>();
        var loadedQuery = GetEntityQuery<LoadedChunkComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var controllerQuery = GetEntityQuery<WorldControllerComponent>();

        while (e.MoveNext(out var loadable, out var xform))
        {
            if (!controllerQuery.TryGetComponent(xform.MapUid, out var controller))
                return;

            var coords = GetChunkCoords(xform.Owner, xform);
            var done = false;
            for (var i = -1; i < 2 && !done; i++)
            {
                for (var j = -1; j < 2 && !done; j++)
                {
                    var chunk = GetOrCreateChunk(coords + (i, j), xform.MapUid!.Value, controller);
                    if (!loadedQuery.TryGetComponent(chunk, out var loaded) || loaded.Loaders is null)
                        continue;

                    foreach (var loader in loaded.Loaders)
                    {
                        if (!xformQuery.TryGetComponent(loader, out var loaderXform))
                            continue;

                        if ((loaderXform.WorldPosition - xform.WorldPosition).Length > loadable.LoadingDistance)
                            continue;

                        RaiseLocalEvent(loadable.Owner, new LocalStructureLoadedEvent());
                        RemCompDeferred<LocalityLoaderComponent>(loadable.Owner);
                        done = true;
                        break;
                    }
                }
            }
        }
    }
}

public record struct LocalStructureLoadedEvent;

