using Content.Server.Administration.Logs;
using Content.Server.Interaction.Components;
using Content.Server.Mind.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
/// This handles logic and interactions related to <see cref="BreedableComponent"/>
/// </summary>
/// <remarks>
/// Yes, I partially wrote this solely to swipe the name BreedableComponent for the memes.
/// For all of our sakes, I'll refrain from literring this code with jokes about it.
/// - emo
/// </remarks>
public sealed class AnimalHusbandrySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly HashSet<EntityUid> _failedAttempts = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BreedableComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<BreedableComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<InfantComponent, EntityUnpausedEvent>(OnInfantUnpaused);
        SubscribeLocalEvent<InfantComponent, ComponentStartup>(OnInfantStartup);
        SubscribeLocalEvent<InfantComponent, ComponentShutdown>(OnInfantShutdown);
    }

    private void OnUnpaused(EntityUid uid, BreedableComponent component, ref EntityUnpausedEvent args)
    {
        component.NextBreedAttempt += args.PausedTime;
    }

    private void OnInfantUnpaused(EntityUid uid, InfantComponent component, ref EntityUnpausedEvent args)
    {
        component.InfantEndTime += args.PausedTime;
    }

    // we express EZ-pass terminate the pregnancy if a player takes the role
    private void OnMindAdded(EntityUid uid, BreedableComponent component, MindAddedMessage args)
    {
        component.Gestating = false;
        component.GestationEndTime = null;
    }

    private void OnInfantStartup(EntityUid uid, InfantComponent component, ComponentStartup args)
    {
        var meta = MetaData(uid);
        component.OriginalName = meta.EntityName;
        meta.EntityName = Loc.GetString("infant-name-prefix", ("name", meta.EntityName));
    }

    private void OnInfantShutdown(EntityUid uid, InfantComponent component, ComponentShutdown args)
    {
        MetaData(uid).EntityName = component.OriginalName;
    }

    /// <summary>
    /// Attempts to breed the entity with a valid
    /// partner nearby.
    /// </summary>
    public bool TryBreedNearby(EntityUid uid, BreedableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var xform = Transform(uid);
        var partners = _entityLookup.GetComponentsInRange<BreedingPartnerComponent>(xform.Coordinates, component.BreedRange);
        foreach (var comp in partners)
        {
            var partner = comp.Owner;
            if (TryBreed(uid, partner, component))
                return true;

            // exit early if a valid attempt failed
            if (_failedAttempts.Contains(uid))
                return false;
        }
        return false;
    }

    /// <summary>
    /// Attempts to breed an entity with
    /// the specified partner.
    /// </summary>
    public bool TryBreed(EntityUid uid, EntityUid partner, BreedableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (uid == partner)
            return false;

        if (!CanBreed(uid, component))
            return false;

        if (!IsValidPartner(uid, partner, component))
            return false;

        // if the partner is valid, yet it fails the random check
        // invalidate the entity from further attempts this tick
        // in order to reduce total possible pairs.
        if (!_random.Prob(component.BreedChance))
        {
            _failedAttempts.Add(uid);
            _failedAttempts.Add(partner);
            return false;
        }

        // this is kinda wack but it's the only sound associated with most animals
        if (TryComp<InteractionPopupComponent>(uid, out var interactionPopup))
            _audio.PlayPvs(interactionPopup.InteractSuccessSound, uid);

        _hunger.ModifyHunger(uid, -component.HungerPerBirth);
        _hunger.ModifyHunger(partner, -component.HungerPerBirth);

        component.GestationEndTime = _timing.CurTime + component.GestationDuration;
        component.Gestating = true;
        _adminLog.Add(LogType.Action, $"{ToPrettyString(uid)} (carrier) and {ToPrettyString(partner)} (partner) successfully bred.");
        return true;
    }

    /// <summary>
    /// Checks if an entity satisfies
    /// the conditions to be able to breed.
    /// </summary>
    public bool CanBreed(EntityUid uid, BreedableComponent? component = null)
    {
        if (_failedAttempts.Contains(uid))
            return false;

        if (Resolve(uid, ref component, false) && component.Gestating)
            return false;

        if (HasComp<InfantComponent>(uid))
            return false;

        if (_mobState.IsIncapacitated(uid))
            return false;

        if (TryComp<HungerComponent>(uid, out var hunger) && _hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
            return false;

        if (TryComp<ThirstComponent>(uid, out var thirst) && thirst.CurrentThirstThreshold < ThirstThreshold.Okay)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a given entity is a valid partner.
    /// Does not include the random check, for sane API reasons.
    /// </summary>
    public bool IsValidPartner(EntityUid uid, EntityUid partner, BreedableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanBreed(partner))
            return false;

        return component.PartnerWhitelist.IsValid(partner);
    }

    /// <summary>
    /// Gives birth to offspring and
    /// resets the parent entity.
    /// </summary>
    public void Birth(EntityUid uid, BreedableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // this is kinda wack but it's the only sound associated with most animals
        if (TryComp<InteractionPopupComponent>(uid, out var interactionPopup))
            _audio.PlayPvs(interactionPopup.InteractSuccessSound, uid);

        var xform = Transform(uid);
        var spawns = EntitySpawnCollection.GetSpawns(component.Offspring, _random);
        foreach (var spawn in spawns)
        {
            var offspring = Spawn(spawn, xform.Coordinates.Offset(_random.NextVector2(0.3f)));
            if (component.MakeOffspringInfant)
            {
                var infant = AddComp<InfantComponent>(offspring);
                infant.InfantEndTime = _timing.CurTime + infant.InfantDuration;
            }
            _adminLog.Add(LogType.Action, $"{ToPrettyString(uid)} gave birth to {ToPrettyString(offspring)}.");
        }

        _popup.PopupEntity(Loc.GetString(component.BirthPopup, ("parent", Identity.Entity(uid, EntityManager))), uid);

        component.Gestating = false;
        component.GestationEndTime = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        HashSet<EntityUid> birthQueue = new();
        _failedAttempts.Clear();

        var query = EntityQueryEnumerator<BreedableComponent>();
        while (query.MoveNext(out var uid, out var breedable))
        {
            if (breedable.GestationEndTime != null && _timing.CurTime >= breedable.GestationEndTime)
            {
                birthQueue.Add(uid);
            }

            if (_timing.CurTime < breedable.NextBreedAttempt)
                continue;
            breedable.NextBreedAttempt += _random.Next(breedable.MinBreedAttemptInterval, breedable.MaxBreedAttemptInterval);

            // no.
            if (HasComp<ActorComponent>(uid))
                continue;

            TryBreedNearby(uid, breedable);
        }

        foreach (var queued in birthQueue)
        {
            Birth(queued);
        }

        var infantQuery = EntityQueryEnumerator<InfantComponent>();
        while (infantQuery.MoveNext(out var uid, out var infant))
        {
            if (_timing.CurTime < infant.InfantEndTime)
                continue;
            RemCompDeferred(uid, infant);
        }
    }
}
