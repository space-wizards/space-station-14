using Content.Server.Chat.Managers;
using Content.Server.Fax;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Chat;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Fax.Components;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingMessageHandlerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadMessageOnChosenComponent, RailroadingCardChosenEvent>(OnChosen);
    }

    private void OnChosen(Entity<RailroadMessageOnChosenComponent> ent, ref RailroadingCardChosenEvent args) 
    {
        if (!_players.TryGetSessionByEntity(args.Subject, out var player))
            return;

        var msg = _random.Pick(ent.Comp.Messages);

        var message = Loc.GetString(msg.Message);
        var wrappedMessage = Loc.GetString(msg.Wrapped);
        _chat.ChatMessageToOne(ChatChannel.Radio, message, wrappedMessage, default, false, player.Channel, msg.Color ?? Color.FromHex("#57A3F7"));
    }
}
