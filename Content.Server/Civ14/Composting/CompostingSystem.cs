using Content.Shared.Composting;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Destructible;

namespace Content.Server.Composting;

public sealed partial class CompostingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CompostingComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CompostingComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<CompostingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CompostingComponent, DestructionEventArgs>(OnDestroyed);
    }

    /// <summary>
    /// Handles inserting a compostable item into the composter.
    /// </summary>
    private void OnInteractUsing(EntityUid uid, CompostingComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var item = args.Used;
        if (!IsCompostable(item, component))
        {
            _popup.PopupEntity("This item cannot be composted.", uid, args.User);
            return;
        }

        // Calculates actual load inside (composting + ready compost)
        var currentLoad = component.CompostingItems.Count + component.ReadyCompost;
        if (currentLoad >= component.MaxCapacity)
        {
            _popup.PopupEntity("It wont fit.", uid, args.User);
            return;
        }

        // Add item to composting process and delete it from the world
        component.CompostingItems[item] = _gameTiming.CurTime + TimeSpan.FromMinutes(component.CompostTime);
        QueueDel(item);
        _popup.PopupEntity("You add the item to compost.", uid, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Checks if an item is compostable based on its tags.
    /// </summary>
    private bool IsCompostable(EntityUid item, CompostingComponent component)
    {
        var tagComponent = Comp<TagComponent>(item);
        var tags = tagComponent?.Tags.Select(tag => tag.Id).ToArray() ?? Array.Empty<string>();
        return component.Whitelist.Any(tag => tags.Contains(tag));
    }

    /// <summary>
    /// Handles collecting finished compost with an empty hand.
    /// </summary>
    private void OnInteractHand(EntityUid uid, CompostingComponent component, InteractHandEvent args)
    {
        if (args.Handled || component.ReadyCompost <= 0)
            return;

        // Spawn compost and try to place it in the player's hand
        var compost = Spawn("Compost", Transform(uid).MapPosition);
        if (_hands.TryPickupAnyHand(args.User, compost))
        {
            component.ReadyCompost--;
            _popup.PopupEntity("You collect a unit of compost.", uid, args.User);
        }
        else
        {
            _popup.PopupEntity("Your hands are full.", uid, args.User);
        }
        args.Handled = true;
    }

    /// <summary>
    /// Updates composting progress and converts finished items to ready compost.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CompostingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var currentTime = _gameTiming.CurTime;
            var toRemove = new List<EntityUid>();

            foreach (var (item, endTime) in component.CompostingItems)
            {
                if (currentTime >= endTime)
                {
                    toRemove.Add(item);
                    component.ReadyCompost++;
                }
            }

            foreach (var item in toRemove)
            {
                component.CompostingItems.Remove(item);
            }
        }
    }

    /// <summary>
    /// Displays the amount of ready compost when examining the composter.
    /// </summary>
    /// <summary>
    /// Displays the status of the compost bin when examined.
    /// </summary>
    private void OnExamined(EntityUid uid, CompostingComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var compostingCount = component.CompostingItems.Count;
        var readyCompost = component.ReadyCompost;

        if (compostingCount == 0 && readyCompost == 0)
        {
            args.PushMarkup("It's empty.");
            return;
        }

        if (compostingCount > 0)
        {
            args.PushMarkup($"Its currently composting.");
        }

        if (readyCompost > 0)
        {
            args.PushMarkup($"There are {readyCompost} units of compost ready.");
        }
    }

    private void OnDestroyed(EntityUid uid, CompostingComponent component, DestructionEventArgs args)
    {
        // Drops all ready compost
        for (int i = 0; i < component.ReadyCompost; i++)
        {
            Spawn("Compost", Transform(uid).MapPosition);
        }
    }
}