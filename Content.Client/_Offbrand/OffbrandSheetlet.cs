using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Offbrand;

[CommonSheetlet]
public sealed class OffbrandSheetlet : Sheetlet<PalettedStylesheet>
{
    public const string ClassFieldLabel = "field-label";
    public const string ClassFieldValue = "field-value";
    public const string ClassFieldLabelLarge = "field-label-large";
    public const string ClassFieldValueLarge = "field-value-large";
    public const string ClassFieldUnit = "field-unit";
    public const string ClassFieldAir = "field-air";
    public const string ClassFieldBlood = "field-blood";
    public const string ClassFieldHeart = "field-heart";

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E()
                .Class(ClassFieldLabel)
                .Font(sheet.BaseFont.GetFont(11)),
            E()
                .Class(ClassFieldLabelLarge)
                .Font(sheet.BaseFont.GetFont(12)),
            E()
                .Class(ClassFieldValueLarge)
                .Font(sheet.BaseFont.GetFont(22, FontKind.Bold)),
            E()
                .Class(ClassFieldValue)
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold)),
            E()
                .Class(ClassFieldUnit)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold))
                .FontColor(Color.DarkGray),
            E()
                .Class(ClassFieldAir)
                .FontColor(Color.FromHex("#44f0d3")),
            E()
                .Class(ClassFieldBlood)
                .FontColor(Color.FromHex("#ff6c7f")),
            E()
                .Class(ClassFieldHeart)
                .FontColor(Color.FromHex("#ff8255")),
        ];
    }
}
