using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Kitchen;
using Content.Shared.Maps;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Server.Nuke;

/// <inheritdoc />
public sealed partial class NukeSystem : SharedNukeSystem
{
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private ExplosionSystem _explosions = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private PopupSystem _popups = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    ///     Used to calculate when the nuke song should start playing for maximum kino with the nuke sfx
    /// </summary>
    private TimeSpan _nukeSongLength;
    private ResolvedSoundSpecifier _selectedNukeSong = String.Empty;

    /// <summary>
    ///     Time to leave between the nuke song and the nuke alarm playing.
    /// </summary>
    private static readonly TimeSpan NukeSongBuffer = TimeSpan.FromSeconds(1.5);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NukeComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<NukeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NukeComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<NukeComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);

        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<NukeComponent, AnchorStateChangedEvent>(OnAnchorChanged);

        // ui events
        SubscribeLocalEvent<NukeComponent, NukeAnchorMessage>(OnAnchorButtonPressed);
        SubscribeLocalEvent<NukeComponent, NukeArmedMessage>(OnArmButtonPressed);
        SubscribeLocalEvent<NukeComponent, NukeKeypadMessage>(OnKeypadButtonPressed);
        SubscribeLocalEvent<NukeComponent, NukeKeypadClearMessage>(OnClearButtonPressed);
        SubscribeLocalEvent<NukeComponent, NukeKeypadEnterMessage>(OnEnterButtonPressed);

        // Doafter events
        SubscribeLocalEvent<NukeComponent, NukeDisarmDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<NukeDiskComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnInit(EntityUid uid, NukeComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, NukeComponent.NukeDiskSlotId, component.DiskSlot);

        UpdateStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnMapInit(Entity<NukeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ArmingTime = ent.Comp.Timer;

        var originStation = _station.GetOwningStation(ent);

        if (originStation != null)
        {
            ent.Comp.OriginStation = originStation;
        }
        else
        {
            var transform = Transform(ent);
            ent.Comp.OriginMapGrid = (transform.MapID, transform.GridUid);
        }

        ent.Comp.Code = GenerateRandomNumberString(ent.Comp.CodeLength);
    }

    /// <summary>
    /// Slightly randomize nuke countdown timer
    /// </summary>
    private void OnMicrowaved(Entity<NukeDiskComponent> ent, ref BeingMicrowavedEvent args)
    {
        if (ent.Comp.TimeModifier != null)
            return;

        var seconds = _random.NextGaussian(ent.Comp.MicrowaveMean.TotalSeconds, ent.Comp.MicrowaveStd.TotalSeconds);
        ent.Comp.TimeModifier = TimeSpan.FromSeconds(seconds);
        _popups.PopupEntity(Loc.GetString("nuke-disk-component-microwave"), ent.Owner, PopupType.Medium);
    }

    private void OnRemove(EntityUid uid, NukeComponent component, ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(uid, component.DiskSlot);
    }

    private void OnItemSlotChanged(EntityUid uid, NukeComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.DiskSlot.ID)
            return;

        UpdateStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    #region Anchor

    private void OnAnchorChanged(EntityUid uid, NukeComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateUserInterface(uid, component);

        if (!args.Anchored
            && component.Status == NukeStatus.ARMED
            && component.ExplosionTime != null
            && component.ExplosionTime.Value.TotalSeconds > component.DisarmDoAfterLength)
        {
            // yes, this means technically if you can find a way to unanchor the nuke, you can disarm it
            // without the doafter. but that takes some effort, and it won't allow you to disarm a nuke that can't be disarmed by the doafter.
            DisarmBomb((uid, component));
        }

        UpdateAppearance(uid, component);
    }

    #endregion

    #region UI Events

    private async void OnAnchorButtonPressed(EntityUid uid, NukeComponent component, NukeAnchorMessage args)
    {
        // malicious client sanity check
        if (component.Status == NukeStatus.ARMED)
            return;

        // Nuke has to have the disk in it to be moved
        if (!component.DiskSlot.HasItem)
        {
            var msg = Loc.GetString("nuke-component-cant-anchor-toggle");
            _popups.PopupEntity(msg, uid, args.Actor, PopupType.MediumCaution);
            return;
        }

        // manually set transform anchor (bypassing anchorable)
        // todo: it will break pullable system
        var xform = Transform(uid);
        if (xform.Anchored)
        {
            _transform.Unanchor(uid, xform);
            _itemSlots.SetLock(uid, component.DiskSlot, true);
            _adminLog.Add(LogType.Anchor, LogImpact.High, $"{args.Actor} unanchored {uid}");
        }
        else
        {
            if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
                return;

            var worldPos = _transform.GetWorldPosition(xform);

            foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, component.RequiredFloorRadius), false))
            {
                if (!_turf.IsSpace(tile))
                    continue;

                var msg = Loc.GetString("nuke-component-cant-anchor-floor");
                _popups.PopupEntity(msg, uid, args.Actor, PopupType.MediumCaution);

                return;
            }

            _transform.SetCoordinates(uid, xform, xform.Coordinates.SnapToGrid());
            _transform.AnchorEntity(uid, xform);
            _itemSlots.SetLock(uid, component.DiskSlot, false);
            _adminLog.Add(LogType.Anchor, LogImpact.High, $"{args.Actor} anchored {uid}");
        }

        UpdateUserInterface(uid, component);
    }

    private void OnEnterButtonPressed(EntityUid uid, NukeComponent component, NukeKeypadEnterMessage args)
    {
        if (component.Status != NukeStatus.AWAIT_CODE)
            return;

        var curTime = _timing.CurTime;
        if (curTime < component.LastCodeEnteredAt + NukeComponent.EnterCodeCooldown)
            return; // Validate that they are not entering codes faster than the cooldown.

        component.LastCodeEnteredAt = curTime;

        UpdateStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnKeypadButtonPressed(EntityUid uid, NukeComponent component, NukeKeypadMessage args)
    {
        PlayNukeKeypadSound(uid, args.Value, component);

        if (component.Status != NukeStatus.AWAIT_CODE)
            return;

        if (component.EnteredCode.Length >= component.CodeLength)
            return;

        component.EnteredCode += args.Value.ToString();
        UpdateUserInterface(uid, component);
    }

    private void OnClearButtonPressed(EntityUid uid, NukeComponent component, NukeKeypadClearMessage args)
    {
        _audio.PlayPvs(component.KeypadPressSound, uid);

        if (component.Status != NukeStatus.AWAIT_CODE)
            return;

        component.EnteredCode = "";
        UpdateUserInterface(uid, component);
    }

    private void OnArmButtonPressed(EntityUid uid, NukeComponent component, NukeArmedMessage args)
    {
        if (!component.DiskSlot.HasItem)
            return;

        if (component.Status == NukeStatus.AWAIT_ARM && Transform(uid).Anchored)
            ArmBomb((uid, component), args.Actor);

        else
        {
            _adminLog.Add(LogType.Explosion, LogImpact.High, $"{args.Actor} is attempting to disarm {uid}");
            DisarmBombDoAfter(uid, args.Actor, component);
        }
    }

    #endregion

    #region Doafter Events

    private void OnDoAfter(EntityUid uid, NukeComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        DisarmBomb((uid, component), args.User);

        var ev = new NukeDisarmSuccessEvent();
        RaiseLocalEvent(ev);

        args.Handled = true;
    }
    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NukeComponent>();
        while (query.MoveNext(out var uid, out var nuke))
        {
            switch (nuke.Status)
            {
                case NukeStatus.ARMED:
                    TickTimer((uid, nuke));
                    break;
                case NukeStatus.COOLDOWN:
                    TickCooldown((uid, nuke));
                    break;
            }
        }
    }

    private void TickCooldown(Entity<NukeComponent> ent)
    {
        if (ent.Comp.CooldownTime == null)
            return;

        if (ent.Comp.CooldownTime <= _timing.CurTime)
        {
            // reset nuke to default state
            ent.Comp.CooldownTime = null;
            ent.Comp.Status = NukeStatus.AWAIT_ARM;
            UpdateStatus(ent.Owner, ent.Comp);
        }

        UpdateUserInterface(ent.Owner, ent.Comp);
    }

    private void TickTimer(Entity<NukeComponent> ent)
    {
        if (ent.Comp.ExplosionTime == null)
            return;

        var remainingTime = ent.Comp.ExplosionTime.Value - _timing.CurTime;

        // Start playing the nuke event song so that it ends a couple seconds before the alert sound
        // should play
        if (remainingTime <= _nukeSongLength + ent.Comp.AlertSoundTime + NukeSongBuffer && !ent.Comp.PlayedNukeSong && !ResolvedSoundSpecifier.IsNullOrEmpty(_selectedNukeSong))
        {
            _sound.DispatchStationEventMusic(ent, _selectedNukeSong, StationEventMusicType.Nuke);
            ent.Comp.PlayedNukeSong = true;
        }

        // play alert sound if time is running out
        if (remainingTime <= ent.Comp.AlertSoundTime && !ent.Comp.PlayedAlertSound)
        {
            _sound.PlayGlobalOnStation(ent, _audio.ResolveSound(ent.Comp.AlertSound), new AudioParams{Volume = -5f});
            _sound.StopStationEventMusic(ent, StationEventMusicType.Nuke);
            ent.Comp.PlayedAlertSound = true;
            UpdateAppearance(ent.Owner, ent.Comp);
        }

        if (remainingTime.TotalSeconds <= 0)
        {
            ent.Comp.ExplosionTime = null;
            ActivateBomb(ent.Owner, ent.Comp);
        }

        else
            UpdateUserInterface(ent.Owner, ent.Comp);
    }

    private void UpdateStatus(EntityUid uid, NukeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        switch (component.Status)
        {
            case NukeStatus.AWAIT_DISK:
                if (component.DiskSlot.HasItem)
                    component.Status = NukeStatus.AWAIT_CODE;
                break;
            case NukeStatus.AWAIT_CODE:
                if (!component.DiskSlot.HasItem)
                {
                    component.Status = NukeStatus.AWAIT_DISK;
                    component.EnteredCode = "";
                    break;
                }

                if (component.EnteredCode == component.Code)
                {
                    component.Status = NukeStatus.AWAIT_ARM;
                    _audio.PlayPvs(component.AccessGrantedSound, uid);
                    _adminLog.Add(LogType.Action, LogImpact.Extreme, $"Nuke code entered correctly on {uid}");
                }
                else
                {
                    component.EnteredCode = "";
                    _audio.PlayPvs(component.AccessDeniedSound, uid);
                    _adminLog.Add(LogType.Action, LogImpact.High, $"Nuke code entered incorrectly on {uid}");
                }

                break;
            case NukeStatus.AWAIT_ARM:
                // do nothing, wait for arm button to be pressed
                break;
            case NukeStatus.ARMED:
                // handling case of wizard recalling disk out of armed Nuke
                if (!component.DiskSlot.HasItem)
                {
                    DisarmBomb((uid, component));
                }
                break;
        }
    }

    private void UpdateUserInterface(EntityUid uid, NukeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_ui.HasUi(uid, NukeUiKey.Key))
            return;

        var anchored = Transform(uid).Anchored;

        var allowArm = component.DiskSlot.HasItem &&
                       (component.Status == NukeStatus.AWAIT_ARM ||
                        component.Status == NukeStatus.ARMED);

        var state = new NukeUiState
        {
            Status = component.Status,
            RemainingTime = component.ExplosionTime - _timing.CurTime,
            DiskInserted = component.DiskSlot.HasItem,
            IsAnchored = anchored,
            AllowArm = allowArm,
            EnteredCodeLength = component.EnteredCode.Length,
            MaxCodeLength = component.CodeLength,
            CooldownTime = component.CooldownTime - _timing.CurTime,
            ArmingTime = component.ArmingTime,
        };

        _ui.SetUiState(uid, NukeUiKey.Key, state);
    }

    private void PlayNukeKeypadSound(EntityUid uid, int number, NukeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // This is a C mixolydian blues scale.
        // 1 2 3    C D Eb
        // 4 5 6    E F F#
        // 7 8 9    G A Bb
        var semitoneShift = number switch
        {
            1 => 0,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 5,
            6 => 6,
            7 => 7,
            8 => 9,
            9 => 10,
            0 => component.LastPlayedKeypadSemitones + 12,
            _ => 0,
        };

        // Don't double-dip on the octave shifting
        component.LastPlayedKeypadSemitones = number == 0 ? component.LastPlayedKeypadSemitones : semitoneShift;

        var opts = component.KeypadPressSound.Params;
        opts = AudioHelpers.ShiftSemitone(opts, semitoneShift).AddVolume(-5f);
        _audio.PlayPvs(component.KeypadPressSound, uid, opts);
    }

    public string GenerateRandomNumberString(int length)
    {
        var ret = "";
        for (var i = 0; i < length; i++)
        {
            var c = (char) _random.Next('0', '9' + 1);
            ret += c;
        }

        return ret;
    }

    #region Public API

    /// <summary>
    ///     Force a nuclear bomb to start a countdown timer
    /// </summary>
    [PublicAPI, Obsolete("Use Entity<T> version instead.")]
    public void ArmBomb(EntityUid uid, NukeComponent? component = null)
    {
        ArmBomb((uid, component));
    }

    /// <summary>
    /// Begins the countdown timer for a nuclear bomb.
    /// </summary>
    /// <param name="nuke">Thing the goes boom.</param>
    /// <param name="user">Entity who started the boom.</param>
    [PublicAPI]
    public void ArmBomb(Entity<NukeComponent?> nuke, EntityUid? user = null)
    {
        if (!Resolve(nuke.Owner, ref nuke.Comp))
            return;

        if (nuke.Comp.Status == NukeStatus.ARMED)
            return;

        var nukeXform = Transform(nuke);
        var stationUid = _station.GetStationInMap(nukeXform.MapID);
        // The nuke may not be on a station, so it's more important to just
        // let people know that a nuclear bomb was armed in their vicinity instead.
        // Otherwise, you could set every station to whatever AlertLevelOnActivate is.
        if (stationUid != null)
            _alertLevel.SetLevel(stationUid.Value, nuke.Comp.AlertLevelOnActivate, true, true, true, true);

        // We are collapsing the randomness here, otherwise we would get separate random song picks for checking duration and when actually playing the song afterwards
        _selectedNukeSong = _audio.ResolveSound(nuke.Comp.ArmMusic);

        // warn a crew
        var announcement = Loc.GetString("nuke-component-announcement-armed",
            ("time", nuke.Comp.ArmingTime.TotalSeconds),
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((nuke, nukeXform)))));
        var sender = Loc.GetString("nuke-component-announcement-sender");
        _chatSystem.DispatchStationAnnouncement(stationUid ?? nuke, announcement, sender, false, null, Color.Red);

        _sound.PlayGlobalOnStation(nuke, _audio.ResolveSound(nuke.Comp.ArmSound));
        _nukeSongLength = _audio.GetAudioLength(_selectedNukeSong);

        // turn on the spinny light
        _pointLight.SetEnabled(nuke, true);
        // enable the navmap beacon for people to find it
        _navMap.SetBeaconEnabled(nuke, true);

        _itemSlots.SetLock(nuke, nuke.Comp.DiskSlot, true);
        if (!nukeXform.Anchored)
        {
            // Admin command shenanigans, just make sure.
            _transform.AnchorEntity(nuke, nukeXform);
        }

        // Set the fuse
        var modifier = CompOrNull<NukeDiskComponent>(nuke.Comp.DiskSlot.Item)?.TimeModifier ?? TimeSpan.Zero;
        var secondsTillBoom = Math.Max(nuke.Comp.ArmingTime.TotalSeconds + modifier.TotalSeconds, nuke.Comp.MinimumTime.TotalSeconds);

        nuke.Comp.ExplosionTime = _timing.CurTime + TimeSpan.FromSeconds(secondsTillBoom);
        DirtyField(nuke, "ExplosionTime");

        var pos = _transform.GetMapCoordinates(nuke, xform: nukeXform);

        _adminLog.Add(
            LogType.Explosion,
            LogImpact.Extreme,
            $"{nuke} has been armed with a {(int) nuke.Comp.ArmingTime.TotalSeconds} second timer! " +
                   $"Preformed by {user} at position: {pos}.");

        nuke.Comp.Status = NukeStatus.ARMED;
        UpdateUserInterface(nuke, nuke.Comp);
        UpdateAppearance(nuke, nuke.Comp);
    }

    /// <summary>
    ///     Stop nuclear bomb timer
    /// </summary>
    [PublicAPI, Obsolete("Use Entity<T> version instead.")]
    public void DisarmBomb(EntityUid uid, NukeComponent? component = null)
    {
        DisarmBomb((uid, component));
    }

    /// <summary>
    /// Disables an active nuke.
    /// </summary>
    /// <param name="nuke">Thing that isn't going boom any longer.</param>
    /// <param name="user">Hero of the station.</param>
    [PublicAPI]
    public void DisarmBomb(Entity<NukeComponent?> nuke, EntityUid? user = null)
    {
        if (!Resolve(nuke, ref nuke.Comp))
            return;

        if (nuke.Comp.Status != NukeStatus.ARMED)
            return;

        TimeSpan remainingTime;
        if (nuke.Comp.ExplosionTime == null)
        {
            Log.Error($"A nuke was disarmed without having had its timer set when armed! Entity: {ToPrettyString(nuke)}");
            remainingTime = nuke.Comp.Timer;
        }
        else
            remainingTime = nuke.Comp.ExplosionTime.Value - _timing.CurTime;

        nuke.Comp.ExplosionTime = null;
        DirtyField(nuke, "ExplosionTime");

        // reset nuke remaining time to either itself or the minimum time, whichever is higher
        nuke.Comp.ArmingTime = remainingTime < nuke.Comp.MinimumTime
                             ? nuke.Comp.MinimumTime
                             : remainingTime;

        var stationUid = _station.GetOwningStation(nuke);
        if (stationUid != null)
            _alertLevel.SetLevel(stationUid.Value, nuke.Comp.AlertLevelOnDeactivate, true, true, true);

        // warn a crew
        var announcement = Loc.GetString("nuke-component-announcement-unarmed");
        var sender = Loc.GetString("nuke-component-announcement-sender");
        _chatSystem.DispatchStationAnnouncement(nuke, announcement, sender, false);

        nuke.Comp.PlayedNukeSong = false;
        _sound.PlayGlobalOnStation(nuke, _audio.ResolveSound(nuke.Comp.DisarmSound));
        _sound.StopStationEventMusic(nuke, StationEventMusicType.Nuke);

        // disable sound and reset it
        if (nuke.Comp.PlayedAlertSound)
        {
            nuke.Comp.PlayedAlertSound = false;
            nuke.Comp.AlertAudioStream = _audio.Stop(nuke.Comp.AlertAudioStream); // Does this ever even get set?
        }

        // turn off the spinny light
        _pointLight.SetEnabled(nuke, false);
        // disable the navmap beacon now that its disarmed
        _navMap.SetBeaconEnabled(nuke, false);

        // start bomb cooldown
        _itemSlots.SetLock(nuke, nuke.Comp.DiskSlot, false);
        nuke.Comp.Status = NukeStatus.COOLDOWN;
        nuke.Comp.CooldownTime = _timing.CurTime + nuke.Comp.Cooldown;

        _adminLog.Add(
            LogType.Explosion,
            LogImpact.Extreme,
            $"Nuke {nuke} was disarmed by {user} with {(int)remainingTime.TotalSeconds} seconds remaining!");

        UpdateUserInterface(nuke.Owner, nuke.Comp);
        UpdateAppearance(nuke.Owner, nuke.Comp);
    }

    /// <summary>
    ///     Toggle bomb arm button
    /// </summary>
    public void ToggleBomb(EntityUid uid, NukeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Status == NukeStatus.ARMED)
            DisarmBomb((uid, component));
        else
            ArmBomb((uid, component));
    }

    /// <summary>
    ///     Force bomb to explode immediately
    /// </summary>
    public void ActivateBomb(EntityUid uid, NukeComponent? component = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref component, ref transform))
            return;

        if (component.Exploded)
            return;

        component.Exploded = true;

        _adminLog.Add(LogType.Explosion, LogImpact.Extreme, $"{uid} detonated.");

        _explosions.QueueExplosion(uid,
            component.ExplosionType,
            component.TotalIntensity,
            component.IntensitySlope,
            component.MaxIntensity);

        RaiseLocalEvent(new NukeExplodedEvent()
        {
            OwningStation = transform.GridUid,
        });

        _sound.StopStationEventMusic(uid, StationEventMusicType.Nuke);
        Del(uid);
    }

    /// <summary>
    /// Set remaining time value.
    /// </summary>
    /// <param name="uid">The nuke.</param>
    /// <param name="timer">Seconds until the nuke explodes!!</param>
    /// <param name="component">The nuke component.</param>
    public void SetRemainingTime(EntityUid uid, float timer, NukeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.ExplosionTime = _timing.CurTime + TimeSpan.FromSeconds(timer);
        UpdateUserInterface(uid, component);
    }

    #endregion

    private void DisarmBombDoAfter(EntityUid uid, EntityUid user, NukeComponent nuke)
    {
        var doAfter = new DoAfterArgs(EntityManager, user, nuke.DisarmDoAfterLength, new NukeDisarmDoAfterEvent(), uid, target: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popups.PopupEntity(Loc.GetString("nuke-component-doafter-warning"),
            user,
            user,
            PopupType.LargeCaution);
    }

    private void UpdateAppearance(EntityUid uid, NukeComponent nuke)
    {
        var xform = Transform(uid);

        _appearance.SetData(uid, NukeVisuals.Deployed, xform.Anchored);

        NukeVisualState state;
        if (nuke.PlayedAlertSound)
            state = NukeVisualState.YoureFucked;
        else if (nuke.Status == NukeStatus.ARMED)
            state = NukeVisualState.Armed;
        else
            state = NukeVisualState.Idle;

        _appearance.SetData(uid, NukeVisuals.State, state);
    }
}

public sealed class NukeExplodedEvent : EntityEventArgs
{
    public EntityUid? OwningStation;
}

/// <summary>
///     Raised directed on the nuke when its disarm doafter is successful.
///     So the game knows not to end.
/// </summary>
public sealed class NukeDisarmSuccessEvent : EntityEventArgs
{

}

