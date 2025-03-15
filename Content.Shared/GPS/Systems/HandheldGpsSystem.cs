using Content.Shared.GPS.Components;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGPSComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HandheldGPSComponent> ent, ref ExaminedEvent args)
    {
        var posText = "Error";
        var xform = _entMan.GetComponent<TransformComponent>(ent);

        if (xform.GridUid is { } gridUid)
        {
            var pos = _transform.GetMapCoordinates(ent, xform);
            var x = (int)pos.X;
            var y = (int)pos.Y;

            posText = $"({x}, {y})";
        }

        args.PushMarkup(Loc.GetString("handheld-gps-coordinates-title", ("coordinates", posText)));
    }
}
