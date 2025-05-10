using Content.Shared.Starlight.Medical.Items;
using Content.Shared.Starlight.Medical.Items.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Damage;
using Content.Shared.Audio;
using Content.Shared.Popups;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Items.EntitySystems;

public sealed class PatchSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PatchComponent, UseInHandEvent>(OnPatchUse);
        SubscribeLocalEvent<PatchComponent, AfterInteractEvent>(OnPatchAfterInteract);
        SubscribeLocalEvent<DamageableComponent, PatchDoAfterEvent>(OnDoAfter);
    }
    
    private void OnPatchUse(Entity<PatchComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryApply(ent, ent.Comp, args.User, args.User))
            args.Handled = true;
    }
    
    private void OnPatchAfterInteract(Entity<PatchComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryApply(ent, ent.Comp, args.User, args.Target.Value))
            args.Handled = true;
    }
    
    private bool TryApply(EntityUid patch, PatchComponent patchComponent, EntityUid user, EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var targetDamage))
            return false;

        if (user != target && !_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return false;

        _audio.PlayPvs(patchComponent.ApplyBeginSound, patch, AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        var isNotSelf = user != target;

        if (isNotSelf)
        {
            var msg = Loc.GetString("medical-item-popup-target", ("user", Identity.Entity(user, EntityManager)), ("item", patch));
            _popupSystem.PopupEntity(msg, target, target, PopupType.Medium);
        }

        var delay = isNotSelf ? patchComponent.Delay : patchComponent.Delay; // * GetScaledHealingPenalty(user, patchComponent); TODO

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, delay, new PatchDoAfterEvent(), target, target: target, used: patch)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }
    
    private void OnDoAfter(Entity<DamageableComponent> entity, ref PatchDoAfterEvent args)
    {
        if (!TryComp(args.Used, out PatchComponent? patchComponent))
            return;
        
        var patch = args.Used.Value;

        if (args.Handled || args.Cancelled)
            return;
        
        var patchUser = EnsureComp<PatchUserComponent>(entity);
        patchUser.NextUpdateTime = _gameTiming.CurTime + patchUser.Delay;
        
        Entity<SolutionComponent>? solutionEntity = null;
        if (_solutionContainerSystem.ResolveSolution(patch, patchComponent.SolutionContainer, ref solutionEntity, out var solution))
        {
            foreach(var reagent in solution.Contents)
                patchUser.ReagentsToInsert.Add(reagent); // Move reagents from entity container to user component.
        }
        
        if (!_netManager.IsClient)
            QueueDel(patch); // Remove entity after moving reagents to PatchUser.
    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PatchUserComponent>();
        while (query.MoveNext(out var uid, out var patchUser))
        {
            if (_gameTiming.CurTime < patchUser.NextUpdateTime)
                continue;
            
            if (patchUser.ReagentsToInsert.Count == 0)
            {
                RemComp<PatchUserComponent>(uid);
                continue;
            }
            
            patchUser.NextUpdateTime += patchUser.Delay;
            
            if (_solutionContainerSystem.TryGetInjectableSolution(uid, out var injectableSolution, out _) && injectableSolution != null)
            {
                for (int i = patchUser.ReagentsToInsert.Count - 1; i >= 0; i--)
                {
                    var reagent = patchUser.ReagentsToInsert[i];
                    var quantityToInsert = reagent.Quantity >= patchUser.ReagentInjectAmount ? 0.5 : reagent.Quantity;

                    if (reagent.Quantity <= quantityToInsert)
                        patchUser.ReagentsToInsert.RemoveAt(i);
                    else
                    {
                        reagent.SetQuantity(reagent.Quantity - quantityToInsert);
                        patchUser.ReagentsToInsert[i] = reagent;
                    }

                    var reagentToInject = new ReagentQuantity(reagent.Reagent, quantityToInsert);
                    var solutionToInject = new Solution();
                    solutionToInject.AddReagent(reagentToInject);
                    _solutionContainerSystem.Inject(uid, injectableSolution.Value, solutionToInject);
                }
            }
                
        }
    }
}