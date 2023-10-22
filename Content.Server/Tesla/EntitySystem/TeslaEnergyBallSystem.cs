using Content.Server.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Server.Tesla.Components;
using Robust.Shared.Physics.Events;
using Microsoft.Extensions.DependencyModel;
using Content.Server.Lightning.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Tag;
using Content.Server.Administration.Logs;
using Content.Shared.Singularity.Components;
using Content.Shared.Database;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// A component that tracks an entity's saturation level from absorbing other creatures by touch, and spawns new entities when the saturation limit is reached.
/// </summary>
public sealed class TeslaEnergyBallSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaEnergyBallComponent, StartCollideEvent>(OnStartCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeslaEnergyBallComponent>();
        while (query.MoveNext(out var uid, out var teslaEnergyBall))
        {
            teslaEnergyBall.AccumulatedFrametime += frameTime;

            if (teslaEnergyBall.AccumulatedFrametime < teslaEnergyBall.UpdateInterval)
                continue;

            AdjustEnergy(uid, teslaEnergyBall, -teslaEnergyBall.EnergyLoss * teslaEnergyBall.AccumulatedFrametime);
            teslaEnergyBall.AccumulatedFrametime = 0f;
        }
    }

    private void OnStartCollide(EntityUid uid, TeslaEnergyBallComponent component, ref StartCollideEvent args)
    {
        if (TryComp<ContainmentFieldComponent>(args.OtherEntity, out var field))
            return;
        if (TryComp<SinguloFoodComponent>(args.OtherEntity, out var singuloFood))
        {
            AdjustEnergy(uid, component, singuloFood.Energy);
        }

        var morsel = args.OtherEntity;
        if (!EntityManager.IsQueuedForDeletion(morsel) // I saw it log twice a few times for some reason? (singulo code copy)
            && (HasComp<MindContainerComponent>(morsel)
            || _tagSystem.HasTag(morsel, "HighRiskItem")
            || HasComp<ContainmentFieldGeneratorComponent>(morsel)))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.Extreme, $"{ToPrettyString(morsel)} collided with Tesla and was turned to dust");
        }

        Spawn(component.ConsumeEffectProto, Transform(args.OtherEntity).Coordinates);
        EntityManager.QueueDeleteEntity(args.OtherEntity);
        AdjustEnergy(uid, component, 20f);

        EntityManager.QueueDeleteEntity(args.OtherEntity);
    }
    public void AdjustEnergy(EntityUid uid, TeslaEnergyBallComponent component, float delta)
    {
        component.Energy += delta;

        if (component.Energy > component.NeedEnergyToSpawn)
        {
            component.Energy -= component.NeedEnergyToSpawn;
            Spawn(component.SpawnProto, Transform(uid).Coordinates);
        }
        if (component.Energy < component.EnergyToDespawn)
        {
            _audio.PlayPvs(component.SoundCollapse, uid);
            QueueDel(uid);
        }
    }
}
