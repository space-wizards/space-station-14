using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;

    protected ISawmill Sawmill = default!;

    protected const float EmptyNextFire = 0.3f;
    protected const float InteractNextFire = 0.3f;

    public override void Initialize()
    {
        Sawmill = Logger.GetSawmill("gun");
        SubscribeLocalEvent<NewGunComponent, ComponentGetState>(OnGetState);
        SubscribeAllEvent<RequestShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<NewGunComponent, ComponentHandleState>(OnHandleState);

        // Interactions
        SubscribeLocalEvent<NewGunComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<NewGunComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<NewGunComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NewGunComponent, CycleModeEvent>(OnCycleMode);
    }
    private void OnShootRequest(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null) return;

        var gun = GetGun(user.Value);

        if (gun?.Owner != msg.Gun) return;

        gun.ShootCoordinates = msg.Coordinates;
        Sawmill.Debug($"Set shoot coordinates to {gun.ShootCoordinates}");
        AttemptShoot(user.Value, gun);
    }

    private void OnStopShootRequest(RequestStopShootEvent ev)
    {
        // TODO validate input
        StopShooting(Comp<NewGunComponent>(ev.Gun));
    }

    private void OnGetState(EntityUid uid, NewGunComponent component, ref ComponentGetState args)
    {
        args.State = new NewGunComponentState
        {
            NextFire = component.NextFire,
            ShotCounter = component.ShotCounter,
            FakeAmmo = component.FakeAmmo,
            SelectiveFire = component.SelectedMode,
            AvailableSelectiveFire = component.AvailableModes,
        };
    }

    private void OnHandleState(EntityUid uid, NewGunComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewGunComponentState state) return;

        Sawmill.Debug($"Handle state: setting shot count from {component.ShotCounter} to {state.ShotCounter}");
        component.NextFire = state.NextFire;
        component.ShotCounter = state.ShotCounter;
        component.FakeAmmo = state.FakeAmmo;
        component.SelectedMode = state.SelectiveFire;
        component.AvailableModes = state.AvailableSelectiveFire;
    }

    protected NewGunComponent? GetGun(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) ||
            hands.ActiveHandEntity is not { } held)
        {
            return null;
        }

        if (!EntityManager.TryGetComponent(held, out NewGunComponent? gun))
            return null;

        if (!_combatMode.IsInCombatMode(entity))
            return null;

        return gun;
    }

    protected void StopShooting(NewGunComponent gun)
    {
        if (gun.ShotCounter == 0) return;

        Sawmill.Debug($"Stopped shooting {ToPrettyString(gun.Owner)}");
        gun.ShotCounter = 0;
        gun.ShootCoordinates = null;
        Dirty(gun);
    }

    private void AttemptShoot(EntityUid user, NewGunComponent gun)
    {
        if (gun.FireRate <= 0f) return;

        if (gun.ShootCoordinates == null) return;

        var curTime = Timing.CurTime;

        // Need to do this to play the clicking sound for empty automatic weapons
        // but not play anything for burst fire.
        if (gun.NextFire > curTime) return;

        // First shot
        if (gun.ShotCounter == 0 && gun.NextFire < curTime)
            gun.NextFire = curTime;

        // Firing on empty. We won't spam the empty sounds at the firerate, just at a reduced rate.
        if (gun.SelectedMode == SelectiveFire.Safety || gun.FakeAmmo == 0)
        {
            PlaySound(gun, gun.SoundEmpty?.GetSound(), user);
            gun.NextFire += TimeSpan.FromSeconds(EmptyNextFire);
            Dirty(gun);
            return;
        }

        var shots = 0;
        var fireRate = TimeSpan.FromSeconds(1f / gun.FireRate);

        while (gun.NextFire <= curTime)
        {
            gun.NextFire += fireRate;
            shots++;
        }

        // Get how many shots we're actually allowed to make, due to clip size or otherwise.
        // Don't do this in the loop so we still reset NextFire.
        switch (gun.SelectedMode)
        {
            case SelectiveFire.SemiAuto:
                shots = Math.Min(shots, 1 - gun.ShotCounter);
                break;
            case SelectiveFire.Burst:
                shots = Math.Min(shots, 3 - gun.ShotCounter);
                break;
            case SelectiveFire.FullAuto:
                break;
            default:
                throw new ArgumentOutOfRangeException($"No implemented shooting behavior for {gun.SelectedMode}!");
        }

        // Remove ammo
        shots = Math.Min(shots, gun.FakeAmmo);
        gun.FakeAmmo -= shots;

        DebugTools.Assert(shots >= 0);

        if (shots <= 0) return;

        // Shoot confirmed
        gun.ShotCounter += shots;

        // Predicted sound moment
        PlaySound(gun, gun.SoundGunshot?.GetSound(), user);
        Dirty(gun);
    }

    protected abstract void PlaySound(NewGunComponent gun, string? sound, EntityUid? user = null);

    protected abstract void Popup(string message, NewGunComponent gun, EntityUid? user);

    /// <summary>
    /// Raised on the client to indicate it'd like to shoot.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestShootEvent : EntityEventArgs
    {
        public EntityUid Gun;
        public MapCoordinates Coordinates;
    }

    [Serializable, NetSerializable]
    public sealed class RequestStopShootEvent : EntityEventArgs
    {
        public EntityUid Gun;
    }

    [Serializable, NetSerializable]
    protected sealed class NewGunComponentState : ComponentState
    {
        public TimeSpan NextFire;
        public int ShotCounter;
        public int FakeAmmo;
        public SelectiveFire SelectiveFire;
        public SelectiveFire AvailableSelectiveFire;
    }
}
