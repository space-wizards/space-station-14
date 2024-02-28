using Content.Server.AlertLevel;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Server.AlertLevelOnPress;

public sealed class AlertLevelOnPressSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelOnPressComponent, SwitchPressedEvent>(OnSwitchPressed);
    }

    private void OnSwitchPressed(Entity<AlertLevelOnPressComponent> ent, ref SwitchPressedEvent args)
    {
        if (_station.GetStationInMap(Transform(ent).MapID) is { } station)
            _alertLevel.SetLevel(station, ent.Comp.AlertLevelOnActivate, true, announce: true, force: false, locked: false);
    }
}
