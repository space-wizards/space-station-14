using Content.Server.Revenant.Components;
using Content.Shared.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Revenant.Components;
using Robust.Shared.Random;

namespace Content.Server.Revenant.EntitySystems;

/// <summary>
/// Attached to entities when a revenant drains them in order to
/// manage their essence.
/// </summary>
public sealed class EssenceSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EssenceComponent, ComponentStartup>(OnEssenceEventReceived);
        SubscribeLocalEvent<EssenceComponent, MobStateChangedEvent>(OnMobstateChanged);
        SubscribeLocalEvent<EssenceComponent, MindAddedMessage>(OnEssenceEventReceived);
        SubscribeLocalEvent<EssenceComponent, MindRemovedMessage>(OnEssenceEventReceived);
        SubscribeLocalEvent<EssenceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMobstateChanged(EntityUid uid, EssenceComponent component, MobStateChangedEvent args)
    {
        UpdateEssenceAmount(uid, component);
    }

    private void OnExamine(EntityUid uid, EssenceComponent component, ExaminedEvent args)
    {
        if (!component.SearchComplete || !HasComp<RevenantComponent>(args.Examiner))
            return;

        string message;
        switch (component.EssenceAmount)
        {
            case <= 45:
                message = "revenant-soul-yield-low";
                break;
            case >= 90:
                message = "revenant-soul-yield-high";
                break;
            default:
                message = "revenant-soul-yield-average";
                break;
        }

        args.PushMarkup(Loc.GetString(message, ("target", uid)));
    }

    private void OnEssenceEventReceived(EntityUid uid, EssenceComponent component, EntityEventArgs args)
    {
        UpdateEssenceAmount(uid, component);
    }

    private void UpdateEssenceAmount(EntityUid uid, EssenceComponent component)
    {
        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;

        switch (mob.CurrentState)
        {
            case MobState.Alive:
                if (TryComp<MindContainerComponent>(uid, out var mind) && mind.Mind != null)
                    component.EssenceAmount = _random.NextFloat(75f, 100f);
                else
                    component.EssenceAmount = _random.NextFloat(45f, 70f);
                break;
            case MobState.Critical:
                component.EssenceAmount = _random.NextFloat(35f, 50f);
                break;
            case MobState.Dead:
                component.EssenceAmount = _random.NextFloat(15f, 20f);
                break;
        }
    }
}
