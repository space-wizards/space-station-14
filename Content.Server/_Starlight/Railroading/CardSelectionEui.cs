using System.Linq;
using Content.Server._Starlight.Railroading;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Shared._Starlight.Railroading;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Starlight.GhostTheme;
using Content.Shared.Starlight.NewLife;

namespace Content.Server.Ghost.Roles.UI;

public sealed class CardSelectionEui : BaseEui
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    public required Entity<RailroadableComponent> Subject { get; init; }

    public CardSelectionEui() => IoCManager.InjectDependencies(this);
    public override CardSelectionEuiState GetNewState() => new()
    {
        Cards = Subject.Comp.IssuedCards != null
            ? [.. Subject.Comp.IssuedCards.Select(_systems.GetEntitySystem<RailroadingSystem>().EntToCard)]
            : []
    };
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        switch (msg)
        {
            case CardSelectedMessage selectedMessage:
                _systems.GetEntitySystem<RailroadingSystem>().OnCardSelected(Subject, selectedMessage.Card);
                break;
            case CardSelectionClosedMessage:
                _systems.GetEntitySystem<RailroadingSystem>().OnCardSelectionClosed(Subject);
                break;
            default:
                break;
        }
    }

    public override void Closed()
    {
        base.Closed();
    }
}
