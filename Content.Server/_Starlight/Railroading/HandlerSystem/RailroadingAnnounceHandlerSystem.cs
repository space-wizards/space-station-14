using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Starlight.Economy;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingAnnounceHandlerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadAnnounceOnChosenComponent, RailroadingCardChosenEvent>(OnChosen);
    }

    private void OnChosen(Entity<RailroadAnnounceOnChosenComponent> ent, ref RailroadingCardChosenEvent args) 
        => _chatSystem.DispatchFilteredAnnouncement
        (
            Filter.Empty().AddWhere(_gameTicker.UserHasJoinedGame),
            Loc.GetString(_random.Pick(ent.Comp.Text)),
            playSound: ent.Comp.PlaySound,
            announcementSound: ent.Comp.Sound,
            colorOverride: ent.Comp.Color
        );
}
