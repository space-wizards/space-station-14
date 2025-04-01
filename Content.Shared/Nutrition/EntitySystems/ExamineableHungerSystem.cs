using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <inheritdoc cref="ExamineableHungerComponent"/>
public sealed class ExamineableHungerSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    private EntityQuery<HungerComponent> _hungerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _hungerQuery = GetEntityQuery<HungerComponent>();

        SubscribeLocalEvent<ExamineableHungerComponent, ExaminedEvent>(OnExamine);
    }

    /// <summary>
    ///     Defines the text provided on examine.
    ///     Changes depending on the amount of hunger the target has.
    /// </summary>
    private void OnExamine(Entity<ExamineableHungerComponent> entity, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(entity, EntityManager);

        if (!_hungerQuery.TryComp(entity, out var hungerComp)
            || !entity.Comp.Descriptions.TryGetValue(_hunger.GetHungerThreshold(hungerComp), out var locId))
        {
            // Use a fallback message if the entity has no HungerComponent
            // or is missing a description for the current threshold
            locId = entity.Comp.NoHungerDescription;
        }

        var msg = Loc.GetString(locId, ("entity", identity));
        args.PushMarkup(msg);
    }
}
