using Content.Client.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlay.Cyclorites;

public sealed class CycloritesVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    private CycloritesVisionOverlay _overlay = default!;
    private EntityUid? _effect = null;
    private readonly EntProtoId _effectPrototype = "EffectNightVision";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CycloritesVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<CycloritesVisionComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<CycloritesVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CycloritesVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<CycloritesVisionComponent, FlashImmunityChangedEvent>(OnFlashImmunityChanged);

        _overlay = new();
    }

    private void OnFlashImmunityChanged(Entity<CycloritesVisionComponent> ent, ref FlashImmunityChangedEvent args)
    {
        if (args.IsImmune)
        {
            ent.Comp.blockedByFlashImmunity = true;
            RemoveNightVision();
        }
        else
        {
            ent.Comp.blockedByFlashImmunity = false;
            AddNightVision(ent.Owner);
        }
    }

    private void OnPlayerAttached(Entity<CycloritesVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);

        if (!ent.Comp.blockedByFlashImmunity)
        {
            if (_effect == null)
                AddNightVision(ent.Owner);
        }
    }

    private void OnPlayerDetached(Entity<CycloritesVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        RemoveNightVision();
    }

    private void OnVisionInit(Entity<CycloritesVisionComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity != ent.Owner) return;

        _overlayMan.AddOverlay(_overlay);
        if (_effect == null)
            AddNightVision(ent.Owner);
    }

    private void OnVisionShutdown(Entity<CycloritesVisionComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner) return;

        _overlayMan.RemoveOverlay(_overlay);
        RemoveNightVision();
    }

    private void AddNightVision(EntityUid uid)
    {
        _effect = SpawnAttachedTo(_effectPrototype, Transform(uid).Coordinates);
        _xformSys.SetParent(_effect.Value, uid);
    }
    private void RemoveNightVision()
    {
        Del(_effect);
        _effect = null;
    }
}
