using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Server.Wires;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Materials;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Materials;

/// <inheritdoc/>
public sealed class MaterialReclaimerSystem : SharedMaterialReclaimerSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedBodySystem _body = default!; //bobby
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialReclaimerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MaterialReclaimerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<MaterialReclaimerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<MaterialReclaimerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MaterialReclaimerComponent, InteractUsingEvent>(OnInteractUsing,
            before: new []{typeof(WiresSystem), typeof(SolutionTransferSystem)});
        SubscribeLocalEvent<MaterialReclaimerComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<ActiveMaterialReclaimerComponent, PowerChangedEvent>(OnActivePowerChanged);
    }
    private void OnStartup(EntityUid uid, MaterialReclaimerComponent component, ComponentStartup args)
    {
        component.OutputSolution = _solutionContainer.EnsureSolution(uid, component.SolutionContainerId);
    }

    private void OnUpgradeExamine(EntityUid uid, MaterialReclaimerComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade(Loc.GetString("material-reclaimer-upgrade-process-rate"), component.MaterialProcessRate / component.BaseMaterialProcessRate);
    }

    private void OnRefreshParts(EntityUid uid, MaterialReclaimerComponent component, RefreshPartsEvent args)
    {
        var rating = args.PartRatings[component.MachinePartProcessRate] - 1;
        component.MaterialProcessRate = component.BaseMaterialProcessRate * MathF.Pow(component.PartRatingProcessRateMultiplier, rating);
        Dirty(component);
    }

    private void OnPowerChanged(EntityUid uid, MaterialReclaimerComponent component, ref PowerChangedEvent args)
    {
        AmbientSound.SetAmbience(uid, component.Enabled && args.Powered);
        component.Powered = args.Powered;
        Dirty(component);
    }

    private void OnInteractUsing(EntityUid uid, MaterialReclaimerComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // if we're trying to get a solution out of the reclaimer, don't destroy it
        if (component.OutputSolution.Contents.Any())
        {
            if (TryComp<SolutionContainerManagerComponent>(args.Used, out var managerComponent) &&
                managerComponent.Solutions.Any(s => s.Value.AvailableVolume > 0))
            {
                if (_openable.IsClosed(args.Used))
                    return;

                if (TryComp<SolutionTransferComponent>(args.Used, out var transfer) &&
                    transfer.CanReceive)
                    return;
            }
        }

        args.Handled = TryStartProcessItem(uid, args.Used, component, args.User);
    }

    private void OnSuicide(EntityUid uid, MaterialReclaimerComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        args.SetHandled(SuicideKind.Bloodloss);
        var victim = args.Victim;
        if (TryComp(victim, out ActorComponent? actor) &&
            _mind.TryGetMind(actor.PlayerSession, out var mindId, out var mind))
        {
            _ticker.OnGhostAttempt(mindId, false, mind: mind);
            if (mind.OwnedEntity is { Valid: true } entity)
            {
                _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message"), entity);
            }
        }

        _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message-others", ("victim", Identity.Entity(victim, EntityManager))),
            victim,
            Filter.PvsExcept(victim, entityManager: EntityManager), true);

        _body.GibBody(victim, true);
        _appearance.SetData(uid, RecyclerVisuals.Bloody, true);
    }

    private void OnActivePowerChanged(EntityUid uid, ActiveMaterialReclaimerComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            TryFinishProcessItem(uid, null, component);
    }

    /// <inheritdoc/>
    public override bool TryFinishProcessItem(EntityUid uid, MaterialReclaimerComponent? component = null, ActiveMaterialReclaimerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return false;

        if (!base.TryFinishProcessItem(uid, component, active))
            return false;

        if (active.ReclaimingContainer.ContainedEntities.FirstOrNull() is not { } item)
            return false;

        Container.Remove(item, active.ReclaimingContainer);
        Dirty(component);

        // scales the output if the process was interrupted.
        var completion = 1f - Math.Clamp((float) Math.Round((active.EndTime - Timing.CurTime) / active.Duration),
            0f, 1f);
        Reclaim(uid, item, completion, component);

        return true;
    }

    /// <inheritdoc/>
    public override void Reclaim(EntityUid uid,
        EntityUid item,
        float completion = 1f,
        MaterialReclaimerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.Reclaim(uid, item, completion, component);

        var xform = Transform(uid);

        SpawnMaterialsFromComposition(uid, item, completion * component.Efficiency, xform: xform);

        if (CanGib(uid, item, component))
        {
            SpawnChemicalsFromComposition(uid, item, completion, false, component, xform);
            _body.GibBody(item, true);
            _appearance.SetData(uid, RecyclerVisuals.Bloody, true);
        }
        else
        {
            SpawnChemicalsFromComposition(uid, item, completion, true, component, xform);
        }

        QueueDel(item);
    }

    private void SpawnMaterialsFromComposition(EntityUid reclaimer,
        EntityUid item,
        float efficiency,
        MaterialStorageComponent? storage = null,
        TransformComponent? xform = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!Resolve(reclaimer, ref storage, ref xform, false))
            return;

        if (!Resolve(item, ref composition, false))
            return;

        foreach (var (material, amount) in composition.MaterialComposition)
        {
            var outputAmount = (int) (amount * efficiency);
            _materialStorage.TryChangeMaterialAmount(reclaimer, material, outputAmount, storage);
        }

        foreach (var (storedMaterial, storedAmount) in storage.Storage)
        {
            var stacks = _materialStorage.SpawnMultipleFromMaterial(storedAmount, storedMaterial,
                xform.Coordinates,
                out var materialOverflow);
            var amountConsumed = storedAmount - materialOverflow;
            _materialStorage.TryChangeMaterialAmount(reclaimer, storedMaterial, -amountConsumed, storage);
            foreach (var stack in stacks)
            {
                _stack.TryMergeToContacts(stack);
            }
        }
    }

    private void SpawnChemicalsFromComposition(EntityUid reclaimer,
        EntityUid item,
        float efficiency,
        bool sound = true,
        MaterialReclaimerComponent? reclaimerComponent = null,
        TransformComponent? xform = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!Resolve(reclaimer, ref reclaimerComponent, ref xform))
            return;

        efficiency *= reclaimerComponent.Efficiency;

        var totalChemicals = new Solution();

        if (Resolve(item, ref composition, false))
        {
            foreach (var (key, value) in composition.ChemicalComposition)
            {
                // TODO use ReagentQuantity
                totalChemicals.AddReagent(key, value * efficiency, false);
            }
        }

        // if the item we inserted has reagents, add it in.
        if (TryComp<SolutionContainerManagerComponent>(item, out var solutionContainer))
        {
            foreach (var solution in solutionContainer.Solutions.Values)
            {
                foreach (var quantity in solution.Contents)
                {
                    totalChemicals.AddReagent(quantity.Reagent.Prototype, quantity.Quantity * efficiency, false);
                }
            }
        }

        _solutionContainer.TryTransferSolution(reclaimer, reclaimerComponent.OutputSolution, totalChemicals, totalChemicals.Volume);
        if (totalChemicals.Volume > 0)
        {
            _puddle.TrySpillAt(reclaimer, totalChemicals, out _, sound, transformComponent: xform);
        }
    }
}
