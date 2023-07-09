using Content.Server.Worldgen.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Worldgen.Systems;

/// <summary>
///     This handles loading in objects based on distance from player, using some metadata on chunks.
/// </summary>
public sealed class LocalityLoaderSystem : BaseWorldSystem
{
    [Dependency] private readonly TransformSystem _xformSys = default!;

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var e = EntityQueryEnumerator<LocalityLoaderComponent, TransformComponent>();
        var loadedQuery = GetEntityQuery<LoadedChunkComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var controllerQuery = GetEntityQuery<WorldControllerComponent>();

        while (e.MoveNext(out var uid, out var loadable, out var xform))
        {
            if (!controllerQuery.TryGetComponent(xform.MapUid, out var controller))
            {
                RaiseLocalEvent(uid, new LocalStructureLoadedEvent());
                RemCompDeferred<LocalityLoaderComponent>(uid);
                continue;
            }

            var coords = GetChunkCoords(uid, xform);
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

                        if ((_xformSys.GetWorldPosition(loaderXform) - _xformSys.GetWorldPosition(xform)).Length() > loadable.LoadingDistance)
                            continue;

                        RaiseLocalEvent(uid, new LocalStructureLoadedEvent());
                        RemCompDeferred<LocalityLoaderComponent>(uid);
                        done = true;
                        break;
                    }
                }
            }
        }
    }
}

/// <summary>
///     A directed fired on a loadable entity when a local loader enters it's vicinity.
/// </summary>
public record struct LocalStructureLoadedEvent;

