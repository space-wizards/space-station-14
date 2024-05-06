using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Audio;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Abilities.Felinid;

public sealed partial class FelinidSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FelinidComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FelinidComponent, HairballActionEvent>(OnHairball);
        SubscribeLocalEvent<FelinidComponent, EatMouseActionEvent>(OnEatMouse);
        SubscribeLocalEvent<FelinidComponent, DidEquipHandEvent>(OnEquipped);
        SubscribeLocalEvent<FelinidComponent, DidUnequipHandEvent>(OnUnequipped);
        SubscribeLocalEvent<HairballComponent, ThrowDoHitEvent>(OnHairballHit);
        SubscribeLocalEvent<HairballComponent, GettingPickedUpAttemptEvent>(OnHairballPickupAttempt);
    }

    private Queue<EntityUid> RemQueue = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var cat in RemQueue)
        {
            RemComp<CoughingUpHairballComponent>(cat);
        }
        RemQueue.Clear();

        foreach (var (hairballComp, catComp) in EntityQuery<CoughingUpHairballComponent, FelinidComponent>())
        {
            hairballComp.Accumulator += frameTime;
            if (hairballComp.Accumulator < hairballComp.CoughUpTime.TotalSeconds)
                continue;

            hairballComp.Accumulator = 0;
            SpawnHairball(hairballComp.Owner, catComp);
            RemQueue.Enqueue(hairballComp.Owner);
        }
    }

    private void OnInit(EntityUid uid, FelinidComponent component, ComponentInit args)
    {
        if (component.HairballAction != null)
            return;

        //component.HairballAction = Spawn("ActionHairball");
        _actionsSystem.AddAction(uid, ref component.HairballAction, component.HairballActionId);
    }

    private void OnEquipped(EntityUid uid, FelinidComponent component, DidEquipHandEvent args)
    {
        if (!HasComp<FelinidFoodComponent>(args.Equipped))
            return;

        component.EatActionTarget = args.Equipped;

        //component.EatAction = Spawn("ActionEatMouse");
        _actionsSystem.AddAction(uid, ref component.EatAction, component.EatActionId);
    }

    private void OnUnequipped(EntityUid uid, FelinidComponent component, DidUnequipHandEvent args)
    {
        if (args.Unequipped == component.EatActionTarget)
        {
            component.EatActionTarget = null;
            if (component.EatAction != null)
                _actionsSystem.RemoveAction(uid, component.EatAction.Value);
        }
    }

    private void OnHairball(EntityUid uid, FelinidComponent component, HairballActionEvent args)
    {
        if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
        EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
        blocker.Enabled)
        {
            _popupSystem.PopupEntity(Loc.GetString("hairball-mask", ("mask", maskUid)), uid, uid);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("hairball-cough", ("name", Identity.Entity(uid, EntityManager))), uid);
        _audio.PlayPvs("/Audio/Nyanotrasen/Effects/Species/hairball.ogg", uid, AudioHelpers.WithVariation(0.15f));

        EnsureComp<CoughingUpHairballComponent>(uid);
        args.Handled = true;
    }

    private void OnEatMouse(EntityUid uid, FelinidComponent component, EatMouseActionEvent args)
    {
        if (component.EatActionTarget == null)
            return;

        if (!TryComp<HungerComponent>(uid, out var hunger))
            return;

        if (hunger.CurrentThreshold == Shared.Nutrition.Components.HungerThreshold.Overfed)
        {
            _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more"), uid, uid, Shared.Popups.PopupType.SmallCaution);
            return;
        }

        if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
        EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
        blocker.Enabled)
        {
            _popupSystem.PopupEntity(Loc.GetString("hairball-mask", ("mask", maskUid)), uid, uid, Shared.Popups.PopupType.SmallCaution);
            return;
        }

        if (component.HairballAction != null)
        {
            _actionsSystem.SetCharges(component.HairballAction, 1); // You get the charge back and that's it. Tough.
            _actionsSystem.SetEnabled(component.HairballAction, true);
        }
        Del(component.EatActionTarget.Value);
        component.EatActionTarget = null;

        _audio.PlayPvs("/Audio/DeltaV/Items/eatfood.ogg", uid, AudioHelpers.WithVariation(0.15f));

        _hungerSystem.ModifyHunger(uid, 50f, hunger);

        if (component.EatAction != null)
            _actionsSystem.RemoveAction(uid, component.EatAction.Value);
    }

    private void SpawnHairball(EntityUid uid, FelinidComponent component)
    {
        var hairball = EntityManager.SpawnEntity(component.HairballPrototype, Transform(uid).Coordinates);
        var hairballComp = Comp<HairballComponent>(hairball);

        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) && bloodstream.ChemicalSolution.HasValue)
        {
            var temp = _solutionSystem.SplitSolution(bloodstream.ChemicalSolution.Value, 20);

            if (_solutionSystem.TryGetSolution(hairball, hairballComp.SolutionName, out var hairballSolution))
            {
                _solutionSystem.TryAddSolution(hairballSolution.Value, temp);
            }
        }
    }
    private void OnHairballHit(EntityUid uid, HairballComponent component, ThrowDoHitEvent args)
    {
        if (HasComp<FelinidComponent>(args.Target) || !HasComp<StatusEffectsComponent>(args.Target))
            return;
        if (_robustRandom.Prob(0.2f))
            _vomitSystem.Vomit(args.Target);
    }

    private void OnHairballPickupAttempt(EntityUid uid, HairballComponent component, GettingPickedUpAttemptEvent args)
    {
        if (HasComp<FelinidComponent>(args.User) || !HasComp<StatusEffectsComponent>(args.User))
            return;

        if (_robustRandom.Prob(0.2f))
        {
            _vomitSystem.Vomit(args.User);
            args.Cancel();
        }
    }
}
