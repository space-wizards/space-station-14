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
using Content.Server.Zombies;
using Robust.Server.GameObjects;
using Content.Shared.Mind;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
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

        InitializeLingAbilities();
    }

    private void OnStartup(Entity<ChangelingComponent> ent, ref ComponentStartup args)
    {
        _uplink.AddUplink(ent, FixedPoint2.New(10), ChangelingShopPresetPrototype, ent, EvolutionPointsCurrencyPrototype); // not really an 'uplink', but it's there to add the evolution menu

        RemComp<HungerComponent>(ent);
        RemComp<ThirstComponent>(ent); // changelings dont get hungry or thirsty
        EnsureComp<ZombieImmuneComponent>(ent); // no zombie lings

        StealDNA(ent, ent, ent.Comp);
    }

    private void OnMapInit(Entity<ChangelingComponent> ent, ref MapInitEvent args)
    {
        // the actions are hooked to the mind because transformation uses polymorph
        if (_mind.TryGetMind(ent, out var mind, out _))
        {
            _action.AddAction(mind, ChangelingEvolutionMenuId);
            _action.AddAction(mind, ChangelingRegenActionId);
            _action.AddAction(mind, ChangelingAbsorbActionId);
            _action.AddAction(mind, ChangelingDNACycleActionId);
            _action.AddAction(mind, ChangelingTransformActionId);
        }
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
        Dirty(uid, component);

        if (regenCap)
            float.Min(component.Chemicals, component.MaxChemicals);

        _alerts.ShowAlert(uid, AlertType.Chemicals);

        return true;
    }

    private bool TryUseAbility(EntityUid uid, ChangelingComponent component, int abilityCost)
    {
        if (component.Chemicals <= Math.Abs(abilityCost))
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), uid, uid);
            return false;
        }

        ChangeChemicalsAmount(uid, component, abilityCost);

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

    private void OnShop(Entity<ChangelingComponent> ent, ref ChangelingEvolutionMenuActionEvent args)
    {
        _store.OnInternalShop(ent);
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
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidAppearanceComp))
            return false;
        if (!TryComp<MetaDataComponent>(target, out var metaDataComp))
            return false;
        if (!TryComp<DnaComponent>(target, out var dnaComp))
            return false;
        if (!TryComp<FingerprintComponent>(target, out var fingerPrintComp))
            return false;

        var transformData = new TransformData
        {
            Name = metaDataComp.EntityName,
            Dna = dnaComp.DNA,
            HumanoidAppearanceComp = humanoidAppearanceComp
        };

        if (fingerPrintComp.Fingerprint != null)
            transformData.Fingerprint = fingerPrintComp.Fingerprint;

        component.StoredDNA.Add(transformData);

        return true;
    }

    public void OnCycleDNA(Entity<ChangelingComponent> ent, ref ChangelingCycleDNAActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.SelectedDNA += 1;

        if (ent.Comp.StoredDNA.Count >= ent.Comp.DNAStrandCap || ent.Comp.SelectedDNA >= ent.Comp.StoredDNA.Count)
            ent.Comp.SelectedDNA = 0;

        var selectedTransformData = ent.Comp.StoredDNA[ent.Comp.SelectedDNA];

        var selfMessage = Loc.GetString("changeling-dna-switchdna", ("target", selectedTransformData.Name));
        _popup.PopupEntity(selfMessage, ent, ent);
    }
}