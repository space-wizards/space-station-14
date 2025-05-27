using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Lathe;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Systems;

public sealed class BlueprintSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BlueprintReceiverComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlueprintReceiverComponent, AfterInteractUsingEvent>(OnAfterInteract);
        SubscribeLocalEvent<BlueprintReceiverComponent, LatheGetRecipesEvent>(OnGetRecipes);
    }

    private void OnStartup(Entity<BlueprintReceiverComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnAfterInteract(Entity<BlueprintReceiverComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !TryComp<BlueprintComponent>(args.Used, out var blueprintComponent))
            return;
        args.Handled = TryInsertBlueprint(ent, (args.Used, blueprintComponent), args.User);
    }

    private void OnGetRecipes(Entity<BlueprintReceiverComponent> ent, ref LatheGetRecipesEvent args)
    {
        var recipes = GetBlueprintRecipes(ent);
        foreach (var recipe in recipes)
        {
            args.Recipes.Add(recipe);
        }
    }

    public bool TryInsertBlueprint(Entity<BlueprintReceiverComponent> ent, Entity<BlueprintComponent> blueprint, EntityUid? user)
    {
        if (!CanInsertBlueprint(ent, blueprint, user))
            return false;

        if (user is not null)
        {
            var userId = Identity.Entity(user.Value, EntityManager);
            var bpId = Identity.Entity(blueprint, EntityManager);
            var machineId = Identity.Entity(ent, EntityManager);
            var msg = Loc.GetString("blueprint-receiver-popup-insert",
                ("user", userId),
                ("blueprint", bpId),
                ("receiver", machineId));
            _popup.PopupPredicted(msg, ent, user);
        }

        _container.Insert(blueprint.Owner, _container.GetContainer(ent, ent.Comp.ContainerId));

        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(ent, ref ev);
        return true;
    }

    public bool CanInsertBlueprint(Entity<BlueprintReceiverComponent> ent, Entity<BlueprintComponent> blueprint, EntityUid? user)
    {
        if (_entityWhitelist.IsWhitelistFail(ent.Comp.Whitelist, blueprint))
        {
            return false;
        }

        if (blueprint.Comp.ProvidedRecipes.Count == 0)
        {
            Log.Error($"Attempted to insert blueprint {ToPrettyString(blueprint)} with no recipes.");
            return false;
        }

        // Don't add new blueprints if there are no new recipes.
        var currentRecipes = GetBlueprintRecipes(ent);
        if (currentRecipes.Count != 0 && currentRecipes.IsSupersetOf(blueprint.Comp.ProvidedRecipes))
        {
            _popup.PopupPredicted(Loc.GetString("blueprint-receiver-popup-recipe-exists"), ent, user);
            return false;
        }

        return _container.CanInsert(blueprint, _container.GetContainer(ent, ent.Comp.ContainerId));
    }

    public HashSet<ProtoId<LatheRecipePrototype>> GetBlueprintRecipes(Entity<BlueprintReceiverComponent> ent)
    {
        var contained = _container.GetContainer(ent, ent.Comp.ContainerId);

        var recipes = new HashSet<ProtoId<LatheRecipePrototype>>();
        foreach (var blueprint in contained.ContainedEntities)
        {
            if (!TryComp<BlueprintComponent>(blueprint, out var blueprintComponent))
                continue;

            foreach (var provided in blueprintComponent.ProvidedRecipes)
            {
                recipes.Add(provided);
            }
        }

        return recipes;
    }
}
