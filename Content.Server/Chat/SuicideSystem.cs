using Content.Server.Ghost;
using Content.Server.Hands.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat;

public sealed class SuicideSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;

    private static readonly ProtoId<TagPrototype> CannotSuicideTag = "CannotSuicide";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, SuicideEvent>(OnDamageableSuicide);
        SubscribeLocalEvent<MobStateComponent, SuicideEvent>(OnEnvironmentalSuicide);
        SubscribeLocalEvent<MindContainerComponent, SuicideGhostEvent>(OnSuicideGhost);
    }

    /// <summary>
    /// Calling this function will attempt to kill the user by suiciding on objects in the surrounding area
    /// or by applying a lethal amount of damage to the user with the default method.
    /// Used when writing /suicide
    /// </summary>
    public bool Suicide(EntityUid victim)
    {
        // Can't suicide if we're already dead
        if (!TryComp<MobStateComponent>(victim, out var mobState) || _mobState.IsDead(victim, mobState))
            return false;

        _adminLogger.Add(LogType.Mind, $"{ToPrettyString(victim):player} is attempting to suicide");

        ICommonSession? session = null;

        if (TryComp<ActorComponent>(victim, out var actor))
            session = actor.PlayerSession;

        var suicideGhostEvent = new SuicideGhostEvent(victim);

        RaiseLocalEvent(victim, suicideGhostEvent);

        // Suicide is considered a fail if the user wasn't able to ghost
        // Suiciding with the CannotSuicide tag will ghost the player but not kill the body
        if (!suicideGhostEvent.Handled || _tagSystem.HasTag(victim, CannotSuicideTag))
            return false;

        // TODO: fix this
        // This is a handled event, but the result is never used
        // It looks like TriggerOnMobstateChange is supposed to prevent you from suiciding
        var suicideEvent = new SuicideEvent(victim);
        RaiseLocalEvent(victim, suicideEvent);

        // Since the player is already dead the log will not contain their username.
        if (session != null)
        {
            _adminLogger.Add(LogType.Mind, $"{session:player} suicided.");
        }
        else
        {
            _adminLogger.Add(LogType.Mind, $"{ToPrettyString(victim):player} suicided.");
        }
        return true;
    }

    /// <summary>
    /// Event subscription created to handle the ghosting aspect relating to suicides
    /// Mainly useful when you can raise an event in Shared and can't call Suicide() directly
    /// </summary>
    private void OnSuicideGhost(Entity<MindContainerComponent> victim, ref SuicideGhostEvent args)
    {
        if (args.Handled)
            return;

        if (victim.Comp.Mind == null)
            return;

        if (!TryComp<MindComponent>(victim.Comp.Mind, out var mindComponent))
            return;

        // CannotSuicide tag will allow the user to ghost, but also return to their mind
        // This is kind of weird, not sure what it applies to?
        if (_tagSystem.HasTag(victim, CannotSuicideTag))
            args.CanReturnToBody = true;

        if (_ghostSystem.OnGhostAttempt(victim.Comp.Mind.Value, args.CanReturnToBody, mind: mindComponent))
            args.Handled = true;
    }

    /// <summary>
    /// Raise event to attempt to use held item, or surrounding entities to attempt to commit suicide
    /// </summary>
    private void OnEnvironmentalSuicide(Entity<MobStateComponent> victim, ref SuicideEvent args)
    {
        if (args.Handled || _mobState.IsCritical(victim))
            return;

        var suicideByEnvironmentEvent = new SuicideByEnvironmentEvent(victim);

        // Try to suicide by raising an event on the held item
        if (_hands.TryGetActiveItem(victim.Owner, out var item))
        {
            RaiseLocalEvent(item.Value, suicideByEnvironmentEvent);
            if (suicideByEnvironmentEvent.Handled)
            {
                args.Handled = suicideByEnvironmentEvent.Handled;
                return;
            }
        }

        // Try to suicide by nearby entities, like Microwaves or Crematoriums, by raising an event on it
        // Returns upon being handled by any entity
        var itemQuery = GetEntityQuery<ItemComponent>();
        foreach (var entity in _entityLookupSystem.GetEntitiesInRange(victim, 1, LookupFlags.Approximate | LookupFlags.Static))
        {
            // Skip any nearby items that can be picked up, we already checked the active held item above
            if (itemQuery.HasComponent(entity))
                continue;

            RaiseLocalEvent(entity, suicideByEnvironmentEvent);
            if (!suicideByEnvironmentEvent.Handled)
                continue;

            args.Handled = suicideByEnvironmentEvent.Handled;
            return;
        }
    }

    /// <summary>
    /// Default suicide behavior for any kind of entity that can take damage
    /// </summary>
    private void OnDamageableSuicide(Entity<DamageableComponent> victim, ref SuicideEvent args)
    {
        if (args.Handled)
            return;

        var othersMessage = Loc.GetString("suicide-command-default-text-others", ("name", Identity.Entity(victim, EntityManager)));
        _popup.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);

        var selfMessage = Loc.GetString("suicide-command-default-text-self");
        _popup.PopupEntity(selfMessage, victim, victim);

        if (args.DamageSpecifier != null)
        {
            _suicide.ApplyLethalDamage(victim, args.DamageSpecifier);
            args.Handled = true;
            return;
        }

        args.DamageType ??= "Bloodloss";
        _suicide.ApplyLethalDamage(victim, args.DamageType);
        args.Handled = true;
    }
}
