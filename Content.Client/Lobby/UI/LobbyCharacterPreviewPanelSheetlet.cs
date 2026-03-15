using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Lobby.UI;

[CommonSheetlet]
public sealed class LobbyCharacterPreviewPanelSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var inactivePanel = new StyleBoxEmpty {};
        var activePanel = new StyleBoxFlat { BackgroundColor = sheet.HighlightPalette.HoveredElement };

        return
        [
            E<DraggableJobTarget>()
                .ParentOf(E<PanelContainer>())
                .Prop(PanelContainer.StylePropertyPanel, inactivePanel),
            E<DraggableJobTarget>()
                .Pseudo(DraggableJobTarget.StylePseudoClassActive)
                .ParentOf(E<PanelContainer>())
                .Prop(PanelContainer.StylePropertyPanel, activePanel),
            E<Collapsible>()
                .Identifier("lobbyCollapsibleSubsection")
                .ParentOf(E<CollapsibleHeading>())
                .ParentOf(E<BoxContainer>())
                .Prop(BoxContainer.StylePropertySeparation, 5),
            E<Collapsible>()
                .Identifier("lobbyCollapsibleSubsection")
                .ParentOf(E<CollapsibleHeading>())
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<Label>())
                .FontColor(sheet.HighlightPalette.Text)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold)),
        ];
    }
}
