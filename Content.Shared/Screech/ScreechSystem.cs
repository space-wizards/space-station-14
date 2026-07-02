using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Screech;

public sealed partial class ScreechSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedStunSystem _stuns = default!;

    [Dependency] private EntityQuery<StatusEffectsComponent> _statusEffectsQuery;

    private readonly HashSet<EntityUid> _entSet = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScreechShockWaveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NoiseProtectionComponent, ScreechEffectAttemptEvent>(OnScreechProtected);
        SubscribeLocalEvent<NoiseProtectionComponent, InventoryRelayedEvent<ScreechEffectAttemptEvent>>((a, ref b) => OnScreechProtected(a, ref b.Args));
        SubscribeLocalEvent<ActionsComponent, ScreechActionEvent>(OnScreechAction);
        SubscribeLocalEvent<NoiseProtectionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<NoiseProtectionComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineQuip.HasValue)
        {
            args.PushMarkup(Loc.GetString(ent.Comp.ExamineQuip.Value));
        }
    }

    private void OnScreechAction(Entity<ActionsComponent> ent, ref ScreechActionEvent args)
    {
        args.Handled = true;
        Screech(ent.Owner, args.Range, args.Vfx, args.ScreechSound, args.SoundRange, args.KnockdownChances, args.Effects);
    }

    private void OnScreechProtected(Entity<NoiseProtectionComponent> ent, ref ScreechEffectAttemptEvent args)
    {
        args.Heard = false;
    }

    private void OnMapInit(Entity<ScreechShockWaveComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.InitTime = _timing.CurTime;
        Dirty(ent);
    }

    /// <summary>
    /// Makes the entity "source" screech.
    /// </summary>
    public void Screech(EntityUid source, float range, EntProtoId? vfx = null, SoundSpecifier? screechSound = null, float soundRange = 6f, float knockdownChances = 0.5f, List<EntityEffect>? effects = null)
    {
        // first, we spawn the vfx attached to the source
        if (vfx.HasValue)
        {
            var vfxEntity = Spawn(vfx.Value);
            var container = _containers.EnsureContainer<ContainerSlot>(source, "screechHolder");
            _containers.Insert(vfxEntity, container);
        }

        // then, we do the screech per-se
        var transform = Transform(source);
        // clean entset cache
        _entSet.Clear();
        _entityLookup.GetEntitiesInRange(transform.Coordinates, range, _entSet);
        foreach (var entity in _entSet)
        {
            // Is the entity affected by the screech via status effects? (It would be a good idea to check for ears instead but IDK how to do that in a way that's performant :P)
            // The entity that screeched is also immune to the screech
            if (!_statusEffectsQuery.HasComponent(entity) || entity == source)
                continue;

            EntityHeardIt(entity, source, knockdownChances, effects);
        }

        _audio.PlayPredicted(screechSound, source, null, AudioParams.Default.WithVolume(1f).WithMaxDistance(soundRange));
    }

    /// <summary>
    /// Tests if that singular entity heard it (it may have screech protection) and if it did it will disarm it.
    /// </summary>
    private void EntityHeardIt(EntityUid ent, EntityUid source, float knockdownChances, List<EntityEffect>? effects)
    {
        var ev = new ScreechEffectAttemptEvent()
        {
            Source = source,
            Heard = true
        };
        RaiseLocalEvent(ent, ref ev);

        if (!ev.Heard)
            return; // if we return here, the entity had screech protection

        // does the disarming
        var dis = new DisarmedEvent(ent, source, knockdownChances);
        RaiseLocalEvent(ent, ref dis);

        // apply entity effects to the target
        if (effects != null)
        {
            foreach (var effect in effects)
            {
                _effects.TryApplyEffect(ent, effect, user: source);
            }
        }

        // maybe knock it down
        if (!SharedRandomExtensions.PredictedProb(_timing, knockdownChances, GetNetEntity(ent)))
            _stuns.TryKnockdown(ent, null);
    }
}

/// <summary>
/// Event that is used to check if an entity hears the screech & feels its full effects.
/// </summary>
[ByRefEvent]
public struct ScreechEffectAttemptEvent : IInventoryRelayEvent
{
    /// <summary>
    /// The entity that caused the screech.
    /// </summary>
    public EntityUid Source;

    /// <summary>
    /// Wether it was heard.
    /// </summary>
    public bool Heard;

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.EARS | SlotFlags.EYES;
}

/// <summary>
/// Event that is fire when an entity uses a screech action.
/// </summary>
public sealed partial class ScreechActionEvent : InstantActionEvent
{
    /// <summary>
    /// The range of the screech's effects.
    /// </summary>
    [DataField]
    public float Range = 6f;

    /// <summary>
    /// Entity that will be spawned in a container on the screecher to display effects.
    /// </summary>
    [DataField]
    public EntProtoId? Vfx = "EffectScreech";

    /// <summary>
    /// Sound that will be played by the screech.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScreechSound;

    /// <summary>
    /// Range at which the sound will be heard.
    /// </summary>
    [DataField]
    public float SoundRange = 20f;

    /// <summary>
    /// Chances of the screech knocking down the victim.
    /// </summary>
    [DataField]
    public float KnockdownChances = 0.5f;

    /// <summary>
    /// Entity effects applied to entities that heard the screech.
    /// </summary>
    [DataField]
    public List<EntityEffect> Effects = [];
}
