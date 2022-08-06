using Content.Server.Temperature.Components;
using Content.Shared.Disease;

namespace Content.Server.Disease.Cures
{
    /// <summary>
    ///     Cures the disease if temperature is within certain bounds.
    /// </summary>
    public sealed class DiseaseBodyTemperatureCure : DiseaseCure
    {
        [DataField("min")]
        public float Min = 0;

        [DataField("max")]
        public float Max = float.MaxValue;
        public override bool Cure(DiseaseEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.DiseasedEntity, out TemperatureComponent? temp))
                return false;

            return temp.CurrentTemperature > Min && temp.CurrentTemperature < float.MaxValue;
        }

        public override string CureText()
        {
            if (Min == 0)
                return Loc.GetString("diagnoser-cure-temp-max", ("max", Math.Round(Max)));
            if (Max == float.MaxValue)
                return Loc.GetString("diagnoser-cure-temp-min", ("min", Math.Round(Min)));

            return Loc.GetString("diagnoser-cure-temp-both", ("max", Math.Round(Max)), ("min", Math.Round(Min)));
        }
    }
}
