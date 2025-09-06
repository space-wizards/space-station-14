using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Shared.IdentityManagement;

public abstract class SharedIdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;

    private static string _slotName = "identity";

    // Recycled hashset for perf
    private readonly HashSet<EntityUid> _queuedIdentityUpdates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<IdentityBlockerComponent, SeeIdentityAttemptEvent>(OnSeeIdentity);
        SubscribeLocalEvent<IdentityBlockerComponent, InventoryRelayedEvent<SeeIdentityAttemptEvent>>((e, c, ev) => OnSeeIdentity(e, c, ev.Args));
        SubscribeLocalEvent<IdentityBlockerComponent, ItemMaskToggledEvent>(OnMaskToggled);

        SubscribeLocalEvent<IdentityComponent, DidEquipEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidEquipHandEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidUnequipEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidUnequipHandEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, WearerMaskToggledEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, EntityRenamedEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, MapInitEvent>(OnMapInit);
    }

    private void OnSeeIdentity(EntityUid uid, IdentityBlockerComponent component, SeeIdentityAttemptEvent args)
    {
        if (component.Enabled)
        {
            args.TotalCoverage |= component.Coverage;
            if(args.TotalCoverage == IdentityBlockerCoverage.FULL)
                args.Cancel();
        }
    }

    protected virtual void OnComponentInit(EntityUid uid, IdentityComponent component, ComponentInit args)
    {
        component.IdentityEntitySlot = _container.EnsureContainer<ContainerSlot>(uid, _slotName);
    }

    private void OnMaskToggled(Entity<IdentityBlockerComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.Mask.Comp.IsToggled;
    }

    /// <summary>
    ///     Queues an identity update to the start of the next tick.
    /// </summary>
    public void QueueIdentityUpdate(EntityUid uid)
    {
        _queuedIdentityUpdates.Add(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _queuedIdentityUpdates)
        {
            if (!TryComp<IdentityComponent>(ent, out var identity))
                continue;

            UpdateIdentityInfo(ent, identity);
        }

        _queuedIdentityUpdates.Clear();
    }

    // This is where the magic happens
    private void OnMapInit(EntityUid uid, IdentityComponent component, MapInitEvent args)
    {
        var ident = Spawn(null, Transform(uid).Coordinates);

        _metaData.SetEntityName(ident, "identity");
        QueueIdentityUpdate(uid);
        _container.Insert(ident, component.IdentityEntitySlot);
    }

    #region Private API

    /// <summary>
    ///     Updates the metadata name for the id(entity) from the current state of the character.
    /// </summary>
    private void UpdateIdentityInfo(EntityUid uid, IdentityComponent identity)
    {
        if (identity.IdentityEntitySlot.ContainedEntity is not { } ident)
            return;

        var representation = GetIdentityRepresentation(uid);
        var name = GetIdentityName(uid, representation);

        // Clone the old entity's grammar to the identity entity, for loc purposes.
        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            var identityGrammar = EnsureComp<GrammarComponent>(ident);
            identityGrammar.Attributes.Clear();

            foreach (var (k, v) in grammar.Attributes)
            {
                identityGrammar.Attributes.Add(k, v);
            }

            // If presumed name is null and we're using that, we set proper noun to be false ("the old woman")
            if (name != representation.TrueName && representation.PresumedName == null)
                _grammarSystem.SetProperNoun((ident, identityGrammar), false);

            Dirty(ident, identityGrammar);
        }

        if (name == Name(ident))
            return;

        _metaData.SetEntityName(ident, name);

        _adminLog.Add(LogType.Identity, LogImpact.Medium, $"{ToPrettyString(uid)} changed identity to {name}");
        var identityChangedEvent = new IdentityChangedEvent(uid, ident);
        RaiseLocalEvent(uid, ref identityChangedEvent);
    }

    private string GetIdentityName(EntityUid target, IdentityRepresentation representation)
    {
        var ev = new SeeIdentityAttemptEvent();

        RaiseLocalEvent(target, ev);
        return representation.ToStringKnown(!ev.Cancelled);
    }

    /// <summary>
    ///     Gets an 'identity representation' of an entity, with their true name being the entity name
    ///     and their 'presumed name' and 'presumed job' being the name/job on their ID card, if they have one.
    /// </summary>
    private IdentityRepresentation GetIdentityRepresentation(EntityUid target,
        InventoryComponent? inventory=null,
        HumanoidAppearanceComponent? appearance=null)
    {
        int age = 18;
        Gender gender = Gender.Epicene;
        string species = SharedHumanoidAppearanceSystem.DefaultSpecies;

        // Always use their actual age and gender, since that can't really be changed by an ID.
        if (Resolve(target, ref appearance, false))
        {
            gender = appearance.Gender;
            age = appearance.Age;
            species = appearance.Species;
        }

        var ageString = _humanoid.GetAgeRepresentation(species, age);
        var trueName = Name(target);
        if (!Resolve(target, ref inventory, false))
            return new(trueName, gender, ageString, string.Empty);

        string? presumedJob = null;
        string? presumedName = null;

        // Get their name and job from their ID for their presumed name.
        if (_idCard.TryFindIdCard(target, out var id))
        {
            presumedName = string.IsNullOrWhiteSpace(id.Comp.FullName) ? null : id.Comp.FullName;
            presumedJob = id.Comp.LocalizedJobTitle?.ToLowerInvariant();
        }

        // If it didn't find a job, that's fine.
        return new(trueName, gender, ageString, presumedName, presumedJob);
    }

    #endregion
}

/// <summary>
///     Gets called whenever an entity changes their identity.
/// </summary>
[ByRefEvent]
public record struct IdentityChangedEvent(EntityUid CharacterEntity, EntityUid IdentityEntity);
