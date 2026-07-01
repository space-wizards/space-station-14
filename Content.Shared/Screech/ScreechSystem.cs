using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
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
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private SharedStunSystem _stuns = default!;

    [Dependency] private EntityQuery<StatusEffectsComponent> _statusEffectsQuery = default!;

    private HashSet<EntityUid> _entSet = new();

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
        Screech(ent.Owner, args.Range, args.Vfx, args.ScreechSound, args.SoundRange, args.KnockdownChances);
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
    public void Screech(EntityUid source, float range, EntProtoId? vfx = null, SoundSpecifier? screechSound = null, float soundRange = 6f, float knockDownChances = 0.5f, float speedMultiplier = 1f, TimeSpan? slowdownTime = null)
    {
        // first, we spawn the vfx attached to the source
        if (vfx.HasValue)
        {
            EntProtoId vfxProto = vfx.Value;
            var k = Spawn(vfxProto);
            var container = _containers.EnsureContainer<ContainerSlot>(source, "screechHolder");
            _containers.Insert(k, container);
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

            EntityHeardIt(entity, source, knockDownChances, speedMultiplier, slowdownTime.GetValueOrDefault());
        }

        _audio.PlayPredicted(screechSound, source, null, AudioParams.Default.WithVolume(1f).WithMaxDistance(soundRange));
    }

    /// <summary>
    /// Tests if that singular entity heard it (it may have screech protection) and if it did it will disarm it
    /// </summary>
    private void EntityHeardIt(EntityUid ent, EntityUid source, float knockdownChances, float speedModifier, TimeSpan slowdownDuration)
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

        // slow it down
        if (speedModifier != 1f && slowdownDuration > TimeSpan.Zero)
            _movementMod.TryUpdateMovementSpeedModDuration(ent, MovementModStatusSystem.FlashSlowdown, slowdownDuration, speedModifier);

        // maybe knock it down
        if (!SharedRandomExtensions.PredictedProb(_timing, knockdownChances, GetNetEntity(ent)))
            _stuns.TryKnockdown(ent, null);
    }
}

/// <summary>
/// Event that is used to check if an entity hears the screech & feels its full effects
/// </summary>
[ByRefEvent]
public struct ScreechEffectAttemptEvent : IInventoryRelayEvent
{
    /// <summary>
    /// The entity that caused the screech
    /// </summary>
    public EntityUid Source;

    /// <summary>
    /// Wether it was heard
    /// </summary>
    public bool Heard;

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.EARS | SlotFlags.EYES;
}

/// <summary>
/// Event that is fire when an entity uses a screech action
/// </summary>
public sealed partial class ScreechActionEvent : InstantActionEvent
{
    /// <summary>
    /// The range of the screech's effects
    /// </summary>
    [DataField]
    public float Range = 6f;

    /// <summary>
    /// Entity that will be spawned in a container on the screecher to display effects
    /// </summary>
    [DataField]
    public EntProtoId? Vfx = "EffectScreech";

    /// <summary>
    /// Sound that will be played by the screech
    /// </summary>
    [DataField]
    public SoundSpecifier? ScreechSound = null;

    /// <summary>
    /// Range at which the sound will be heard
    /// </summary>
    [DataField]
    public float SoundRange = 20f;

    /// <summary>
    /// Chances of the screech knocking down the victim
    /// </summary>
    [DataField]
    public float KnockdownChances = 0.5f;

    /// <summary>
    /// Speed modifier of the entities who heard the screech (withing range)
    /// </summary>
    [DataField]
    public float SpeedMultiplier = 1f;

    /// <summary>
    /// How long the screech's slow down lasts
    /// </summary>
    [DataField]
    public float SlowdownTime = 0f;
}
