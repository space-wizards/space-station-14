using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Medical;
using Content.Server.Popups;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Server.Audio;
using Content.Server.Nutrition.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Tag;
using Content.Server.DeadSpace.Abilities.Felinid;

namespace Content.Server.DeadSpace.Races;

public sealed class FelinidSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    [ValidatePrototypeId<EntityPrototype>] private const string EatMouseActionId = "EatMouseAction";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, MapInitEvent>(OnMapInit);
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

    private void OnMapInit(EntityUid uid, FelinidComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.HairballActionEntity, component.HairballAction, uid);
    }

    private void OnEquipped(EntityUid uid, FelinidComponent component, DidEquipHandEvent args)
    {
        if (!_tagSystem.HasTag(args.Equipped, "FelinidFood"))
            return;

        component.PotentialTarget = args.Equipped;

        _actionsSystem.AddAction(uid, ref component.EatMouse, EatMouseActionId);
    }

    private void OnUnequipped(EntityUid uid, FelinidComponent component, DidUnequipHandEvent args)
    {
        if (args.Unequipped == component.PotentialTarget)
        {
            component.PotentialTarget = null;
            _actionsSystem.RemoveAction(uid, component.EatMouse);
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
        _audio.PlayEntity("/Audio/_DeadSpace/Effects/Species/hairball.ogg", Filter.Pvs(uid), uid, true, AudioHelpers.WithVariation(0.15f));

        EnsureComp<CoughingUpHairballComponent>(uid);
        args.Handled = true;
    }

    private void OnEatMouse(EntityUid uid, FelinidComponent component, EatMouseActionEvent args)
    {
        if (component.PotentialTarget == null)
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

        if (_actionsSystem.TryGetActionData(component.HairballActionEntity, out var action))
        {
            _actionsSystem.SetCharges(component.HairballActionEntity, action.Charges + 1);
        }

        _actionsSystem.SetEnabled(component.HairballActionEntity, true);

        QueueDel(component.PotentialTarget.Value);
        component.PotentialTarget = null;

        _audio.PlayEntity(component.EatSound, Filter.Pvs(uid), uid, true, AudioHelpers.WithVariation(0.15f));

        _hungerSystem.ModifyHunger(uid, 70f, hunger);

        _actionsSystem.RemoveAction(uid, component.EatMouse);
    }

    private void SpawnHairball(EntityUid uid, FelinidComponent component)
    {
        var hairball = EntityManager.SpawnEntity(component.HairballPrototype, Transform(uid).Coordinates);
        var hairballComp = Comp<HairballComponent>(hairball);

        if (TryComp<BloodstreamComponent>(uid, out var bloodStream) &&
            _solutionContainer.ResolveSolution(uid, bloodStream.ChemicalSolutionName, ref bloodStream.ChemicalSolution))
        {
            var vomitChemstreamAmount = _solutionContainer.SplitSolution(bloodStream.ChemicalSolution.Value, 20);

            if (_solutionContainer.TryGetSolution(hairball, hairballComp.SolutionName, out var hairballSolution))
            {
                _solutionContainer.TryAddSolution(hairballSolution.Value, vomitChemstreamAmount);
            }
        }
    }
    private void OnHairballHit(EntityUid uid, HairballComponent component, ThrowDoHitEvent args)
    {
        if (HasComp<FelinidComponent>(args.Target) || !HasComp<StatusEffectsComponent>(args.Target))
            return;

        component.VomitChance = Math.Clamp(component.VomitChance, 0, 1);

        if (_robustRandom.Prob(component.VomitChance))
            _vomitSystem.Vomit(args.Target);
    }

    private void OnHairballPickupAttempt(EntityUid uid, HairballComponent component, GettingPickedUpAttemptEvent args)
    {
        if (HasComp<FelinidComponent>(args.User) || !HasComp<StatusEffectsComponent>(args.User))
            return;

        component.VomitChance = Math.Clamp(component.VomitChance, 0, 1);

        if (_robustRandom.Prob(component.VomitChance))
        {
            _vomitSystem.Vomit(args.User);
            args.Cancel();
        }
    }
}
