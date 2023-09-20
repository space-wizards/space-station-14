using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly float ApproxEqual = 0.01f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ActivateInWorldEvent>(OnActivate);

        SubscribeLocalEvent<GeigerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<GeigerComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<GeigerComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<GeigerComponent, GotUnequippedHandEvent>(OnUnequippedHand);

        SubscribeLocalEvent<RadiationSystemUpdatedEvent>(OnUpdate);
        SubscribeLocalEvent<GeigerComponent, ComponentGetState>(OnGetState);
    }

    private void OnActivate(EntityUid uid, GeigerComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || component.AttachedToSuit)
            return;
        args.Handled = true;

        SetEnabled(uid, component, !component.IsEnabled);
    }

    private void OnEquipped(EntityUid uid, GeigerComponent component, GotEquippedEvent args)
    {
        if (component.AttachedToSuit)
            SetEnabled(uid, component, true);
        SetUser(component, args.Equipee);
    }

    private void OnEquippedHand(EntityUid uid, GeigerComponent component, GotEquippedHandEvent args)
    {
        if (component.AttachedToSuit)
            return;

        SetUser(component, args.User);
    }

    private void OnUnequipped(EntityUid uid, GeigerComponent component, GotUnequippedEvent args)
    {
        if (component.AttachedToSuit)
            SetEnabled(uid, component, false);
        SetUser(component, null);
    }

    private void OnUnequippedHand(EntityUid uid, GeigerComponent component, GotUnequippedHandEvent args)
    {
        if (component.AttachedToSuit)
            return;

        SetUser(component, null);
    }

    private void OnUpdate(RadiationSystemUpdatedEvent ev)
    {
        // update only active geiger counters
        // deactivated shouldn't have rad receiver component
        var query = EntityQuery<GeigerComponent, RadiationReceiverComponent>();
        foreach (var (geiger, receiver) in query)
        {
            var rads = receiver.CurrentRadiation;
            SetCurrentRadiation(geiger.Owner, geiger, rads);
        }
    }

    private void OnGetState(EntityUid uid, GeigerComponent component, ref ComponentGetState args)
    {
        args.State = new GeigerComponentState
        {
            CurrentRadiation = component.CurrentRadiation,
            DangerLevel = component.DangerLevel,
            IsEnabled = component.IsEnabled,
            User = GetNetEntity(component.User)
        };
    }

    private void SetCurrentRadiation(EntityUid uid, GeigerComponent component, float rads)
    {
        // check that it's approx equal
        if (MathHelper.CloseTo(component.CurrentRadiation, rads, ApproxEqual))
            return;

        var curLevel = component.DangerLevel;
        var newLevel = RadsToLevel(rads);

        component.CurrentRadiation = rads;
        component.DangerLevel = newLevel;

        if (curLevel != newLevel)
        {
            UpdateAppearance(uid, component);
            UpdateSound(uid, component);
        }

        Dirty(component);
    }

    private void SetUser(GeigerComponent component, EntityUid? user)
    {
        if (component.User == user)
            return;

        component.User = user;
        Dirty(component);
        UpdateSound(component.Owner, component);
    }

    private void SetEnabled(EntityUid uid, GeigerComponent component, bool isEnabled)
    {
        if (component.IsEnabled == isEnabled)
            return;

        component.IsEnabled = isEnabled;
        if (!isEnabled)
        {
            component.CurrentRadiation = 0f;
            component.DangerLevel = GeigerDangerLevel.None;
        }

        _radiation.SetCanReceive(uid, isEnabled);

        UpdateAppearance(uid, component);
        UpdateSound(uid, component);
        Dirty(component);
    }

    private void UpdateAppearance(EntityUid uid, GeigerComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, GeigerVisuals.IsEnabled, component.IsEnabled, appearance);
        _appearance.SetData(uid, GeigerVisuals.DangerLevel, component.DangerLevel, appearance);
    }

    private void UpdateSound(EntityUid uid, GeigerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.Stream?.Stop();

        if (!component.Sounds.TryGetValue(component.DangerLevel, out var sounds))
            return;

        if (component.User == null)
            return;

        if (!_player.TryGetSessionByEntity(component.User.Value, out var session))
            return;

        var sound = _audio.GetSound(sounds);
        var param = sounds.Params.WithLoop(true).WithVolume(-4f);

        component.Stream = _audio.PlayGlobal(sound, session, param);
    }

    public static GeigerDangerLevel RadsToLevel(float rads)
    {
        return rads switch
        {
            < 0.2f => GeigerDangerLevel.None,
            < 1f => GeigerDangerLevel.Low,
            < 3f => GeigerDangerLevel.Med,
            < 6f => GeigerDangerLevel.High,
            _ => GeigerDangerLevel.Extreme
        };
    }
}
