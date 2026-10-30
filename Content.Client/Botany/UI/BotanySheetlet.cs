using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Botany.UI;

[CommonSheetlet]
public sealed class BotanySheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<BotanyMetricControl>().Class("MetricHealth").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#E06C75")),
            E<BotanyMetricControl>().Class("MetricGrowth").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#98C379")),
            E<BotanyMetricControl>().Class("MetricTemperature").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#E06C75")),
            E<BotanyMetricControl>().Class("MetricPressure").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#61AFEF")),
            E<BotanyMetricControl>().Class("MetricWater").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#56B6C2")),
            E<BotanyMetricControl>().Class("MetricNutrients").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#E5C07B")),
            E<BotanyMetricControl>().Class("MetricWeeds").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#7AAB55")),
            E<BotanyMetricControl>().Class("MetricPests").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#D19A66")),
            E<BotanyMetricControl>().Class("MetricToxins").Prop(BotanyMetricControl.StylePropertyAccent, Color.FromHex("#C678DD")),
        ];
    }
}
