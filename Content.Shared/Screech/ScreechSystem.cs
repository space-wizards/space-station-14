using Content.Shared.Actions;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Screech;

public sealed partial class ScreechSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [Dependency] private EntityQuery<StatusEffectsComponent> _statusEffectsQuery = default!;

    private readonly HashSet<EntityUid> _entSet = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoiseProtectionComponent, InventoryRelayedEvent<ScreechEffectAttemptEvent>>((a, ref b) => OnScreechProtected(a, ref b.Args));
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<NoiseProtectionComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineText.HasValue)
        {
            args.PushMarkup(Loc.GetString(ent.Comp.ExamineText.Value));
        }
    }

    [SubscribeLocalEvent]
    private void OnScreechAction(Entity<ScreechActionComponent> ent, ref ScreechActionEvent args)
    {
        args.Handled = true;
        var param = ent.Comp; // shorthand
        Screech(args.Performer, param.Range, param.Vfx, param.ScreechSound, param.Effects);
    }

    [SubscribeLocalEvent]
    private void OnScreechProtected(Entity<NoiseProtectionComponent> ent, ref ScreechEffectAttemptEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<ScreechShockWaveComponent> ent, ref ComponentInit args)
    {
        ent.Comp.InitTime = _timing.CurTime;
        Dirty(ent);
    }

    /// <summary>
    /// Makes the entity "source" screech, applying the "effects" to every entity in "range" that does not have screech protection.
    /// </summary>
    public void Screech(EntityUid source, float range, EntProtoId? vfx = null, SoundSpecifier? screechSound = null, List<EntityEffect>? effects = null)
    {
        // first, we spawn the vfx attached to the source
        if (vfx.HasValue)
            PredictedSpawnAttachedTo(vfx.Value, new EntityCoordinates(source, 0f, 0f));

        // then, we do the screech per-se
        var transform = Transform(source);

        // reset entset cache
        _entSet.Clear();
        _entityLookup.GetEntitiesInRange(transform.Coordinates, range, _entSet);

        foreach (var entity in _entSet)
        {
            // Is the entity affected by the screech via status effects? (It would be a good idea to check for ears instead but IDK how to do that in a way that's performant :P)
            // The entity that screeched is also immune to the screech
            if (!_statusEffectsQuery.HasComponent(entity) || entity == source)
                continue;

            EntityHeardIt(entity, source, effects);
        }

        _audio.PlayPredicted(screechSound, source, source);
    }

    /// <summary>
    /// Tests if that singular entity heard it (it may have screech protection) and if it did it will receive the entity effects.
    /// </summary>
    private void EntityHeardIt(EntityUid ent, EntityUid source, List<EntityEffect>? effects)
    {
        var ev = new ScreechEffectAttemptEvent(source);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return; // if we return here, the entity had screech protection

        // apply entity effects to the target
        if (effects != null)
        {
            foreach (var effect in effects)
            {
                _effects.TryApplyEffect(ent, effect, user: source);
            }
        }
    }
}

/// <summary>
/// Event that is used to check if an entity hears the screech & feels its full effects.
/// </summary>
[ByRefEvent]
public record struct ScreechEffectAttemptEvent(EntityUid Source, bool Cancelled = false) : IInventoryRelayEvent
{

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.EARS | SlotFlags.EYES;
}

/// <summary>
/// Event that is fire when an entity uses a screech action.
/// </summary>
public sealed partial class ScreechActionEvent : InstantActionEvent;
