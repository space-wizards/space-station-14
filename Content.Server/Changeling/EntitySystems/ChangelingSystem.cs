using Content.Server.Actions;
using Content.Shared.Inventory;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Store;
using Content.Server.Traitor.Uplink;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Server.Polymorph.Systems;
using System.Linq;
using Content.Server.Forensics;
using Content.Shared.Actions;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Alert;
using Content.Shared.Stealth.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Chemistry.Containers.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainers = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ChangelingComponent, ChangelingEvolutionMenuActionEvent>(OnShop);
        SubscribeLocalEvent<ChangelingComponent, ChangelingCycleDNAActionEvent>(OnCycleDNA);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformActionEvent>(OnTransform);

        InitializeLingAbilities();
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        _uplink.AddUplink(uid, FixedPoint2.New(10), ChangelingShopPresetPrototype, uid, EvolutionPointsCurrencyPrototype); // not really an 'uplink', but it's there to add the evolution menu

        RemComp<HungerComponent>(uid);
        RemComp<ThirstComponent>(uid); // changelings dont get hungry or thirsty
    }

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingEvolutionMenuId = "ActionChangelingEvolutionMenu";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingRegenActionId = "ActionLingRegenerate";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorbActionId = "ActionChangelingAbsorb";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingDNACycleActionId = "ActionChangelingCycleDNA";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransformActionId = "ActionChangelingTransform";

    [ValidatePrototypeId<CurrencyPrototype>]
    public const string EvolutionPointsCurrencyPrototype = "EvolutionPoints";

    [ValidatePrototypeId<StorePresetPrototype>]
    public const string ChangelingShopPresetPrototype = "StorePresetChangeling";

    public bool ChangeChemicalsAmount(EntityUid uid, ChangelingComponent component, int amount, bool regenCap = true)
    {
        component.Chemicals += amount;

        if (regenCap)
            float.Min(component.Chemicals, component.MaxChemicals);

        _alerts.ShowAlert(uid, AlertType.Chemicals);

        return true;
    }

    private bool TryUseAbility(EntityUid uid, ChangelingComponent component, int abilityCost, bool activated = true, float regenCost = 0f)
    {
        if (component.Chemicals <= Math.Abs(abilityCost) && activated)
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), uid, uid);
            return false;
        }

        return true;
    }

    private bool TryStingTarget(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("changeling-sting-fail-target");
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }

        return true;
    }

    private void TryReagentStingTarget(EntityUid uid, EntityUid target, ChangelingComponent component, string reagentId, FixedPoint2 reagentAmount, bool doDigestionDelay = false)
    {
        if (TryStingTarget(uid, target, component))
        {
            var solution = new Solution();
            solution.AddReagent(reagentId, reagentAmount);

            if (doDigestionDelay)
            {
                if (!TryComp<BodyComponent>(target, out var body))
                    return;
                if (!_body.TryGetBodyOrganComponents<StomachComponent>(target, out var stomachs, body))
                    return;

                var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Comp.Owner, solution, stomach.Comp));

                if (firstStomach == null)
                    return;

                _reaction.DoEntityReaction(target, solution, ReactionMethod.Ingestion);
                //TODO: Grab the stomach UIDs somehow without using Owner
                _stomach.TryTransferSolution(firstStomach.Value.Comp.Owner, solution, firstStomach.Value.Comp);
            }
            else
            {
                if (!_solutionContainers.TryGetInjectableSolution(target, out var targetSoln, out var targetSolution))
                    return;
                if (!targetSolution.CanAddSolution(solution))
                    return;

                _solutionContainers.TryAddSolution(targetSoln.Value, solution);
            }
        }
    }

    private void OnMapInit(EntityUid uid, ChangelingComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ChangelingEvolutionMenuId);
        _action.AddAction(uid, ChangelingRegenActionId);
        _action.AddAction(uid, ChangelingAbsorbActionId);
        _action.AddAction(uid, ChangelingDNACycleActionId);
        _action.AddAction(uid, ChangelingTransformActionId);
    }

    private void OnShop(EntityUid uid, ChangelingComponent component, ChangelingEvolutionMenuActionEvent args)
    {
        _store.OnInternalShop(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChangelingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Accumulator += frameTime;

            if (comp.Accumulator <= comp.ChemicalRegenTime)
                continue;
            comp.Accumulator -= comp.ChemicalRegenTime;

            if (_mobState.IsDead(uid)) // if ling is dead dont regenerate chemicals
                return;

            if (comp.Chemicals < comp.MaxChemicals)
            {
                ChangeChemicalsAmount(uid, comp, 1);
            }
        }
    }

    public bool StealDNA(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidappearance))
        {
            return false;
        }

        return true;
    }

    public static void OnCycleDNA(EntityUid uid, ChangelingComponent component, ChangelingCycleDNAActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        component.SelectedDNA += 1;

        if (component.StoredDNA.Count >= component.DNAStrandCap || component.SelectedDNA >= component.StoredDNA.Count)
            component.SelectedDNA = 0;

        // var selfMessage = Loc.GetString("changeling-dna-switchdna", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
        // _popup.PopupEntity(selfMessage, uid, uid);
    }

    public void OnTransform(EntityUid uid, ChangelingComponent component, ChangelingTransformActionEvent args)
    {
        if (args.Handled)
            return;

        // var selfMessage = Loc.GetString("changeling-transform-fail", ("target", selectedHumanoidData.Name));
        // _popup.PopupEntity(selfMessage, uid, uid);
        // var selfMessage = Loc.GetString("changeling-transform-activate", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
        // _popup.PopupEntity(selfMessage, transformedUid.Value, transformedUid.Value);
    }
}