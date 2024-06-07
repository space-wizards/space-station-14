using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Supermatter.Components;
using Content.Shared.Damage;
using Content.Shared.Radiation.Components;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.EntitySystems;

[UsedImplicitly]
public sealed class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterComponent, StartCollideEvent>(OnCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (sm, dmg, xform, xplode, rads)
        in EntityManager.EntityQuery<SupermatterComponent, DamageableComponent, TransformComponent, ExplosiveComponent, RadiationSourceComponent>())
        {
            HandleRads(sm.Owner, frameTime, sm, xform, rads);
        }
    }

    private void HandleRads(EntityUid uid, float frameTime, SupermatterComponent? sm = null, TransformComponent? xform = null, RadiationSourceComponent? rads = null)
    {
        if (!Resolve(uid, ref sm, ref xform, ref rads))
            return;

        var gasMixPowerRatio = 0f;
        var dynamicHeatModifier = 0f;
        var powerTransmissionBonus = 0f;
        var dynamicHeatResistance = 0f;
        var moleHeatPenalty = 0f;
        var powerlossDynamicScaling = 0f;
        var powerlossInhibitor = 0f;

        sm.AtmosUpdateAccumulator += frameTime;
        if (sm.AtmosUpdateAccumulator > SupermatterComponent.AtmosUpdateTimer && _atmos.GetTileMixture(xform.Coordinates) is { } mixture)
        {
            sm.AtmosUpdateAccumulator -= SupermatterComponent.AtmosUpdateTimer;

            var pressure = mixture.Pressure;
            var moles = mixture.TotalMoles;
            var temp = mixture.Temperature;

            if (moles > 0)
            {
                var gases = new float[Enum.GetValues(typeof(Gas)).Length];

                for (int i = 0; i < Enum.GetValues(typeof(Gas)).Length; i++)
                {
                    var gasPercentage = Math.Clamp(mixture.GetMoles(i) / moles, 0, 1);
                    gases[i] = gasPercentage;

                    dynamicHeatResistance += gasPercentage * sm.GasFacts[i].HeatResistance;
                    gasMixPowerRatio += gasPercentage * sm.GasFacts[i].Ratio;
                    dynamicHeatModifier += gasPercentage + sm.GasFacts[i].ReleaseModifier;
                    powerTransmissionBonus = gasPercentage + sm.GasFacts[i].TransmitModifier;
                }

                dynamicHeatResistance = Math.Max(dynamicHeatResistance, 1);
                gasMixPowerRatio = Math.Clamp(gasMixPowerRatio, 0, 1);
                dynamicHeatModifier = Math.Max(dynamicHeatModifier, .5f);

                var h2oBonus = 1 - gases[(int) Gas.WaterVapor] * .25f;
                powerTransmissionBonus *= h2oBonus;

                moleHeatPenalty = Math.Max(moles / moleHeatPenalty, .25f);

                if (moles > SupermatterComponent.PowerlossInhibitionGasThreshold
                && gases[(int) Gas.CarbonDioxide] > SupermatterComponent.PowerlossInhibitionGasThreshold)
                {
                    powerlossDynamicScaling = Math.Clamp(powerlossDynamicScaling + Math.Clamp(gases[(int) Gas.CarbonDioxide] - powerlossDynamicScaling, -.02f, .02f), 0, 1);
                }
                else
                {
                    powerlossDynamicScaling = Math.Clamp(powerlossDynamicScaling - .05f, 0, 1);
                }

                powerlossInhibitor = Math.Clamp(1 - powerlossDynamicScaling * Math.Clamp(moles / SupermatterComponent.PowerlossInhibitionMoleBoostThreshold, 1, 1.5f), 0, 1);

                var tempFactor = gasMixPowerRatio > .8f ? 50f : 30f;

                sm.Power = Math.Max((temp * tempFactor / Atmospherics.T0C * gasMixPowerRatio) + sm.Power, 0);
            }
        }
    }
    private void HandleDamage(EntityUid uid)
    {

    }
}
