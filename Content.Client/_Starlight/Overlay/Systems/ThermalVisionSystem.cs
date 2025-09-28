using Content.Client.Eye.Blinding;
using Content.Client.GameTicking.Managers;
using Content.Shared.Eye.Blinding.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Inventory.Events;
using Content.Shared.Flash.Components;
using Content.Shared.Starlight.Overlay;
using Content.Shared.Mech.Components;
using Content.Shared.Mech;

namespace Content.Client._Starlight.Overlay;

public sealed class ThermalVisionSystem : SharedThermalVisionSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly FlashImmunitySystem _flashImmunity = default!;


    private ThermalVisionEntityHighlightOverlay _throughWallsOverlay = default!;
    private ThermalVisionOverlay _overlay = default!;

    [ViewVariables]
    private EntityUid? _effect = null;
    protected override bool IsPredict() => !_timing.IsFirstTimePredicted;
    public override void Initialize()
    {
        base.Initialize();

        //handled in base class
        //SubscribeLocalEvent<ThermalVisionComponent, ComponentInit>(OnVisionInit);
        //SubscribeLocalEvent<ThermalVisionComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ThermalVisionComponent, FlashImmunityCheckEvent>(OnFlashImmunityChanged);

        _throughWallsOverlay = new(_prototypeManager.Index<ShaderPrototype>("BrightnessShader"));
        _overlay = new(_prototypeManager.Index<ShaderPrototype>("ThermalVisionScreenShader"));
    }

    private void OnFlashImmunityChanged(Entity<ThermalVisionComponent> ent, ref FlashImmunityCheckEvent args)
    {
        if (args.IsImmune)
        {
            AttemptRemoveVision(ent.Owner);
        }
        else
        {
            AttemptAddVision(ent.Owner);
        }
    }

    private void OnPlayerAttached(Entity<ThermalVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AttemptAddVision(ent.Owner);
    }

    private void OnPlayerDetached(Entity<ThermalVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        AttemptRemoveVision(ent.Owner, true);
    }

    protected override void ToggleOn(Entity<ThermalVisionComponent> ent)
    {
        AttemptAddVision(ent.Owner);
    }

    protected override void ToggleOff(Entity<ThermalVisionComponent> ent)
    {
        AttemptRemoveVision(ent.Owner);
    }

    private void AttemptAddVision(EntityUid uid)
    {
        if (_player.LocalSession?.AttachedEntity != uid) return;

        //if they currently have flash immunity, dont add
        if (_flashImmunity.HasFlashImmunityVisionBlockers(uid)) return;

        //only add if its active
        if (!TryComp<ThermalVisionComponent>(uid, out var thermalVision) || !thermalVision.Active) return;

        if (_effect != null) return;
        
        _overlayMan.AddOverlay(_throughWallsOverlay);
        _overlayMan.AddOverlay(_overlay);
        _effect = SpawnAttachedTo(thermalVision.EffectPrototype, Transform(uid).Coordinates);
        _xformSys.SetParent(_effect.Value, uid);
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

        _overlayMan.RemoveOverlay(_throughWallsOverlay);
        _overlayMan.RemoveOverlay(_overlay);
        Del(_effect);
        _effect = null;
    }
}
