using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.UserInterface.Controls;

[CommonSheetlet]
public sealed class SegmentedBarChartSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<SegmentedBarChart>()
                .Prop(SegmentedBarChart.StylePropertyNotchColor, new Color(1f, 1f, 1f, 0.25f))
                .Prop(SegmentedBarChart.StylePropertyBackgroundColor, new Color(0.1f, 0.1f, 0.1f))
                .Prop(SegmentedBarChart.StylePropertyGap, 0f)
                .Prop(SegmentedBarChart.StylePropertyMediumNotchInterval, 5)
                .Prop(SegmentedBarChart.StylePropertyBigNotchInterval, 10)
                .Prop(SegmentedBarChart.StylePropertyMinEntryWidth, 0f)
                .Prop(SegmentedBarChart.StylePropertyMinSmallNotchScreenDistance, 2)
                .Prop(SegmentedBarChart.StylePropertySmallNotchHeight, 0.1f)
                .Prop(SegmentedBarChart.StylePropertyMediumNotchHeight, 0.25f)
                .Prop(SegmentedBarChart.StylePropertyBigNotchHeight, 1f)
                .Prop(SegmentedBarChart.StylePropertyAnimated, true)
                .Prop(SegmentedBarChart.StylePropertyShowBackground, true)
                .Prop(SegmentedBarChart.StylePropertyShowRuler, true),
            E<SegmentedBarChart>()
                .Class(SegmentedBarChart.StyleClassClassicSplitBar)
                .Prop(SegmentedBarChart.StylePropertyGap, 5f)
                .Prop(SegmentedBarChart.StylePropertyMinEntryWidth, 12f)
                .Prop(SegmentedBarChart.StylePropertyShowBackground, false)
                .Prop(SegmentedBarChart.StylePropertyShowRuler, false)
        ];
    }
}
