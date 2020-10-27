using Content.Server.Atmos;
using Content.Server.Explosions;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    [ComponentReference(typeof(GasMixtureHolderComponent))]
    public class GasTankComponent : GasMixtureHolderComponent
    {
        private const float MaxExplosionRange = 14f;

        public override string Name => "GasTank";

        private float _pressureResistance;
        private float _distributePressure;

        private int _integrity = 3;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _pressureResistance, "pressureResistance", Atmospherics.OneAtmosphere * 5f);
            serializer.DataField(ref _distributePressure, "distributePressure", Atmospherics.OneAtmosphere);
        }

        public void Update()
        {
            Air?.React(this);
            CheckStatus();
        }

        public void AssumeAir(GasMixture giver)
        {
            Air.Merge(giver);

            CheckStatus();
        }

        private void CheckStatus()
        {
            if (Air == null)
                return;

            var pressure = Air.Pressure;

            if (pressure > Atmospherics.TankFragmentPressure)
            {
                // Give the gas a chance to build up more pressure.
                Air.React(this);
                Air.React(this);
                Air.React(this);
                pressure = Air.Pressure;
                var range = (pressure - Atmospherics.TankFragmentPressure) / Atmospherics.TankFragmentScale;

                // Let's cap the explosion, yeah?
                if (range > MaxExplosionRange)
                {
                    range = MaxExplosionRange;
                }

                Owner.SpawnExplosion((int) (range * 0.25f), (int) (range * 0.5f), (int) (range * 1.5f), 1);

                Owner.Delete();
                return;
            }

            if (pressure > Atmospherics.TankRupturePressure)
            {
                if (_integrity <= 0)
                {
                    var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();
                    tileAtmos?.AssumeAir(Air);

                    EntitySystem.Get<AudioSystem>().PlayAtCoords("Audio/Effects/spray.ogg", Owner.Transform.Coordinates,
                        AudioHelpers.WithVariation(0.125f));

                    Owner.Delete();
                    return;
                }

                _integrity--;
                return;
            }

            if (pressure > Atmospherics.TankLeakPressure)
            {
                if (_integrity <= 0)
                {
                    var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();
                    if (tileAtmos == null)
                        return;

                    var leakedGas = Air.RemoveRatio(0.25f);
                    tileAtmos.AssumeAir(leakedGas);
                } else
                {
                    _integrity--;
                }

                return;
            }

            if (_integrity < 3)
                _integrity++;
        }
    }
}
