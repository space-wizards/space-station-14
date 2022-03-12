using Content.Server.Temperature.Components;
using Content.Shared.Disease;

namespace Content.Server.Disease.Cures
{
    /// <summary>
    ///     Requires the solution entity to be above or below a certain temperature.
    ///     Used for things like cryoxadone and pyroxadone.
    /// </summary>
    public sealed class DiseaseBodyTemperature : DiseaseCure
    {
        [DataField("min")]
        public float Min = 0;

        [DataField("max")]
        public float Max = float.MaxValue;
        public override bool Cure(DiseaseEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.DiseasedEntity, out TemperatureComponent temp))
            {
                if (temp.CurrentTemperature > Min && temp.CurrentTemperature < Max)
                    return true;
            }

            return false;
        }

        public override string CureText()
        {
            return Loc.GetString("disease-cure-temp", ("max", Max), ("min", Min));
        }
    }
}
