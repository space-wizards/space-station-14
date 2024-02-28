using Content.Shared.Interaction;
using Content.Shared.ChangeAlertLevel;
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
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.ChangeAlertLevel;

public sealed class ChangeAlertLevelSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeAlertLevelComponent, ActivateInWorldEvent>(OnActivated);
    }

    public void OnActivated(Entity<ChangeAlertLevelComponent> ent, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        var buttonXform = Transform(ent.Owner);
        var stationUid = _station.GetStationInMap(buttonXform.MapID);

        var playsound = true;
        var announce = true;
        var force = false;
        var locked = false;

        if (stationUid != null)
            _alertLevel.SetLevel(stationUid.Value, ent.Comp.AlertLevelOnActivate, playsound, announce, force, locked);
        Dirty(ent.Owner, ent.Comp);

        _audio.PlayPvs(ent.Comp.ClickSound, ent.Owner, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));

        args.Handled = true;
    }
}
