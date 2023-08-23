using Content.Shared.Examine;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class TemperatureConstructionGraphStep : ConstructionGraphStep
    {
        [DataField("minTemperature")]
        public float? MinTemperature;
        [DataField("maxTemperature")]
        public float? MaxTemperature;

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            float guideTemperature = MinTemperature.HasValue ? MinTemperature.Value : (MaxTemperature.HasValue ? MaxTemperature.Value : 0);
            examinedEvent.PushMarkup(Loc.GetString("construction-temperature-default", ("temperature", guideTemperature)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            float guideTemperature = MinTemperature.HasValue ? MinTemperature.Value : (MaxTemperature.HasValue ? MaxTemperature.Value : 0);

            return new ConstructionGuideEntry()
            {
                Localization = "construction-presenter-temperature-step",
                Arguments = new (string, object)[] { ("temperature", guideTemperature) }
            };
        }
    }
}
