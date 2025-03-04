using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.UserInterface;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    /// <summary>
    ///     Used to calculate when the finale song should start playing
    /// </summary>
    public void SubscribeFinale()
    {
        SubscribeLocalEvent<CosmicFinaleComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<CosmicFinaleComponent, StartFinaleDoAfterEvent>(OnFinaleStartDoAfter);
        SubscribeLocalEvent<CosmicFinaleComponent, CancelFinaleDoAfterEvent>(OnFinaleCancelDoAfter);
    }

    private void OnInteract(Entity<CosmicFinaleComponent> uid, ref InteractHandEvent args)
    {
        if (!HasComp<CosmicCultComponent>(args.User) && uid.Comp.FinaleActive && !args.Handled)
        {
            uid.Comp.Occupied = true;
            var doargs = new DoAfterArgs(EntityManager, args.User, uid.Comp.InteractionTime, new CancelFinaleDoAfterEvent(), uid, uid)
            {
                DistanceThreshold = 1f, Hidden = false, BreakOnHandChange = true, BreakOnDamage = true, BreakOnMove = true
            };
            _popup.PopupEntity(Loc.GetString("cosmiccult-finale-cancel-begin"), args.User, args.User);
            _doAfter.TryStartDoAfter(doargs);
            args.Handled = true;
        }
        else if (HasComp<CosmicCultComponent>(args.User) && uid.Comp.FinaleReady && !args.Handled)
        {
            uid.Comp.Occupied = true;
            var doargs = new DoAfterArgs(EntityManager, args.User, uid.Comp.InteractionTime, new StartFinaleDoAfterEvent(), uid, uid)
            {
                DistanceThreshold = 1f, Hidden = false, BreakOnHandChange = true, BreakOnDamage = true, BreakOnMove = true
            };
            _popup.PopupEntity(Loc.GetString("cosmiccult-finale-beckon-begin"), args.User, args.User);
            _doAfter.TryStartDoAfter(doargs);
            args.Handled = true;
        }
    }

    private void OnFinaleStartDoAfter(Entity<CosmicFinaleComponent> uid, ref StartFinaleDoAfterEvent args)
    {
        var comp = uid.Comp;

        if (args.Args.Target == null || args.Cancelled || args.Handled || !TryComp<MonumentComponent>(args.Args.Target, out var monument) || !TryComp<CosmicCorruptingComponent>(uid, out var corruptingComp))
        {
            uid.Comp.Occupied = false;
            return;
        }

        _popup.PopupEntity(Loc.GetString("cosmiccult-finale-beckon-success"), args.Args.User, args.Args.User);
        if (!comp.BufferComplete)
        {
            corruptingComp.CorruptionSpeed = TimeSpan.FromSeconds(3);
            _appearance.SetData(uid, MonumentVisuals.FinaleReached, 2);
            comp.BufferTimer = _timing.CurTime + comp.BufferRemainingTime;
            comp.SelectedBufferSong = _audio.GetSound(comp.BufferMusic);
            _sound.DispatchStationEventMusic(uid, comp.SelectedBufferSong, StationEventMusicType.CosmicCult);
        }
        else
        {
            corruptingComp.CorruptionSpeed = TimeSpan.FromSeconds(1);
            _appearance.SetData(uid, MonumentVisuals.FinaleReached, 3);
            comp.FinaleTimer = _timing.CurTime + comp.FinaleRemainingTime;
            comp.SelectedFinaleSong = _audio.GetSound(comp.FinaleMusic);
            comp.FinaleSongLength = TimeSpan.FromSeconds(_audio.GetAudioLength(comp.SelectedFinaleSong).TotalSeconds);
            _sound.DispatchStationEventMusic(uid, comp.SelectedFinaleSong, StationEventMusicType.CosmicCult);
        }

        var stationUid = _station.GetStationInMap(Transform(uid).MapID);
        if (stationUid != null)
        {
            _alert.SetLevel(stationUid.Value, "octarine", true, true, true, true);
        }

        if (TryComp<ActivatableUIComponent>(uid, out var uiComp))
            uiComp.Key = MonumentKey.Key; // wow! This is the laziest way to enable a UI ever!

        comp.FinaleReady = false;
        comp.FinaleActive = true;
        monument.Enabled = true;

        Dirty(args.Args.Target!.Value, monument);
        _ui.SetUiState(uid.Owner, MonumentKey.Key, new MonumentBuiState(monument));
    }

    private void OnFinaleCancelDoAfter(Entity<CosmicFinaleComponent> uid, ref CancelFinaleDoAfterEvent args)
    {
        var comp = uid.Comp;
        if (args.Args.Target == null || args.Cancelled || args.Handled)
        {
            uid.Comp.Occupied = false;
            return;
        }

        var stationUid = _station.GetOwningStation(uid);

        if (stationUid != null)
            _alert.SetLevel(stationUid.Value, "green", true, true, true);

        _sound.PlayGlobalOnStation(uid, _audio.GetSound(comp.CancelEventSound));
        _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);

        if (!comp.BufferComplete)
            comp.BufferRemainingTime = comp.BufferTimer - _timing.CurTime + TimeSpan.FromSeconds(15);
        else
            comp.FinaleRemainingTime = comp.FinaleTimer - _timing.CurTime;

        comp.PlayedFinaleSong = false;
        comp.PlayedBufferSong = false;
        comp.FinaleActive = false;
        comp.FinaleReady = true;

        if (TryComp<CosmicCorruptingComponent>(uid, out var corruptingComp)) corruptingComp.CorruptionSpeed = TimeSpan.FromSeconds(6);

        if (TryComp<ActivatableUIComponent>(uid, out var uiComp)) uiComp.Key = null; // wow! This is the laziest way to disable a UI ever!

        _appearance.SetData(uid, MonumentVisuals.FinaleReached, 1);

        if (!TryComp<MonumentComponent>(args.Args.Target, out var monument))
            return;

        Dirty(args.Args.Target!.Value, monument);
        _ui.SetUiState(uid.Owner, MonumentKey.Key, new MonumentBuiState(monument));
    }
}

