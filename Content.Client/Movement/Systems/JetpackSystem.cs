using Content.Client.Clothing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EffectSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, ComponentHandleState>(OnJetpackHandleState);
        SubscribeLocalEvent<JetpackComponent, AppearanceChangeEvent>(OnJetpackAppearance);
    }

    protected override bool CanEnable(JetpackComponent component)
    {
        // No predicted atmos so you'd have to do a lot of funny to get this working.
        return false;
    }

    private void OnJetpackAppearance(EntityUid uid, JetpackComponent component, ref AppearanceChangeEvent args)
    {
        args.Component.TryGetData(JetpackVisuals.Enabled, out bool enabled);

        var state = "icon" + (enabled ? "-on" : "");
        args.Sprite?.LayerSetState(0, state);

        if (TryComp<ClothingComponent>(uid, out var clothing))
            clothing.EquippedPrefix = enabled ? "on" : null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        foreach (var comp in EntityQuery<ActiveJetpackComponent>())
        {
            comp.Accumulator += frameTime;

            if (comp.Accumulator < comp.EffectCooldown) continue;
            comp.Accumulator -= comp.EffectCooldown;
            CreateParticles(comp.Owner);
        }
    }

    private void OnJetpackHandleState(EntityUid uid, JetpackComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not JetpackComponentState state) return;
        component.Enabled = state.Enabled;
    }

    private void CreateParticles(EntityUid uid)
    {
        var xform = Transform(uid);
        var coordinates = xform.Coordinates;
        var gridUid = coordinates.GetGridUid(EntityManager);

        if (_mapManager.TryGetGrid(gridUid, out var grid))
        {
            coordinates = new EntityCoordinates(grid.GridEntityId, grid.WorldToLocal(coordinates.ToMapPos(EntityManager)));
        }
        else if (xform.MapUid != null)
        {
            coordinates = new EntityCoordinates(xform.MapUid.Value, xform.WorldPosition);
        }
        else
        {
            return;
        }

        var startTime = _timing.CurTime;
        var deathTime = startTime + TimeSpan.FromSeconds(2);
        var effect = new EffectSystemMessage
        {
            EffectSprite = "Effects/atmospherics.rsi",
            Born = startTime,
            DeathTime = deathTime,
            Coordinates = coordinates,
            RsiState = "freon_old",
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 125), 1.0f),
            ColorDelta = Vector4.Multiply(new Vector4(0, 0, 0, -10), 1.0f),
            AnimationLoops = true,
            Shaded = false,
        };

        _effects.CreateEffect(effect);
    }
}
