using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Materials;

/// <summary>
/// Handles interactions and logic related to <see cref="MaterialReclaimerComponent"/>,
/// <see cref="CollideMaterialReclaimerComponent"/>, and <see cref="ActiveMaterialReclaimerComponent"/>.
/// </summary>
public abstract class SharedMaterialReclaimerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAmbientSoundSystem AmbientSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;

    public const string ActiveReclaimerContainerId = "active-material-reclaimer-container";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MaterialReclaimerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MaterialReclaimerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MaterialReclaimerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<MaterialReclaimerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CollideMaterialReclaimerComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ActiveMaterialReclaimerComponent, ComponentStartup>(OnActiveStartup);
    }

    private void OnMapInit(EntityUid uid, MaterialReclaimerComponent component, MapInitEvent args)
    {
        component.NextSound = Timing.CurTime;
    }

    private void OnShutdown(EntityUid uid, MaterialReclaimerComponent component, ComponentShutdown args)
    {
        _audio.Stop(component.Stream);
    }

    private void OnExamined(EntityUid uid, MaterialReclaimerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("recycler-count-items", ("items", component.ItemsProcessed)));
    }

    private void OnEmagged(EntityUid uid, MaterialReclaimerComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    private void OnCollide(EntityUid uid, CollideMaterialReclaimerComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != component.FixtureId)
            return;
        if (!TryComp<MaterialReclaimerComponent>(uid, out var reclaimer))
            return;
        TryStartProcessItem(uid, args.OtherEntity, reclaimer);
    }

    private void OnActiveStartup(EntityUid uid, ActiveMaterialReclaimerComponent component, ComponentStartup args)
    {
        component.ReclaimingContainer = Container.EnsureContainer<Container>(uid, ActiveReclaimerContainerId);
    }

    /// <summary>
    /// Tries to start processing an item via a <see cref="MaterialReclaimerComponent"/>.
    /// </summary>
    public bool TryStartProcessItem(EntityUid uid, EntityUid item, MaterialReclaimerComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanStart(uid, component))
            return false;

        if (HasComp<MobStateComponent>(item) && !CanGib(uid, item, component)) // whitelist? We be gibbing, boy!
            return false;

        if (component.Whitelist is {} whitelist && !whitelist.IsValid(item))
            return false;

        if (component.Blacklist is {} blacklist && blacklist.IsValid(item))
            return false;

        if (Container.TryGetContainingContainer(item, out _) && !Container.TryRemoveFromContainer(item))
            return false;

        if (user != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(user.Value):player} destroyed {ToPrettyString(item)} in the material reclaimer, {ToPrettyString(uid)}");
        }

        if (Timing.CurTime > component.NextSound)
        {
            component.Stream = _audio.PlayPredicted(component.Sound, uid, user)?.Entity;
            component.NextSound = Timing.CurTime + component.SoundCooldown;
        }

        var reclaimedEvent = new GotReclaimedEvent(Transform(uid).Coordinates);
        RaiseLocalEvent(item, ref reclaimedEvent);

        var duration = GetReclaimingDuration(uid, item, component);
        // if it's instant, don't bother with all the active comp stuff.
        if (duration == TimeSpan.Zero)
        {
            Reclaim(uid, item, 1, component);
            return true;
        }

        var active = EnsureComp<ActiveMaterialReclaimerComponent>(uid);
        active.Duration = duration;
        active.EndTime = Timing.CurTime + duration;
        Container.Insert(item, active.ReclaimingContainer);
        return true;
    }

    /// <summary>
    /// Finishes processing an item, freeing up the the reclaimer.
    /// </summary>
    /// <remarks>
    /// This doesn't reclaim the entity itself, but rather ends the formal
    /// process started with <see cref="ActiveMaterialReclaimerComponent"/>.
    /// The actual reclaiming happens in <see cref="Reclaim"/>
    /// </remarks>
    public virtual bool TryFinishProcessItem(EntityUid uid, MaterialReclaimerComponent? component = null, ActiveMaterialReclaimerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return false;

        RemCompDeferred(uid, active);
        return true;
    }

    /// <summary>
    /// Spawns the materials and chemicals associated
    /// with an entity. Also deletes the item.
    /// </summary>
    public virtual void Reclaim(EntityUid uid,
        EntityUid item,
        float completion = 1f,
        MaterialReclaimerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.ItemsProcessed++;
        if (component.CutOffSound)
        {
            _audio.Stop(component.Stream);
        }

        Dirty(uid, component);
    }

    /// <summary>
    /// Sets the Enabled field on the reclaimer.
    /// </summary>
    public void SetReclaimerEnabled(EntityUid uid, bool enabled, MaterialReclaimerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;
        component.Enabled = enabled;
        AmbientSound.SetAmbience(uid, enabled && component.Powered);
        Dirty(uid, component);
    }

    /// <summary>
    /// Whether or not the specified reclaimer can currently
    /// begin reclaiming another entity.
    /// </summary>
    public bool CanStart(EntityUid uid, MaterialReclaimerComponent component)
    {
        if (HasComp<ActiveMaterialReclaimerComponent>(uid))
            return false;

        return component.Powered && component.Enabled;
    }

    /// <summary>
    /// Whether or not the reclaimer satisfies the conditions
    /// allowing it to gib/reclaim a living creature.
    /// </summary>
    public bool CanGib(EntityUid uid, EntityUid victim, MaterialReclaimerComponent component)
    {
        return component.Powered &&
               component.Enabled &&
               HasComp<BodyComponent>(victim) &&
               HasComp<EmaggedComponent>(uid);
    }

    /// <summary>
    /// Gets the duration of processing a specified entity.
    /// Processing is calculated from the sum of the materials within the entity.
    /// It does not regard the chemicals within it.
    /// </summary>
    public TimeSpan GetReclaimingDuration(EntityUid reclaimer,
        EntityUid item,
        MaterialReclaimerComponent? reclaimerComponent = null,
        PhysicalCompositionComponent? compositionComponent = null)
    {
        if (!Resolve(reclaimer, ref reclaimerComponent))
            return TimeSpan.Zero;

        if (!reclaimerComponent.ScaleProcessSpeed ||
            !Resolve(item, ref compositionComponent, false))
            return reclaimerComponent.MinimumProcessDuration;

        var materialSum = compositionComponent.MaterialComposition.Values.Sum();
        materialSum *= CompOrNull<StackComponent>(item)?.Count ?? 1;
        var duration = TimeSpan.FromSeconds(materialSum / reclaimerComponent.MaterialProcessRate);
        if (duration < reclaimerComponent.MinimumProcessDuration)
            duration = reclaimerComponent.MinimumProcessDuration;
        return duration;
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ActiveMaterialReclaimerComponent, MaterialReclaimerComponent>();
        while (query.MoveNext(out var uid, out var active, out var reclaimer))
        {
            if (Timing.CurTime < active.EndTime)
                continue;
            TryFinishProcessItem(uid, reclaimer, active);
        }
    }
}

[ByRefEvent]
public record struct GotReclaimedEvent(EntityCoordinates ReclaimerCoordinates);
