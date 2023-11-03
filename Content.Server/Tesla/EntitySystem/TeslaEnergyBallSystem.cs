using Content.Server.Administration.Logs;
using Content.Server.Singularity.Components;
using Content.Server.Tesla.Components;
using Content.Shared.Database;
using Content.Shared.Singularity.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Tag;
using Robust.Shared.Physics.Events;
using Content.Server.Lightning.Components;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// A component that tracks an entity's saturation level from absorbing other creatures by touch, and spawns new entities when the saturation limit is reached.
/// </summary>
public sealed class TeslaEnergyBallSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
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

    private void OnStartCollide(Entity<TeslaEnergyBallComponent> tesla, ref StartCollideEvent args)
    {
        if (TryComp<ContainmentFieldComponent>(args.OtherEntity, out var field))
            return;
        if (TryComp<LightningComponent>(args.OtherEntity, out var lightning)) //its dirty. but idk how to setup colliders to not eating lightnings
            return;
        if (TryComp<SinguloFoodComponent>(args.OtherEntity, out var singuloFood))
        {
            AdjustEnergy(tesla, tesla.Comp, singuloFood.Energy);
        }

        var morsel = args.OtherEntity;
        if (!EntityManager.IsQueuedForDeletion(morsel) // I saw it log twice a few times for some reason? (singulo code copy)
            && (HasComp<MindContainerComponent>(morsel)
            || _tagSystem.HasTag(morsel, "HighRiskItem")
            || HasComp<ContainmentFieldGeneratorComponent>(morsel)))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.Extreme, $"{ToPrettyString(morsel)} collided with {ToPrettyString(tesla)} and was turned to dust");
        }

        Spawn(tesla.Comp.ConsumeEffectProto, Transform(args.OtherEntity).Coordinates);
        QueueDel(args.OtherEntity);
        AdjustEnergy(tesla, tesla.Comp, 20f);
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
