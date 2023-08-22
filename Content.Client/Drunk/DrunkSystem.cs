using Content.Shared.Drunk;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.StatusEffect;
using Content.Shared.Bed.Sleep;

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

        //SubscribeLocalEvent<DrunkComponent, StatusEffectTimeAddedEvent>(OnDrunkUpdated);
        _overlay = new();
    }

    private void OnDrunkUpdated(EntityUid uid, DrunkComponent component, StatusEffectTimeAddedEvent args)
    {
        s = Logger.GetSawmill("up");
        s.Debug("1");
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            s.Debug("2");
            if (args.Key == DrunkKey)
            {
                s.Debug("3");
                var drunkOverlay = _overlayMan.GetOverlay<DrunkOverlay>();
                if (drunkOverlay.CurrentBoozePower > 10)
                {
                    s.Debug("4");
                    _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(5), false);
                }
            }
        }
    }

    private void OnPlayerAttached(EntityUid uid, DrunkComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, DrunkComponent component, PlayerDetachedEvent args)
    {
        _overlay.CurrentBoozePower = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnDrunkInit(EntityUid uid, DrunkComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnDrunkShutdown(EntityUid uid, DrunkComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlay.CurrentBoozePower = 0;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
