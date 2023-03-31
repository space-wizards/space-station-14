using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Materials;

public abstract class SharedMaterialReclaimerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public const string ActiveReclaimerContainerId = "active-material-reclaimer-container";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MaterialReclaimerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MaterialReclaimerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ActiveMaterialReclaimerComponent, ComponentStartup>(OnActiveStartup);
        SubscribeLocalEvent<ActiveMaterialReclaimerComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnGetState(EntityUid uid, MaterialReclaimerComponent component, ref ComponentGetState args)
    {
        args.State = new MaterialReclaimerComponentState(component.Powered, component.MaterialProcessRate);
    }

    private void OnHandleState(EntityUid uid, MaterialReclaimerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MaterialReclaimerComponentState state)
            return;
        component.Powered = state.Powered;
        component.MaterialProcessRate = state.MaterialProcessRate;
    }

    private void OnActiveStartup(EntityUid uid, ActiveMaterialReclaimerComponent component, ComponentStartup args)
    {
        component.ReclaimingContainer = _container.EnsureContainer<Container>(uid, ActiveReclaimerContainerId);
    }

    private void OnUnpaused(EntityUid uid, ActiveMaterialReclaimerComponent component, ref EntityUnpausedEvent args)
    {
        component.EndTime += args.PausedTime;
    }

    public bool TryStartProcessItem(EntityUid uid, EntityUid item, MaterialReclaimerComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Powered)
            return false;

        if (IsBusy(uid))
            return false;

        if (component.Whitelist != null && !component.Whitelist.IsValid(item))
            return false;

        if (component.Blacklist != null && component.Blacklist.IsValid(item))
            return false;

        if (user != null)
            _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(user.Value):player} destroyed {ToPrettyString(item)} in the material reclaimer, {ToPrettyString(uid)}");

        var ev = new GetMaterialReclaimedEvent();
        RaiseLocalEvent(item, ref ev);

        if (_net.IsServer)
            _ambientSound.SetAmbience(uid, true);

        var active = EnsureComp<ActiveMaterialReclaimerComponent>(uid);
        var duration = GetReclaimingDuration(uid, item, component);
        active.Duration = duration;
        active.EndTime = Timing.CurTime + duration;
        active.ReclaimingContainer.Insert(item);
        return true;
    }

    public virtual bool TryFinishProcessItem(EntityUid uid, MaterialReclaimerComponent? component = null, ActiveMaterialReclaimerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return false;

        if (_net.IsServer)
            _ambientSound.SetAmbience(uid, false);

        RemCompDeferred(uid, active);
        return true;
    }

    public bool IsBusy(EntityUid uid)
    {
        return HasComp<ActiveMaterialReclaimerComponent>(uid);
    }

    /// <summary>
    /// Gets the duration of processing a specified entity.
    /// Processing is calculated from the sum of the materials within the entity.
    /// It does not regard the chemicals within it.
    /// </summary>
    /// <param name="reclaimer"></param>
    /// <param name="item"></param>
    /// <param name="reclaimerComponent"></param>
    /// <param name="compositionComponent"></param>
    /// <returns></returns>
    public TimeSpan GetReclaimingDuration(EntityUid reclaimer,
        EntityUid item,
        MaterialReclaimerComponent? reclaimerComponent = null,
        PhysicalCompositionComponent? compositionComponent = null)
    {
        if (!Resolve(reclaimer, ref reclaimerComponent))
            return TimeSpan.Zero;

        if (!Resolve(item, ref compositionComponent, false))
            return reclaimerComponent.MinimumProcessDuration;

        var materialSum = compositionComponent.MaterialComposition.Values.Sum();
        materialSum *= CompOrNull<StackComponent>(item)?.Count ?? 1;
        var duration = TimeSpan.FromSeconds(materialSum / reclaimerComponent.MaterialProcessRate);
        if (duration < reclaimerComponent.MinimumProcessDuration)
            duration = reclaimerComponent.MinimumProcessDuration;
        return duration;
    }

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
