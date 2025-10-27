using Content.Shared.Administration.Logs;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen;

/// <summary>
/// Used to butcher some entities like monkeys.
/// </summary>
public sealed class SharedKitchenSpikeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _logger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitchenSpikeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<KitchenSpikeComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<KitchenSpikeComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<KitchenSpikeComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<KitchenSpikeComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeHookDoAfterEvent>(OnSpikeHookDoAfter);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeUnhookDoAfterEvent>(OnSpikeUnhookDoAfter);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeButcherDoAfterEvent>(OnSpikeButcherDoAfter);
        SubscribeLocalEvent<KitchenSpikeComponent, ExaminedEvent>(OnSpikeExamined);
        SubscribeLocalEvent<KitchenSpikeComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<KitchenSpikeComponent, DestructionEventArgs>(OnDestruction);

        SubscribeLocalEvent<KitchenSpikeVictimComponent, ExaminedEvent>(OnVictimExamined);

        // Prevent the victim from doing anything while on the spike.
        SubscribeLocalEvent<KitchenSpikeHookedComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, UpdateCanMoveEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, IsEquippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<KitchenSpikeHookedComponent, IsUnequippingAttemptEvent>(OnAttempt);

        // Container Jank
        SubscribeLocalEvent<KitchenSpikeHookedComponent, AccessibleOverrideEvent>(OnAccessibleOverride);
    }

    private void OnInit(Entity<KitchenSpikeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    private void OnInsertAttempt(Entity<KitchenSpikeComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || TryComp<ButcherableComponent>(args.EntityUid, out var butcherable) && butcherable.Type == ButcheringType.Spike)
            return;

        args.Cancel();
    }

    private void OnEntInsertedIntoContainer(Entity<KitchenSpikeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_gameTiming.ApplyingState)
            return;

        EnsureComp<KitchenSpikeHookedComponent>(args.Entity);
        _damageableSystem.TryChangeDamage(args.Entity, ent.Comp.SpikeDamage, true);

        ent.Comp.NextDamage = _gameTiming.CurTime + ent.Comp.DamageInterval;
        Dirty(ent);

        // TODO: Add sprites for different species.
        _appearanceSystem.SetData(ent.Owner, KitchenSpikeVisuals.Status, KitchenSpikeStatus.Bloody);
    }

    private void OnEntRemovedFromContainer(Entity<KitchenSpikeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_gameTiming.ApplyingState)
            return;

        RemComp<KitchenSpikeHookedComponent>(args.Entity);
        _damageableSystem.TryChangeDamage(args.Entity, ent.Comp.SpikeDamage, true);

        _appearanceSystem.SetData(ent.Owner, KitchenSpikeVisuals.Status, KitchenSpikeStatus.Empty);
    }

    private void OnInteractHand(Entity<KitchenSpikeComponent> ent, ref InteractHandEvent args)
    {
        var victim = ent.Comp.BodyContainer.ContainedEntity;

        if (args.Handled || !victim.HasValue)
            return;

        _popupSystem.PopupClient(Loc.GetString("butcherable-need-knife",
            ("target", Identity.Entity(victim.Value, EntityManager))),
            ent,
            args.User,
            PopupType.Medium);

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<KitchenSpikeComponent> ent, ref InteractUsingEvent args)
    {
        var victim = ent.Comp.BodyContainer.ContainedEntity;

        if (args.Handled || !TryComp<ButcherableComponent>(victim, out var butcherable) || butcherable.SpawnedEntities.Count == 0)
            return;

        args.Handled = true;

        if (!TryComp<SharpComponent>(args.Used, out var sharp))
        {
            _popupSystem.PopupClient(Loc.GetString("butcherable-need-knife",
                    ("target", Identity.Entity(victim.Value, EntityManager))),
                    ent,
                    args.User,
                    PopupType.Medium);

            return;
        }

        var victimIdentity = Identity.Entity(victim.Value, EntityManager);

        _popupSystem.PopupPredicted(Loc.GetString("comp-kitchen-spike-begin-butcher-self", ("victim", victimIdentity)),
            Loc.GetString("comp-kitchen-spike-begin-butcher", ("user", Identity.Entity(args.User, EntityManager)), ("victim", victimIdentity)),
            ent,
            args.User,
            PopupType.MediumCaution);

        var delay = TimeSpan.FromSeconds(sharp.ButcherDelayModifier * butcherable.ButcherDelay);

        if (_mobStateSystem.IsAlive(victim.Value))
            delay += ent.Comp.ButcherDelayAlive;
        else
            delay *= ent.Comp.ButcherModifierDead;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            delay,
            new SpikeButcherDoAfterEvent(),
            ent,
            target: victim,
            used: args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnCanDrop(Entity<KitchenSpikeComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = _containerSystem.CanInsert(args.Dragged, ent.Comp.BodyContainer);
        args.Handled = true;
    }

    private void OnDragDrop(Entity<KitchenSpikeComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        ShowPopups("comp-kitchen-spike-begin-hook-self",
            "comp-kitchen-spike-begin-hook-self-other",
            "comp-kitchen-spike-begin-hook-other-self",
            "comp-kitchen-spike-begin-hook-other",
            args.User,
            args.Dragged,
            ent);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.HookDelay,
            new SpikeHookDoAfterEvent(),
            ent,
            target: args.Dragged)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });

        args.Handled = true;
    }

    private void OnSpikeHookDoAfter(Entity<KitchenSpikeComponent> ent, ref SpikeHookDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        if (_containerSystem.Insert(args.Target.Value, ent.Comp.BodyContainer))
        {
            ShowPopups("comp-kitchen-spike-hook-self",
                "comp-kitchen-spike-hook-self-other",
                "comp-kitchen-spike-hook-other-self",
                "comp-kitchen-spike-hook-other",
                args.User,
                args.Target.Value,
                ent);

            // normally medium severity, but for humanoids high severity, so new players get relay'd to admin alerts.
            var logSeverity = HasComp<HumanoidAppearanceComponent>(args.Target) ? LogImpact.High : LogImpact.Medium;

            _logger.Add(LogType.Action,
                logSeverity,
                $"{ToPrettyString(args.User):user} put {ToPrettyString(args.Target):target} on the {ToPrettyString(ent):spike}");

            _audioSystem.PlayPredicted(ent.Comp.SpikeSound, ent, args.User);
        }

        args.Handled = true;
    }

    private void OnSpikeUnhookDoAfter(Entity<KitchenSpikeComponent> ent, ref SpikeUnhookDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        if (_containerSystem.Remove(args.Target.Value, ent.Comp.BodyContainer))
        {
            ShowPopups("comp-kitchen-spike-unhook-self",
                "comp-kitchen-spike-unhook-self-other",
                "comp-kitchen-spike-unhook-other-self",
                "comp-kitchen-spike-unhook-other",
                args.User,
                args.Target.Value,
                ent);

            _logger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.User):user} took {ToPrettyString(args.Target):target} off the {ToPrettyString(ent):spike}");

            _audioSystem.PlayPredicted(ent.Comp.SpikeSound, ent, args.User);
        }

        args.Handled = true;
    }

    private void OnSpikeButcherDoAfter(Entity<KitchenSpikeComponent> ent, ref SpikeButcherDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue || !args.Used.HasValue || !TryComp<ButcherableComponent>(args.Target, out var butcherable))
            return;

        var victimIdentity = Identity.Entity(args.Target.Value, EntityManager);

        _popupSystem.PopupPredicted(Loc.GetString("comp-kitchen-spike-butcher-self", ("victim", victimIdentity)),
            Loc.GetString("comp-kitchen-spike-butcher", ("user", Identity.Entity(args.User, EntityManager)), ("victim", victimIdentity)),
            ent,
            args.User,
            PopupType.MediumCaution);

        // Get a random entry to spawn.
        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var index = rand.Next(butcherable.SpawnedEntities.Count);
        var entry = butcherable.SpawnedEntities[index];

        var uid = PredictedSpawnNextToOrDrop(entry.PrototypeId, ent);
        _metaDataSystem.SetEntityName(uid,
            Loc.GetString("comp-kitchen-spike-meat-name",
                ("name", Name(uid)),
                ("victim", args.Target)));

        // Decrease the amount since we spawned an entity from that entry.
        entry.Amount--;

        // Remove the entry if its new amount is zero, or update it.
        if (entry.Amount <= 0)
            butcherable.SpawnedEntities.RemoveAt(index);
        else
            butcherable.SpawnedEntities[index] = entry;

        Dirty(args.Target.Value, butcherable);

        // Gib the victim if there is nothing else to butcher.
        if (butcherable.SpawnedEntities.Count == 0)
        {
            _bodySystem.GibBody(args.Target.Value, true);

            var logSeverity = HasComp<HumanoidAppearanceComponent>(args.Target) ? LogImpact.Extreme : LogImpact.High;

            _logger.Add(LogType.Gib,
                logSeverity,
                $"{ToPrettyString(args.User):user} finished butchering {ToPrettyString(args.Target):target} on the {ToPrettyString(ent):spike}");
        }
        else
        {
            EnsureComp<KitchenSpikeVictimComponent>(args.Target.Value);

            _damageableSystem.ChangeDamage(args.Target.Value, ent.Comp.ButcherDamage, true);

            // Log severity for damaging other entities is normally medium.
            _logger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.User):user} butchered {ToPrettyString(args.Target):target} on the {ToPrettyString(ent):spike}");
        }

        _audioSystem.PlayPredicted(ent.Comp.ButcherSound, ent, args.User);

        _popupSystem.PopupClient(Loc.GetString("butcherable-knife-butchered-success",
            ("target", Identity.Entity(args.Target.Value, EntityManager)),
            ("knife", args.Used.Value)),
            ent,
            args.User,
            PopupType.Medium);

        args.Handled = true;
    }

    private void OnSpikeExamined(Entity<KitchenSpikeComponent> ent, ref ExaminedEvent args)
    {
        var victim = ent.Comp.BodyContainer.ContainedEntity;

        if (!victim.HasValue)
            return;

        // Show it at the end of the examine so it looks good.
        args.PushMarkup(Loc.GetString("comp-kitchen-spike-hooked", ("victim", Identity.Entity(victim.Value, EntityManager))), -1);
        args.PushMessage(_examineSystem.GetExamineText(victim.Value, args.Examiner), -2);
    }

    private void OnGetVerbs(Entity<KitchenSpikeComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var victim = ent.Comp.BodyContainer.ContainedEntity;

        if (!victim.HasValue || !_containerSystem.CanRemove(victim.Value, ent.Comp.BodyContainer))
            return;

        var user = args.User;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("comp-kitchen-spike-unhook-verb"),
            Act = () => TryUnhook(ent, user, victim.Value),
            Impact = LogImpact.Medium,
        });
    }

    private void OnDestruction(Entity<KitchenSpikeComponent> ent, ref DestructionEventArgs args)
    {
        _containerSystem.EmptyContainer(ent.Comp.BodyContainer, destination: Transform(ent).Coordinates);
    }

    private void OnVictimExamined(Entity<KitchenSpikeVictimComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("comp-kitchen-spike-victim-examine", ("target", Identity.Entity(ent, EntityManager))));
    }

    private static void OnAttempt(EntityUid uid, KitchenSpikeHookedComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnAccessibleOverride(Entity<KitchenSpikeHookedComponent> ent, ref AccessibleOverrideEvent args)
    {
        // Check if the entity is the target to avoid giving the hooked entity access to everything.
        // If we already have access we don't need to run more code.
        if (args.Accessible || args.Target != ent.Owner)
            return;

        var xform = Transform(ent);
        if (!_interaction.CanAccess(args.User, xform.ParentUid))
            return;

        args.Accessible = true;
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<KitchenSpikeComponent>();

        while (query.MoveNext(out var uid, out var kitchenSpike))
        {
            var contained = kitchenSpike.BodyContainer.ContainedEntity;

            if (!contained.HasValue)
                continue;

            if (kitchenSpike.NextDamage > _gameTiming.CurTime)
                continue;

            kitchenSpike.NextDamage += kitchenSpike.DamageInterval;
            Dirty(uid, kitchenSpike);

            _damageableSystem.ChangeDamage(contained.Value, kitchenSpike.TimeDamage, true);
        }
    }

    /// <summary>
    /// A helper method to show predicted popups that can be targeted towards yourself or somebody else.
    /// </summary>
    private void ShowPopups(string selfLocMessageSelf,
        string selfLocMessageOthers,
        string locMessageSelf,
        string locMessageOthers,
        EntityUid user,
        EntityUid victim,
        EntityUid hook)
    {
        string messageSelf, messageOthers;

        var victimIdentity = Identity.Entity(victim, EntityManager);

        if (user == victim)
        {
            messageSelf = Loc.GetString(selfLocMessageSelf, ("hook", hook));
            messageOthers = Loc.GetString(selfLocMessageOthers, ("victim", victimIdentity), ("hook", hook));
        }
        else
        {
            messageSelf = Loc.GetString(locMessageSelf, ("victim", victimIdentity), ("hook", hook));
            messageOthers = Loc.GetString(locMessageOthers,
                ("user", Identity.Entity(user, EntityManager)),
                ("victim", victimIdentity),
                ("hook", hook));
        }

        _popupSystem.PopupPredicted(messageSelf, messageOthers, hook, user, PopupType.MediumCaution);
    }

    /// <summary>
    /// Tries to unhook the victim.
    /// </summary>
    private void TryUnhook(Entity<KitchenSpikeComponent> ent, EntityUid user, EntityUid target)
    {
        ShowPopups("comp-kitchen-spike-begin-unhook-self",
            "comp-kitchen-spike-begin-unhook-self-other",
            "comp-kitchen-spike-begin-unhook-other-self",
            "comp-kitchen-spike-begin-unhook-other",
            user,
            target,
            ent);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            ent.Comp.UnhookDelay,
            new SpikeUnhookDoAfterEvent(),
            ent,
            target: target)
        {
            BreakOnDamage = user != target,
            BreakOnMove = true,
        });
    }
}

[Serializable, NetSerializable]
public sealed partial class SpikeHookDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SpikeUnhookDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SpikeButcherDoAfterEvent : SimpleDoAfterEvent;
