using Content.Shared.GPS.Components;
using Content.Shared.Examine;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using System.Reflection.Emit;

namespace Content.Shared.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<Entity<HandheldGPSComponent>> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGPSComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HandheldGPSComponent, ComponentStartup>(AddComponent);
    }

    private void UpdateCoords(Entity<HandheldGPSComponent> ent)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();

        var posText = "Unknown";
        if (entManager.TryGetComponent(ent, out TransformComponent? transComp))
        {
            var pos = _transform.GetMapCoordinates(ent, transComp);
            var x = (int)pos.X;
            var y = (int)pos.Y;
            posText = $"({x}, {y})";
        }

        var posLoc = Loc.GetString("handheld-gps-coordinates-title", ("coordinates", posText));

        ent.Comp.StoredCoords = posLoc;
    }

    private void AddComponent(Entity<HandheldGPSComponent> gps, ref ComponentStartup args)
    {
        _components.Add(gps);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;

        for (var i = _components.Count - 1; i >= 0; i--)
        {
            var gps = _components[i];

            if (gps.Comp.Deleted)
            {
                _components.RemoveAt(i);
                continue;
            }

            if (currentTime >= gps.Comp.NextCoordUpdate)
            {
                UpdateCoords(gps);

                gps.Comp.NextCoordUpdate = currentTime + TimeSpan.FromSeconds(gps.Comp.UpdateRate);
            }
        }
    }

    private void OnExamine(Entity<HandheldGPSComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.StoredCoords);
    }
}
