using System.Diagnostics;
using Content.Server.Drunk;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Shared.Bed.Sleep;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.ReagentEffects.Amnesia;

public sealed class AmnesiaSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKeySleep = "ForcedSleep";

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKeyJitter = "Jitter";

    /// <summary>
    /// Used to keep track of entities that have the amnesia component.
    /// </summary>
    private List<EntityUid> _amnesiaEntities = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AmnesiaComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AmnesiaComponent, ComponentRemove>(OnComponentRemove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var uid in _amnesiaEntities.ToArray()) // Copy the list so that we can remove entities from it while iterating over it.
        {
            if (!EntityManager.TryGetComponent(uid, out AmnesiaComponent? amnesiaComponent))
            {
                _amnesiaEntities.Remove(uid); // Remove the entity from the list if it no longer has the amnesia component.
                continue;
            }

            // Remove time from the amnesia component.
            amnesiaComponent.TimeUntilForget -= _timing.CurTime - amnesiaComponent.LastTime;
            amnesiaComponent.LastTime = _timing.CurTime;


            switch (amnesiaComponent.Stage)
            {
                // Message for 85s
                case 0 when amnesiaComponent.TimeUntilForget.TotalSeconds < 85:
                    _popupSystem.PopupEntity(Loc.GetString("amnesia-effect-stage-1"), uid, PopupType.LargeCaution);
                    amnesiaComponent.Stage++;
                    break;
                // Message for 70s
                case 1 when amnesiaComponent.TimeUntilForget.TotalSeconds < 70:
                    _popupSystem.PopupEntity(Loc.GetString("amnesia-effect-stage-2"), uid, PopupType.LargeCaution);
                    amnesiaComponent.Stage++;
                    break;
                // Message for 50s
                case 2 when amnesiaComponent.TimeUntilForget.TotalSeconds < 50:
                    _popupSystem.PopupEntity(Loc.GetString("amnesia-effect-stage-3"), uid, PopupType.LargeCaution);
                    amnesiaComponent.Stage++;
                    break;
                // Message for 30s
                case 3 when amnesiaComponent.TimeUntilForget.TotalSeconds < 30:
                    _popupSystem.PopupEntity(Loc.GetString("amnesia-effect-stage-4"), uid, PopupType.LargeCaution);
                    amnesiaComponent.Stage++;
                    break;
                case 4 when amnesiaComponent.TimeUntilForget.TotalSeconds < 27:
                    _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKeySleep,
                        TimeSpan.FromSeconds(50), true);
                    amnesiaComponent.Stage++;
                    break;
                case 5 when amnesiaComponent.TimeUntilForget.TotalSeconds < 15:
                    _popupSystem.PopupEntity(Loc.GetString("amnesia-effect-stage-5"), uid, PopupType.LargeCaution);
                    _statusEffectsSystem.TryAddStatusEffect<JitteringComponent>(uid, StatusEffectKeyJitter,
                        TimeSpan.FromSeconds(25), true);
                    amnesiaComponent.Stage++;
                    break;
                case 6 when amnesiaComponent.TimeUntilForget.TotalSeconds < 3:
                    EntityManager.RemoveComponent<AmnesiaComponent>(uid);
                    _popupSystem.PopupEntity(Loc.GetString("amnesia-effect-stage-6"), uid, PopupType.LargeCaution);
                    ForceGhostRoleAmnesia(uid);
                    break;
            }
        }
    }

    private void OnComponentInit(EntityUid uid, AmnesiaComponent component, ComponentInit args)
    {
        component.LastTime = _timing.CurTime;
        _amnesiaEntities.Add(uid);
    }

    private void OnComponentRemove(EntityUid uid, AmnesiaComponent component, ComponentRemove args)
    {
        _amnesiaEntities.Remove(uid);
    }


    /// <summary>
    /// Forces the target entities mind to ghost and also turns that entity into a ghost role with the amensia description.
    /// </summary>
    public void ForceGhostRoleAmnesia(EntityUid uid)
    {
        // If the entity already has a ghost role, don't add another one.
        if (_entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole) && !ghostRole.Taken)
        {
            return;
        }

        // If the entity has a mind, kick them out of it. This is done because the Ghost role cannot be taken when the entity has a mind.
        var minds = _entityManager.System<SharedMindSystem>();
        if (minds.TryGetMind(uid, out var mindId, out var mind))
        {
            _entityManager.System<GameTicker>().OnGhostAttempt(mindId, false, false, mind);
        }

        // If the entity has a ghost role, remove it. This is done, because ReregisterOnGhost is set to false.
        // Without this, the entity would not show up in the ghost role menu if the amnesia reagent effect is applied again.
        // Removing the ghost role component removes it in the GhostRoleSystem, meaning it will be re-added when the amnesia reagent effect is applied again.
        if (_entityManager.TryGetComponent(uid, out ghostRole))
        {
            _entityManager.RemoveComponent<GhostRoleComponent>(uid);
        }

        ghostRole = _entityManager.EnsureComponent<GhostRoleComponent>(uid);
        _entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = _entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-amnesia-description");
        // This is done so that doing /ghost will not show the entity in the ghost role menu.
        ghostRole.ReregisterOnGhost = false;
    }
}
