using System.Linq;
using System.Numerics;
using Content.Client._Starlight.NewLife;
using Content.Client._Starlight.UI;
using Content.Client.Eui;
using Content.Client.Lobby;
using Content.Shared._Starlight.Railroading;
using Content.Shared.Eui;
using Content.Shared.Starlight.NewLife;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client._Starlight.Railroading;

[UsedImplicitly]
public sealed class CardSelectionEui : BaseEui
{
    private List<IDisposable> _disposables = [];
    private static readonly Vector2 _cardSize = new(264, 370);
    private static readonly Vector2 _cardContentSize = new(254, 200);
    private static readonly Vector2 _cardDescSize = new(254, 180);
    private SLWindow _window;

    public CardSelectionEui()
    {
        _window = new SLWindow();
        _window.OnClose += ()=> SendMessage(new CardSelectionClosedMessage());
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase baseState)
    {
        base.HandleState(baseState);

        if (baseState is not CardSelectionEuiState state)
            return;

        var size = new Vector2(_cardSize.X * state.Cards.Count, _cardSize.Y);
        _window.Resizable = false;
        _window.Contents.SetSize = size;
        _window.Contents.MinSize = size;
        _window.Contents.MaxSize = size;

        _window.Title = Loc.GetString("card-selection-window-title");

        _window.Contents.RemoveAllChildren();
        _window
            .Box
            (
                BoxContainer.LayoutOrientation.Horizontal,
                box => state
                    .Cards.ForEach(card => box
                    .Layout(layout =>
                    {
                        layout.FixSize(_cardSize);
                        if (card.Image?.TexturePath is not null)
                            layout.TextureRect(textureRect =>
                            {
                                textureRect.Margin = new Thickness(5);
                                textureRect.MaxSize = _cardContentSize;
                                textureRect.TexturePath = card.Image.TexturePath.ToString();
                                textureRect.Stretch = TextureRect.StretchMode.KeepAspect;
                            });
                        layout.Box(BoxContainer.LayoutOrientation.Vertical,
                            cardBox =>
                            {
                                cardBox.MinSize = _cardSize;
                                cardBox.MaxSize = _cardSize;
                                RenderCard(cardBox, card);
                            });
                        layout.Button(
                            button => button
                                .WhenPressed(_=>
                                {
                                    SendMessage(new CardSelectedMessage() { Card = card.Id });
                                    Closed();
                                })
                                .WhenMouseEntered(_ => button.Modulate(Color.ForestGreen))
                                .WhenMouseExited(_ => button.Modulate(card.Color))
                                .FixSize(_cardSize)
                                .AddClass("CardBorder")
                                .Modulate(card.Color)
                        );
                    }))
            );
    }
    private static void RenderCard(SLBox cardBox, Card card)
    {
        cardBox.Box(BoxContainer.LayoutOrientation.Horizontal,
            box =>
            {
                box.Panel(panel => panel
                    .Label(x => x.WithText(card.Title))
                    .WithHorizontalExp()
                    .WithMargin(new Thickness(5, 5, -1, 0))
                    .WithVAlignment(Control.VAlignment.Top)
                    .AddClass("CardHeader")
                    .Modulate(card.Color));
                if (card.Icon is not null)
                    box.Panel(panel => panel
                        .WithMargin(new Thickness(0, 5, 0, 0))
                        .AddClass("AngleRect")
                        .Modulate(card.Color)
                        .Label(x => x.WithText(card.Icon).WithFont("/Fonts/_Starlight/GameIcons/game-icons.ttf", 32).Modulate(card.IconColor)));
            });
        cardBox.Panel(panel =>
        {
            panel.AddClass("AngleRect")
                .Modulate(Color.FromHex("#080808"))
                .WithHorizontalExp()
                .WithVAlignment(Control.VAlignment.Bottom);

            // To-do: rework the layout once it becomes clear why the alignment isn’t working.
            panel.Margin = new Thickness(0, 185, 0, 0);
            panel.MinSize = _cardDescSize;
            panel.MaxSize = _cardDescSize;

            panel.RichText(label => label
                    .WithText(card.Description).WithVAlignment(Control.VAlignment.Top));
        });
    }
}
