using Content.Client._Starlight.Overlay.Cyclorites;
using Content.Client.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlay.Thermal;

public sealed class ThermalVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    private ThermalVisionOverlay _overlay = default!;
    private EntityUid? _effect = null;
    private readonly EntProtoId _effectPrototype = "EffectThermalVision";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(Entity<ThermalVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (_effect == null)
            AddNightVision(ent.Owner);
        else if (HasComp<EyeProtectionComponent>(ent.Owner))
            RemoveNightVision();
    }

    private void OnPlayerDetached(Entity<ThermalVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveNightVision();
    }

    private void OnVisionInit(Entity<ThermalVisionComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity != ent.Owner) return;

        if (_effect == null)
            AddNightVision(ent.Owner);
        else if (HasComp<EyeProtectionComponent>(ent.Owner))
            RemoveNightVision();
    }

    private void OnVisionShutdown(Entity<ThermalVisionComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner) return;

        RemoveNightVision();
    }

    private void AddNightVision(EntityUid uid)
    {
        if (HasComp<EyeProtectionComponent>(uid)) return;

        _overlayMan.AddOverlay(_overlay);
        _effect = SpawnAttachedTo(_effectPrototype, Transform(uid).Coordinates);
        _xformSys.SetParent(_effect.Value, uid);
    }
    private void RemoveNightVision()
    {
        _overlayMan.RemoveOverlay(_overlay);
        Del(_effect);
        _effect = null;
    }
}
