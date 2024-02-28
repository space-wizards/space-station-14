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
using JetBrains.FormatRipper.Elf;

namespace Content.Server.ChangeAlertLevel;

public sealed class AlertLevelOnPressSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelOnPressComponent, ActivateInWorldEvent>(OnButtonPressed);
    }

    private void OnButtonPressed(Entity<AlertLevelOnPressComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_station.GetStationInMap(Transform(ent).MapID) is { } station)
            _alertLevel.SetLevel(station, ent.Comp.AlertLevelOnActivate, true, announce: true, force: false, locked: false);
    }
}
