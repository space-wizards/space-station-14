using Content.Shared.Drunk;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.StatusEffect;

namespace Content.Client.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKey = "ForcedSleep";
    private DrunkOverlay _overlay = default!;
    ISawmill s = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrunkComponent, ComponentInit>(OnDrunkInit);
        SubscribeLocalEvent<DrunkComponent, ComponentShutdown>(OnDrunkShutdown);

        SubscribeLocalEvent<DrunkComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DrunkComponent, PlayerDetachedEvent>(OnPlayerDetached);


        //SubscribeLocalEvent<StatusEffectsComponent, StatusEffectTimeAddedEvent>(OnDrunkUpdated);
        _overlay = new();
    }

    //public void OnDrunkUpdated(EntityUid uid, StatusEffectsComponent component, StatusEffectTimeAddedEvent args)
    //{
    //    s = Logger.GetSawmill("client");
    //    s.Debug("1");
    //    DrunkOverlay ov = _overlayMan.GetOverlay<DrunkOverlay>();
    //    s.Debug("1");
    //    if (!_statusEffectsSystem.TryGetTime(uid, DrunkKey, out var time))
    //        return;
    //    float timeLeft = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
    //    ov.CurrentBoozePower = timeLeft;
    //    s.Debug(ov.CurrentBoozePower.ToString());
        
    //}
    private void OnPlayerAttached(EntityUid uid, DrunkComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, DrunkComponent component, PlayerDetachedEvent args)
    {
        if (!TryComp<DrunkComponent>(uid, out var drunkComp))
            return;
        drunkComp.CurrentBoozePower = 0f;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnDrunkInit(EntityUid uid, DrunkComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnDrunkShutdown(EntityUid uid, DrunkComponent component, ComponentShutdown args)
    {
        if (!TryComp<DrunkComponent>(uid, out var drunkComp))
            return;
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {            
            drunkComp.CurrentBoozePower = 0f;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
