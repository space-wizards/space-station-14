using Content.Server.Mind.Components;
using Content.Server.Revenant.Components;
using Content.Shared.Examine;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
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

        SubscribeLocalEvent<EssenceComponent, ComponentStartup>(UpdateEssenceAmount);
        SubscribeLocalEvent<EssenceComponent, MobStateChangedEvent>(UpdateEssenceAmount);
        SubscribeLocalEvent<EssenceComponent, MindAddedMessage>(UpdateEssenceAmount);
        SubscribeLocalEvent<EssenceComponent, MindRemovedMessage>(UpdateEssenceAmount);
        SubscribeLocalEvent<EssenceComponent, ExaminedEvent>(OnExamine);
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

    private void UpdateEssenceAmount(EntityUid uid, EssenceComponent component, EntityEventArgs args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;

        switch (mob.CurrentState)
        {
            case DamageState.Alive:
                if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
                    component.EssenceAmount = _random.NextFloat(75f, 100f);
                else
                    component.EssenceAmount = _random.NextFloat(45f, 70f);
                break;
            case DamageState.Critical:
                component.EssenceAmount = _random.NextFloat(35f, 50f);
                break;
            case DamageState.Dead:
                component.EssenceAmount = _random.NextFloat(15f, 20f);
                break;
        }
    }
}
