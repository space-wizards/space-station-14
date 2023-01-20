using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Lathe.Components;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.UserInterface;
using Content.Shared.Database;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed class LatheSystem : SharedLatheSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
        [Dependency] private readonly StackSystem _stack = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
            SubscribeLocalEvent<LatheComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LatheComponent, RefreshPartsEvent>(OnPartsRefresh);
            SubscribeLocalEvent<LatheComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<LatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);

            SubscribeLocalEvent<LatheComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
            SubscribeLocalEvent<LatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);

            SubscribeLocalEvent<LatheComponent, BeforeActivatableUIOpenEvent>((u,c,_) => UpdateUserInterfaceState(u,c));
            SubscribeLocalEvent<LatheComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);

            SubscribeLocalEvent<TechnologyDatabaseComponent, LatheGetRecipesEvent>(OnGetRecipes);
        }

        public override void Update(float frameTime)
        {
            foreach (var (comp, lathe) in EntityQuery<LatheProducingComponent, LatheComponent>())
            {
                if (lathe.CurrentRecipe == null)
                    continue;

                if ( _timing.CurTime - comp.StartTime >= comp.ProductionLength)
                    FinishProducing(comp.Owner, lathe);
            }
        }

        private void OnGetWhitelist(EntityUid uid, LatheComponent component, ref GetMaterialWhitelistEvent args)
        {
            if (args.Storage != uid)
                return;
            var materialWhitelist = new List<string>();
            var recipes =  GetAllBaseRecipes(component);
            foreach (var id in recipes)
            {
                if (!_proto.TryIndex<LatheRecipePrototype>(id, out var proto))
                    continue;
                foreach (var (mat, _) in proto.RequiredMaterials)
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

        [PublicAPI]
        public bool TryGetAvailableRecipes(EntityUid uid, [NotNullWhen(true)] out List<string>? recipes, [NotNullWhen(true)] LatheComponent? component = null)
        {
            recipes = null;
            if (!Resolve(uid, ref component))
                return false;
            recipes = GetAvailableRecipes(uid, component);
            return true;
        }

        public List<string> GetAvailableRecipes(EntityUid uid, LatheComponent component)
        {
            var ev = new LatheGetRecipesEvent(uid)
            {
                Recipes = component.StaticRecipes
            };
            RaiseLocalEvent(uid, ev);
            return ev.Recipes;
        }

        public List<string> GetAllBaseRecipes(LatheComponent component)
        {
            return component.DynamicRecipes == null
                ? component.StaticRecipes
                : component.StaticRecipes.Union(component.DynamicRecipes).ToList();
        }

        public bool TryAddToQueue(EntityUid uid, LatheRecipePrototype recipe, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!CanProduce(uid, recipe, 1, component))
                return false;

            foreach (var (mat, amount) in recipe.RequiredMaterials)
            {
                var adjustedAmount = recipe.ApplyMaterialDiscount
                    ? (int) (-amount * component.MaterialUseMultiplier)
                    : -amount;

                _materialStorage.TryChangeMaterialAmount(uid, mat, adjustedAmount);
            }
            component.Queue.Add(recipe);

            return true;
        }

        public bool TryStartProducing(EntityUid uid, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (component.CurrentRecipe != null || component.Queue.Count <= 0 || !this.IsPowered(uid, EntityManager))
                return false;

            var recipe = component.Queue.First();
            component.Queue.RemoveAt(0);

            var lathe = EnsureComp<LatheProducingComponent>(uid);
            lathe.StartTime = _timing.CurTime;
            lathe.ProductionLength = recipe.CompleteTime * component.TimeMultiplier;
            component.CurrentRecipe = recipe;

            _audio.PlayPvs(component.ProducingSound, uid);
            UpdateRunningAppearance(uid, true);
            UpdateUserInterfaceState(uid, component);
            return true;
        }

        public void FinishProducing(EntityUid uid, LatheComponent? comp = null, LatheProducingComponent? prodComp = null)
        {
            if (!Resolve(uid, ref comp, ref prodComp, false))
                return;

            if (comp.CurrentRecipe != null)
            {
                var result = Spawn(comp.CurrentRecipe.Result, Transform(uid).Coordinates);
                _stack.TryMergeToContacts(result);
            }

            comp.CurrentRecipe = null;
            prodComp.StartTime = _timing.CurTime;

            if (!TryStartProducing(uid, comp))
            {
                RemCompDeferred(uid, prodComp);
                UpdateUserInterfaceState(uid, comp);
                UpdateRunningAppearance(uid, false);
            }
        }

        public void UpdateUserInterfaceState(EntityUid uid, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = _uiSys.GetUi(uid, LatheUiKey.Key);
            var producing = component.CurrentRecipe ?? component.Queue.FirstOrDefault();

            var state = new LatheUpdateState(GetAvailableRecipes(uid, component), component.Queue, producing);
            _uiSys.SetUiState(ui, state);
        }

        private void OnGetRecipes(EntityUid uid, TechnologyDatabaseComponent component, LatheGetRecipesEvent args)
        {
            if (uid != args.Lathe || !TryComp<LatheComponent>(uid, out var latheComponent) || latheComponent.DynamicRecipes == null)
                return;

            args.Recipes = args.Recipes.Union(component.RecipeIds.Where(r => latheComponent.DynamicRecipes.Contains(r))).ToList();
        }

        private void OnMaterialAmountChanged(EntityUid uid, LatheComponent component, ref MaterialAmountChangedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        /// <summary>
        /// Initialize the UI and appearance.
        /// Appearance requires initialization or the layers break
        /// </summary>
        private void OnMapInit(EntityUid uid, LatheComponent component, MapInitEvent args)
        {
            _appearance.SetData(uid, LatheVisuals.IsInserting, false);
            _appearance.SetData(uid, LatheVisuals.IsRunning, false);

            _materialStorage.UpdateMaterialWhitelist(uid);
        }

        /// <summary>
        /// Sets the machine sprite to either play the running animation
        /// or stop.
        /// </summary>
        private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
        {
            _appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
        }

        private void OnPowerChanged(EntityUid uid, LatheComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                RemComp<LatheProducingComponent>(uid);
                UpdateRunningAppearance(uid, false);
            }
            else if (component.CurrentRecipe != null)
            {
                EnsureComp<LatheProducingComponent>(uid);
                TryStartProducing(uid, component);
            }
        }

        private void OnPartsRefresh(EntityUid uid, LatheComponent component, RefreshPartsEvent args)
        {
            var printTimeRating = args.PartRatings[component.MachinePartPrintTime];
            var materialUseRating = args.PartRatings[component.MachinePartMaterialUse];

            component.TimeMultiplier = MathF.Pow(component.PartRatingPrintTimeMultiplier, printTimeRating - 1);
            component.MaterialUseMultiplier = MathF.Pow(component.PartRatingMaterialUseMultiplier, materialUseRating - 1);
            Dirty(component);
        }

        private void OnUpgradeExamine(EntityUid uid, LatheComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("lathe-component-upgrade-speed", 1 / component.TimeMultiplier);
            args.AddPercentageUpgrade("lathe-component-upgrade-material-use", component.MaterialUseMultiplier);
        }

        private void OnDatabaseModified(EntityUid uid, LatheComponent component, ref TechnologyDatabaseModifiedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        protected override bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, LatheComponent component)
        {
            return GetAvailableRecipes(uid, component).Contains(recipe.ID);
        }

        #region UI Messages

        private void OnLatheQueueRecipeMessage(EntityUid uid, LatheComponent component, LatheQueueRecipeMessage args)
        {
            if (_proto.TryIndex(args.ID, out LatheRecipePrototype? recipe))
            {
                var count = 0;
                for (var i = 0; i < args.Quantity; i++)
                {
                    if (TryAddToQueue(uid, recipe, component))
                        count++;
                }
                if (count > 0 && args.Session.AttachedEntity != null)
                {
                    _adminLogger.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(args.Session.AttachedEntity.Value):player} queued {count} {recipe.Name} at {ToPrettyString(uid):lathe}");
                }
            }
            TryStartProducing(uid, component);
            UpdateUserInterfaceState(uid, component);
        }

        private void OnLatheSyncRequestMessage(EntityUid uid, LatheComponent component, LatheSyncRequestMessage args)
        {
            UpdateUserInterfaceState(uid, component);
        }
        #endregion
    }
}
