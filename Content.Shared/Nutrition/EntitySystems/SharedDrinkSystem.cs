using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Nutrition.EntitySystems;

[Obsolete("Migration to Content.Shared.Nutrition.EntitySystems.IngestionSystem is required")]
public abstract partial class SharedDrinkSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUseDrinkInHand, after: new[] { typeof(OpenableSystem), typeof(InventorySystem) });
        SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(OnUseDrink);

        SubscribeLocalEvent<DrinkComponent, AttemptShakeEvent>(OnAttemptShake);

        SubscribeLocalEvent<DrinkComponent, GetVerbsEvent<AlternativeVerb>>(AddDrinkVerb);

        SubscribeLocalEvent<DrinkComponent, BeforeIngestedEvent>(OnBeforeDrinkEaten);
        SubscribeLocalEvent<DrinkComponent, IngestedEvent>(OnDrinkEaten);

        SubscribeLocalEvent<DrinkComponent, EdibleEvent>(OnDrink);

        SubscribeLocalEvent<DrinkComponent, IsDigestibleEvent>(OnIsDigestible);

        SubscribeLocalEvent<DrinkComponent, GetEdibleTypeEvent>(OnGetEdibleType);
    }

    protected void OnAttemptShake(Entity<DrinkComponent> entity, ref AttemptShakeEvent args)
    {
        if (IsEmpty(entity, entity.Comp))
            args.Cancelled = true;
    }

    protected FixedPoint2 DrinkVolume(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out _, out var sol))
            return FixedPoint2.Zero;

        return sol.Volume;
    }

    protected bool IsEmpty(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        return DrinkVolume(uid, component) <= 0;
    }

    /// <summary>
    /// Eat or drink an item
    /// </summary>
    private void OnUseDrinkInHand(Entity<DrinkComponent> entity, ref UseInHandEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = _ingestion.TryIngest(ev.User, ev.User, entity);
    }

    /// <summary>
    /// Feed someone else
    /// </summary>
    private void OnUseDrink(Entity<DrinkComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = _ingestion.TryIngest(args.User, args.Target.Value, entity);
    }

    private void AddDrinkVerb(Entity<DrinkComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (entity.Owner == user || !args.CanInteract || !args.CanAccess)
            return;

        if (!_ingestion.TryGetIngestionVerb(user, entity, IngestionSystem.Drink, out var verb))
            return;

        args.Verbs.Add(verb);
    }

    private void OnBeforeDrinkEaten(Entity<DrinkComponent> food, ref BeforeIngestedEvent args)
    {
        if (args.Cancelled)
            return;

        // Set it to transfer amount if it exists, otherwise eat the whole volume if possible.
        args.Transfer = food.Comp.TransferAmount;
    }

    private void OnDrinkEaten(Entity<DrinkComponent> entity, ref IngestedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPredicted(entity.Comp.UseSound, args.Target, args.User, AudioParams.Default.WithVolume(-2f).WithVariation(0.25f));

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(entity.Owner, args.Target, args.Split);

        if (args.ForceFed)
        {
            var targetName = Identity.Entity(args.Target, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);

            _popup.PopupEntity(Loc.GetString("edible-force-feed-success", ("user", userName), ("verb", _ingestion.GetProtoVerb(IngestionSystem.Drink)), ("flavors", flavors)), entity, entity);

            _popup.PopupClient(Loc.GetString("edible-force-feed-success-user", ("target", targetName), ("verb", _ingestion.GetProtoVerb(IngestionSystem.Drink))), args.User, args.User);

            // log successful forced drinking
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity.Owner):user} forced {ToPrettyString(args.User):target} to drink {ToPrettyString(entity.Owner):drink}");
        }
        else
        {
            _popup.PopupPredicted(Loc.GetString("edible-slurp", ("flavors", flavors)),
                Loc.GetString("edible-slurp-other"),
                args.User,
                args.User);

            // log successful voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} drank {ToPrettyString(entity.Owner):drink}");
        }

        if (_ingestion.GetUsesRemaining(entity, entity.Comp.Solution, args.Split.Volume) <= 0)
            return;

        // Leave some of the consumer's DNA on the consumed item...
        var ev = new TransferDnaEvent
        {
            Donor = args.Target,
            Recipient = entity,
            CanDnaBeCleaned = false,
        };
        RaiseLocalEvent(args.Target, ref ev);

        args.Repeat = !args.ForceFed;
    }

    private void OnDrink(Entity<DrinkComponent> drink, ref EdibleEvent args)
    {
        if (args.Cancelled || args.Solution != null)
            return;

        if (!_solutionContainer.TryGetSolution(drink.Owner, drink.Comp.Solution, out args.Solution) || IsEmpty(drink))
        {
            args.Cancelled = true;

            _popup.PopupClient(Loc.GetString("ingestion-try-use-is-empty", ("entity", drink)), drink, args.User);
            return;
        }

        args.Time += TimeSpan.FromSeconds(drink.Comp.Delay);
    }

    private void OnIsDigestible(Entity<DrinkComponent> ent, ref IsDigestibleEvent args)
    {
        // Anyone can drink from puddles on the floor!
        args.UniversalDigestion();
    }

    private void OnGetEdibleType(Entity<DrinkComponent> ent, ref GetEdibleTypeEvent args)
    {
        if (args.Type != null)
            return;

        args.SetPrototype(IngestionSystem.Drink);
    }
}
