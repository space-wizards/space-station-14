using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Lathe.Components;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.ReagentSpeed;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Lathe;

/// <summary>
/// This handles all lathe entities which are entities that take in materials and spit out entities with a generic UI.
/// Are you a material?
/// </summary>
public abstract partial class LatheSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private ReagentSpeedSystem _reagentSpeed = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] protected SharedUserInterfaceSystem UISys = default!;

    [Dependency] protected EntityQuery<LatheComponent> LatheQuery = default!;
    [Dependency] protected EntityQuery<LatheProducingComponent> ProducingQuery = default!;

    public const int MaxItemsPerRequest = 10_000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUI((u, c)));
        BuildInverseRecipeDictionary();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LatheProducingComponent, LatheComponent>();
        while (query.MoveNext(out var uid, out var comp, out var lathe))
        {
            if (comp.CompletionTime <= Timing.CurTime)
                FinishProducing((uid, lathe));
        }
    }

    [SubscribeLocalEvent]
    private void OnGetState(Entity<LatheComponent> lathe, ref ComponentGetState args)
    {
        if (args.FromTick > lathe.Comp.CreationTick)
        {
            var aspects = EntityManager.GetModifiedAspects(lathe.Comp, args.FromTick);

            if (aspects > 0 && aspects < DeltaAspect.Unclassified)
            {
                var deltaState = new LatheComponentDeltaState
                {
                    ChangedFields = aspects,
                };

                if ((aspects & (1UL << LatheComponentDeltaState.QueueIndex)) != 0)
                    deltaState.Queue = lathe.Comp.Queue;

                if ((aspects & (1UL << LatheComponentDeltaState.CurrentRecipeIndex)) != 0)
                    deltaState.CurrentRecipe = lathe.Comp.CurrentRecipe;

                if ((aspects & (1UL << LatheComponentDeltaState.RecipesIndex)) != 0)
                    deltaState.Recipes = lathe.Comp.Recipes;

                args.State = deltaState;
                return;
            }
        }

        args.State = new LatheComponentState
        {
            Queue = lathe.Comp.Queue,
            CurrentRecipe = lathe.Comp.CurrentRecipe,
            Recipes = lathe.Comp.Recipes
        };
    }

    /// <summary>
    /// Initialize the UI and appearance.
    /// Appearance requires initialization or the layers break
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<LatheComponent> entity, ref ComponentInit args)
    {
        _appearance.SetData(entity, LatheVisuals.IsInserting, false);
        _appearance.SetData(entity, LatheVisuals.IsRunning, false);

        _materialStorage.UpdateMaterialWhitelist(entity);
        UpdateRecipies(entity);
    }

    [SubscribeLocalEvent]
    private void OnProductionStartup(Entity<LatheProducingComponent> ent, ref MapInitEvent args)
    {
        _powerState.TrySetWorkingState(ent.Owner, true);
        UpdateRunningAppearance(ent, true);

        if (!LatheQuery.TryComp(ent, out var lathe))
            return;

        UpdateUI((ent, lathe));
    }

    [SubscribeLocalEvent]
    private void OnProductionShutdown(Entity<LatheProducingComponent> ent, ref ComponentShutdown args)
    {
        // use the Try variant of this here
        // or else you get trolled by AllComponentsOneToOneDeleteTest
        _powerState.TrySetWorkingState(ent.Owner, false);
        UpdateRunningAppearance(ent, false);

        if (!LatheQuery.TryComp(ent, out var lathe))
            return;

        lathe.CurrentRecipe = null;
        UpdateUI((ent, lathe));
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<LatheComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.ReagentOutputSlotId != null)
            args.PushMarkup(Loc.GetString("lathe-menu-reagent-slot-examine"));
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<LatheComponent> entity, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            AbortProduction(entity.AsNullable());
        else
            TryStartProducing(entity.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnGetWhitelist(Entity<LatheComponent> entity, ref GetMaterialWhitelistEvent args)
    {
        if (args.Storage != entity.Owner)
            return;
        var materialWhitelist = new List<ProtoId<MaterialPrototype>>();
        var recipes = GetAvailableRecipes(entity, true);
        foreach (var id in recipes)
        {
            if (!ProtoMan.Resolve(id, out var proto))
                continue;
            foreach (var (mat, _) in proto.Materials)
            {
                if (!materialWhitelist.Contains(mat))
                {
                    materialWhitelist.Add(mat);
                }
            }
        }

        var combined = args.Whitelist.Union(materialWhitelist).ToList();
        args.Whitelist = combined;
    }

    /// <summary>
    /// Sets the machine sprite to either play the running animation
    /// or stop.
    /// </summary>
    private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
    {
        _appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
    }

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int)MathF.Ceiling(original * multiplier) : original;
}
