using Content.Client.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Starlight.Overlay;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlay;

public sealed class CycloriteVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly FlashImmunitySystem _flashImmunity = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private CycloriteVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CycloriteVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<CycloriteVisionComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<CycloriteVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CycloriteVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new(_prototypeManager.Index<ShaderPrototype>("CycloriteShader"));
    }

    private void OnPlayerAttached(Entity<CycloriteVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AttemptAddVision(ent.Owner);
    }

    private void OnPlayerDetached(Entity<CycloriteVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        AttemptRemoveVision(ent.Owner, true);
    }

    private void OnVisionInit(Entity<CycloriteVisionComponent> ent, ref ComponentInit args)
    {
        AttemptAddVision(ent.Owner);
    }

    private void OnVisionShutdown(Entity<CycloriteVisionComponent> ent, ref ComponentShutdown args)
    {
        AttemptRemoveVision(ent.Owner);
    }

    private void AttemptAddVision(EntityUid uid)
    {
        //ENSURE this is the local player
        if (_player.LocalSession?.AttachedEntity != uid) return;

        //only add if its active
        if (!TryComp<CycloriteVisionComponent>(uid, out var cycloriteVision) || !cycloriteVision.Active) return;

        _overlayMan.AddOverlay(_overlay);
    }

    /// <summary>
    /// Attempt to remove the overlay from the local player.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="force">Use if you need to forcefully remove the overlay no matter what. Only should be used with events that ONLY the local player can fire, like attach/detach</param>
    private void AttemptRemoveVision(EntityUid uid, bool force = false)
    {
        //ENSURE this is the local player
        if (_player.LocalSession?.AttachedEntity != uid && !force) return;

        _overlayMan.RemoveOverlay(_overlay);
    }
}
