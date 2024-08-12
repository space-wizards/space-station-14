using System.Numerics;
using Content.Client.SurveillanceCamera;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;

namespace Content.Client.StationAi;

public sealed class StationAiSystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IOverlayManager _overlayMgr = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    private StationAiOverlay? _overlay;

    public bool Enabled => _overlay != null;

    private HashSet<Entity<SurveillanceCameraVisualsComponent>> _entities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightingPassEvent>(OnLightingPass);
        _overlay = new StationAiOverlay();
        _overlayMgr.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMgr.RemoveOverlay<StationAiOverlay>();
    }

    private void OnLightingPass(ref LightingPassEvent ev)
    {
        _entities.Clear();

        if (_overlay == null || _overlay._blep == null)
            return;

        var pass = new LightingPass()
        {
            Target = _overlay._blep,
        };

        _lookup.GetEntitiesIntersecting(ev.MapId, ev.WorldAabb.Enlarged(10f), _entities);

        foreach (var ent in _entities)
        {
            var (worldPos, worldRot) = _xforms.GetWorldPositionRotation(ent);

            pass.Lights.Add(new RenderLight()
            {
                Color = Color.White,
                Radius = 10f,
                Energy = 1f,
                Position = worldPos,
                Rotation = worldRot,
                Softness = 1f,
            });
        }
        ev.Add(pass);
    }
}
