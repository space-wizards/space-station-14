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


        //SubscribeLocalEvent<DrunkComponent, StatusEffectTimeAddedEvent>(OnDrunkUpdated);
        _overlay = new();
    }

    public override void UpdateOverlay(float currentBoozePower)
    {
        var ov = _overlayMan.GetOverlay<DrunkOverlay>();
        ov.CurrentBoozePower = currentBoozePower;
    }
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
