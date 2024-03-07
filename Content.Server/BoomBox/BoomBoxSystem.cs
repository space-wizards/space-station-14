using Content.Server.Interaction;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System;
using System.Timers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using AudioComponent = Robust.Shared.Audio.Components.AudioComponent;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Content.Shared.Tag;
using Content.Shared.Popups;
using Content.Shared.BoomBox;
using Content.Server.NPC.HTN;
using Content.Server.UserInterface;
using Content.Shared.UserInterface;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Server.Speech.Components;
using Content.Server.Radio.Components;
using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;

namespace Content.Server.BoomBox;

public sealed class BoomBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BoomBoxComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<BoomBoxComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<BoomBoxComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

        //User
        SubscribeLocalEvent<BoomBoxComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<BoomBoxComponent, GotEmaggedEvent>(OnEmagged);

        // UI
        SubscribeLocalEvent<BoomBoxComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxPlusVolMessage>(OnPlusVolButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxMinusVolMessage>(OnMinusVolButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxStartMessage>(OnStartButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxStopMessage>(OnStopButtonPressed);
    }


    // This method makes it possible to insert cassettes into the boombox
    private void OnComponentInit(EntityUid uid, BoomBoxComponent component, ComponentInit args)
    {

        foreach (var slot in component.Slots)
        {
            _itemSlotsSystem.AddItemSlot(uid, slot.Key, slot.Value);
        }

        _signalSystem.EnsureSourcePorts(uid, component.Port);
    }

    private void OnItemInserted(EntityUid uid, BoomBoxComponent comp, EntInsertedIntoContainerMessage args)
    {
        _popup.PopupEntity(Loc.GetString("tape-in"), uid);

        // We change the value of this field to prevent the boombox from being turned on without a cassette.
        comp.Inserted = true;

        // This method is an intermediate step where we embed additional checks.
        UpdateSoundPath(uid, comp);
    }

    private void OnItemRemoved(EntityUid uid, BoomBoxComponent comp, EntRemovedFromContainerMessage args)
    {
        _popup.PopupEntity(Loc.GetString("tape-out"), uid);

        // Turn off the playback of the melody, because the cassette is no longer there
        comp.Stream = _audioSystem.Stop(comp.Stream);

        // We change the value of this field to prevent the boombox from being turned on without a cassette.
        comp.Inserted = false;
        comp.Enabled = false;
    }

    // This method is an intermediate step where we embed additional checks.
    private void UpdateSoundPath(EntityUid uid, BoomBoxComponent comp)
    {
        foreach (var slot in comp.Slots.Values)
        {
            if (slot.ContainerSlot is not null && slot.ContainerSlot.ContainedEntity is not null)
                AddCurrentSoundPath(uid, comp, (EntityUid) slot.ContainerSlot.ContainedEntity);
        }
    }

    // This method updates the path to the music being played. That is why the initial value of the field is not particularly important
    private void AddCurrentSoundPath(EntityUid uid, BoomBoxComponent comp, EntityUid added)
    {
        
        var tagComp = EnsureComp<TagComponent>(uid);

        if (!TryComp<BoomBoxTapeComponent>(added, out var BoomBoxTapeComp) || BoomBoxTapeComp.SoundPath is null)
            return;


        comp.SoundPath = BoomBoxTapeComp.SoundPath;
    }

    private void OnMinusVolButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxMinusVolMessage args)
    {
        MinusVol(uid, component);
    }

    private void OnPlusVolButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxPlusVolMessage args)
    {
        PlusVol(uid, component);
    }

    private void OnStartButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxStartMessage args)
    {
        StartPlay(uid, component);
    }

    private void OnStopButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxStopMessage args)
    {
        StopPlay(uid, component);
    }

    private void OnToggleInterface(EntityUid uid, BoomBoxComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    // ----------------------------------------------------------------------------------------------------------------

    private void UpdateUserInterface(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        bool canPlusVol = true;
        bool canMinusVol = true;
        bool canStop = false;
        bool canStart = false;

        if (component.Volume == 5)
            canPlusVol = false;

        if (component.Volume == -13)
            canMinusVol = false;

        if (component.Inserted)
        {
            if (component.Enabled)
            {
                canStart = false;
                canStop = true;
            }
            else
            {
                canStart = true;
                canStop = false;
            }
        }
        else
        {
            canStart = false;
            canStop = false;
        }


        var state = new BoomBoxUiState(canPlusVol, canMinusVol, canStop, canStart);
        _userInterface.TrySetUiState(uid, BoomBoxUiKey.Key, state);
    }

    private void MinusVol(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Volume = component.Volume - 3f;
        _audioSystem.SetVolume(component.Stream, component.Volume);

        _signalSystem.InvokePort(uid, component.Port);

        UpdateUserInterface(uid, component);
    }

    private void PlusVol(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Volume = component.Volume + 3f;
        _audioSystem.SetVolume(component.Stream, component.Volume);

        _signalSystem.InvokePort(uid, component.Port);

        UpdateUserInterface(uid, component);
    }

    private void StartPlay(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Inserted && !component.Enabled)
        {
            component.Enabled = true;

            _popup.PopupEntity(Loc.GetString("boombox-on"), uid);

            // We play music with these parameters. Be sure to set "WithLoop(true)" this will allow the music to play indefinitely.
            component.Stream = _audioSystem.PlayPvs(component.SoundPath, uid, AudioParams.Default.WithVolume(component.Volume).WithLoop(true).WithMaxDistance(7f))?.Entity;
        }

        _signalSystem.InvokePort(uid, component.Port);

        CheckSyndStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void StopPlay(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if(component.Inserted && component.Enabled)
        {
            component.Enabled = false;

            _popup.PopupEntity(Loc.GetString("boombox-off"), uid);

            // Turning off the looped audio stream
            component.Stream = _audioSystem.Stop(component.Stream);
        }

        _signalSystem.InvokePort(uid, component.Port);

        UpdateUserInterface(uid, component);
    }

    private void CheckSyndStatus(EntityUid uid, BoomBoxComponent comp)
    {
        foreach (var slot in comp.Slots.Values)
        {
            if (slot.ContainerSlot is not null && slot.ContainerSlot.ContainedEntity is not null)
                SpawnSyndEntity(uid, comp, (EntityUid) slot.ContainerSlot.ContainedEntity);
        }
    }

    private void SpawnSyndEntity(EntityUid uid, BoomBoxComponent comp, EntityUid added)
    {
        var tagComp = EnsureComp<TagComponent>(uid);

        if (!TryComp<BoomBoxTapeComponent>(added, out var BoomBoxTapeComp) || BoomBoxTapeComp.SyndStatus == false)
            return;

        if (BoomBoxTapeComp.SyndStatus && !BoomBoxTapeComp.Used)
        {
            _popup.PopupEntity(Loc.GetString("boombox-synd-spawn"), uid);
            var product = EntityManager.SpawnEntity(BoomBoxTapeComp.SyndItem, Transform(uid).Coordinates);
            _hands.PickupOrDrop(comp.User, product);

            BoomBoxTapeComp.Used = true;
        }
    }

    private void OnInteractUsing(EntityUid uid, BoomBoxComponent component, InteractUsingEvent args)
    {
        component.User = args.User;
    }

    public void OnEmagged(EntityUid uid, BoomBoxComponent component, ref GotEmaggedEvent args)
    {
        var comp = _entities.AddComponent<RadioMicrophoneComponent>(uid);
        comp.Enabled = true;
        comp.BroadcastChannel = "Syndicate";
        comp.ToggleOnInteract = false;
        comp.ListenRange = 4;

        var comp2 = _entities.AddComponent<ActiveListenerComponent>(uid);
        comp2.Range = 4;

        _popup.PopupEntity(Loc.GetString("boombox-emagged"), uid);
        _audioSystem.PlayPvs(component.EmagSound, uid);
        args.Handled = true;
    }
}
