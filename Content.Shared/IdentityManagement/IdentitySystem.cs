using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Timing;

namespace Content.Shared.IdentityManagement;

/// <summary>
/// Responsible for updating the identity of an entity on init or clothing equip/unequip.
/// </summary>
public sealed class IdentitySystem : EntitySystem
{
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedCriminalRecordsConsoleSystem _criminalRecordsConsole = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    // The name of the container holding the identity entity
    private const string SlotName = "identity";

    // Recycled hashset for tracking identities each tick that need to update
    private readonly HashSet<EntityUid> _queuedIdentityUpdates = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityBlockerComponent, SeeIdentityAttemptEvent>(OnSeeIdentity);
        SubscribeLocalEvent<IdentityBlockerComponent, InventoryRelayedEvent<SeeIdentityAttemptEvent>>(OnRelaySeeIdentity);
        SubscribeLocalEvent<IdentityBlockerComponent, ItemMaskToggledEvent>(OnMaskToggled);

        SubscribeLocalEvent<IdentityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IdentityComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<IdentityComponent, DidEquipEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidEquipHandEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidUnequipEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidUnequipHandEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, WearerMaskToggledEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, EntityRenamedEvent>((uid, _, _) => QueueIdentityUpdate(uid));
    }

    /// <summary>
    /// Iterates through all identities that need to be updated.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _queuedIdentityUpdates)
        {
            if (!TryComp<IdentityComponent>(ent, out var identity))
                continue;

            UpdateIdentityInfo((ent, identity));
        }

        _queuedIdentityUpdates.Clear();
    }

    #region Event Handlers

    // Creates an identity entity, and store it in the identity container
    private void OnMapInit(Entity<IdentityComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.IdentityEntitySlot is not { } slot)
        {
            Log.Error($"Uninitialized IdentityEntitySlot for {ToPrettyString(ent.Owner)}.");
            return;
        }

        var ident = Spawn(null, Transform(ent).Coordinates);

        _metaData.SetEntityName(ident, "identity");
        QueueIdentityUpdate(ent);
        _container.Insert(ident, slot);
    }

    private void OnComponentInit(Entity<IdentityComponent> ent, ref ComponentInit args)
    {
        ent.Comp.IdentityEntitySlot = _container.EnsureContainer<ContainerSlot>(ent, SlotName);
    }

    // Adds an identity blocker's coverage, and cancels the event if coverage is complete.
    private void OnSeeIdentity(Entity<IdentityBlockerComponent> ent, ref SeeIdentityAttemptEvent args)
    {
        if (ent.Comp.Enabled)
        {
            args.TotalCoverage |= ent.Comp.Coverage;
            if (args.TotalCoverage == IdentityBlockerCoverage.FULL)
                args.Cancel();
        }
    }

    private void OnRelaySeeIdentity(Entity<IdentityBlockerComponent> ent, ref InventoryRelayedEvent<SeeIdentityAttemptEvent> args)
    {
        OnSeeIdentity(ent, ref args.Args);
    }

    // Toggles if a mask is hiding the identity.
    private void OnMaskToggled(Entity<IdentityBlockerComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.Mask.Comp.IsToggled;
        Dirty(ent);
    }

    #endregion

    /// <summary>
    /// Queues an identity update to the start of the next tick.
    /// </summary>
    public void QueueIdentityUpdate(EntityUid uid)
    {
        if (_timing.ApplyingState)
            return;

        _queuedIdentityUpdates.Add(uid);
    }
    #region Private API

    /// <summary>
    /// Updates the metadata name for the id(entity) from the current state of the character.
    /// </summary>
    private void UpdateIdentityInfo(Entity<IdentityComponent> ent)
    {
        if (ent.Comp.IdentityEntitySlot?.ContainedEntity is not { } ident)
            return;

        var representation = GetIdentityRepresentation(ent.Owner);
        var name = GetIdentityName(ent, representation);

        // Clone the old entity's grammar to the identity entity, for loc purposes.
        if (TryComp<GrammarComponent>(ent, out var grammar))
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

        _adminLog.Add(LogType.Identity, LogImpact.Medium, $"{ToPrettyString(ent)} changed identity to {name}");
        var identityChangedEvent = new IdentityChangedEvent(ent, ident);
        RaiseLocalEvent(ent, ref identityChangedEvent);
        SetIdentityCriminalIcon(ent);
    }

    /// <summary>
    /// When the identity of a person is changed, searches the criminal records to see if the name of the new identity
    /// has a record. If the new name has a criminal status attached to it, the person will get the criminal status
    /// until they change identity again.
    /// </summary>
    private void SetIdentityCriminalIcon(EntityUid uid)
    {
        _criminalRecordsConsole.CheckNewIdentity(uid);
    }

    /// <summary>
    /// Attempts to get an entity's name. Cancelled if the entity has full coverage from <see cref="IdentityBlockerComponent"/>.
    /// </summary>
    /// <param name="target">The entity being targeted.</param>
    /// <param name="representation">The data structure containing an entity's identities.</param>
    /// <returns>
    /// An entity's real name if <see cref="SeeIdentityAttemptEvent"/> isn't cancelled,
    /// or a hidden identity such as a fake ID or fully hidden identity like "middle-aged man".
    /// </returns>
    private string GetIdentityName(EntityUid target, IdentityRepresentation representation)
    {
        var ev = new SeeIdentityAttemptEvent();

        RaiseLocalEvent(target, ev);
        return representation.ToStringKnown(!ev.Cancelled);
    }

    /// <summary>
    /// Gets an 'identity representation' of an entity, with their true name being the entity name
    /// and their 'presumed name' and 'presumed job' being the name/job on their ID card, if they have one.
    /// </summary>
    private IdentityRepresentation GetIdentityRepresentation(Entity<InventoryComponent?, HumanoidAppearanceComponent?> target)
    {
        var age = 18;
        var gender = Gender.Epicene;
        var species = SharedHumanoidAppearanceSystem.DefaultSpecies;

        // Always use their actual age and gender, since that can't really be changed by an ID.
        if (Resolve(target, ref target.Comp2, false))
        {
            gender = target.Comp2.Gender;
            age = target.Comp2.Age;
            species = target.Comp2.Species;
        }

        var ageString = _humanoid.GetAgeRepresentation(species, age);
        var trueName = Name(target);
        if (!Resolve(target, ref target.Comp1, false))
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
/// Gets called whenever an entity changes their identity.
/// </summary>
[ByRefEvent]
public record struct IdentityChangedEvent(EntityUid CharacterEntity, EntityUid IdentityEntity);
