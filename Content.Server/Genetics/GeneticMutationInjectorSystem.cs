using Content.Shared.Chemistry.Events;
using Content.Shared.DoAfter;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server.Genetics;

public sealed partial class GeneticMutationInjectorSystem : EntitySystem
{
    [Dependency] private SharedGeneticsSystem _genetics = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticMutationInjectorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GeneticMutationInjectorComponent, InjectorDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, GeneticMutationInjectorComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        if (!_mobState.IsAlive(target))
        {
            _popup.PopupEntity("Cannot inject: target is not a living humanoid", uid, args.User);
            return;
        }

        if (!_interaction.InRangeUnobstructed(args.User, target))
            return;

        if (TryComp<GeneticMutationComponent>(target, out var genetics) && 
            genetics.ActiveMutations.Contains(component.MutationId))
        {
            _popup.PopupEntity("Cannot inject: mutation already active", uid, args.User);
            return;
        }

        args.Handled = true;

        if (args.User == target)
        {
            if (_genetics.TryApplyMutationFromInjector(target, component.MutationId))
            {
                SetMutationDuration(target, component);
                QueueDel(uid);
            }
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(3), 
            new InjectorDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            MovementThreshold = 1f,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, GeneticMutationInjectorComponent component, InjectorDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        if (!_mobState.IsAlive(args.Target.Value))
            return;

        if (_genetics.TryApplyMutationFromInjector(args.Target.Value, component.MutationId))
        {
            SetMutationDuration(args.Target.Value, component);
            QueueDel(uid);
        }
    }

    private void SetMutationDuration(EntityUid target, GeneticMutationInjectorComponent injector)
    {
        if (!TryComp<GeneticMutationComponent>(target, out var genetics))
            return;

        if (injector.ActiveDuration > 0f)
            genetics.TemporaryMutationTimers[injector.MutationId] = injector.ActiveDuration;
        else
            genetics.TemporaryMutationTimers.Remove(injector.MutationId);
        Dirty(target, genetics);
    }
}
