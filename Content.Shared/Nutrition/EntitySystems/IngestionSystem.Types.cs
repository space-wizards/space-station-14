using System.Diagnostics.CodeAnalysis;
using Content.Shared.Nutrition.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This handles the different types of edibles there are, including methods for
/// getting Verbs, nouns, verbs, adjectives, really anything. If you add a new food type
/// you need to put it here.
/// </summary>
public sealed partial class IngestionSystem
{
    public void InitializeTypes()
    {
        SubscribeLocalEvent<EdibleComponent, GetVerbsEvent<AlternativeVerb>>(AddIngestionVerbs);
    }

    private void AddIngestionVerbs(Entity<EdibleComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (entity.Owner == user || !args.CanInteract || !args.CanAccess)
            return;

        // TODO: This might not need to be a bool or even return anything???
        if (!TryGetIngestionVerb(user, entity, entity.Comp.Type, out var verb))
            return;

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Tries to get the ingestion verbs for a given user entity and ingestible entity
    /// </summary>
    /// <param name="user">The one getting the verbs who would be doing the eating.</param>
    /// <param name="ingested"></param>
    /// <param name="type"></param>
    /// <param name="verb"></param>
    /// <returns></returns>
    private bool TryGetIngestionVerb(EntityUid user, EntityUid ingested, NutritionType type, [NotNullWhen(true)] out AlternativeVerb? verb)
    {
        verb = null;

        // We want to see if we can ingest this item, but we don't actually want to ingest it.
        if (!TryIngest(user, user, ingested, false))
            return false;

        SpriteSpecifier icon;
        string text;

        switch (type)
        {
            case NutritionType.Food:
                icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png"));
                text = Loc.GetString("food-system-verb-eat");
                break;
            case NutritionType.Drink:
                icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/drink.svg.192dpi.png"));
                text = Loc.GetString("drink-system-verb-drink");
                break;
            default:
                Log.Error($"Entity {ToPrettyString(ingested)} doesn't have a proper Nutrition type or its verb isn't properly set up.");
                return false;
        }

        verb = new()
        {
            Act = () =>
            {
                TryIngest(user, user, ingested);
            },
            Icon = icon,
            Text = text,
            Priority = -1
        };

        return true;
    }

    private string GetStringNoun(EdibleComponent component)
    {
        switch (component.Type)
        {
            case NutritionType.Food:
                return Loc.GetString("food");
            case NutritionType.Drink:
                return Loc.GetString("drink");
            default:
                Log.Error($"EdibleType {component.Type} doesn't have a proper noun associated with it.");
                return Loc.GetString("edible");
        }
    }

    private string GetStringVerb(EdibleComponent component)
    {
        switch (component.Type)
        {
            case NutritionType.Food:
                return Loc.GetString("eat");
            case NutritionType.Drink:
                return Loc.GetString("drink");
            default:
                Log.Error($"EdibleType {component.Type} doesn't have a proper verb associated with it.");
                return Loc.GetString("edible");
        }
    }
}
