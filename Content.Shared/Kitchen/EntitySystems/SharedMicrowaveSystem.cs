using Content.Shared.DeviceLinking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

/// <summary>
///     A system that handles microwave logic, such as activation, malfunctions, and producing cooked recipes.
///     TODO: Replace with a more sophisticated(?) cooking system.
/// </summary>
public abstract partial class SharedMicrowaveSystem : EntitySystem
{
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private RecipeManager _recipeManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MicrowaveComponent, MapInitEvent>(OnMapInit);

        InitializeActive();
        InitializeContainer();
        InitializeUI();
    }

    /// <summary>
    ///     Processes every active microwave's ongoing cooking operation.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ActiveMicrowaveComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out var active, out var microwave))
        {
            var timeElapsed = curTime - active.LastCookUpdated;

            // Roll malfunctions
            if (active.NextMalfunction < curTime)
            {
                active.NextMalfunction += microwave.MalfunctionInterval;
                DirtyField(uid, active, nameof(ActiveMicrowaveComponent.NextMalfunction));

                RollMalfunction((uid, microwave));
            }

            // Finish cooking
            if (active.CookTimeEnd < curTime)
            {
                AddTemperature((uid, microwave), (float)timeElapsed.TotalSeconds);
                CompleteCooking((uid, active, microwave));
                continue;
            }

            // Otherwise, process the cooking cycle
            if (active.NextCookUpdate < curTime)
            {
                active.NextCookUpdate += active.CookUpdateInterval;
                active.LastCookUpdated = curTime;
                DirtyField(uid, active, nameof(ActiveMicrowaveComponent.NextCookUpdate));
                DirtyField(uid, active, nameof(ActiveMicrowaveComponent.LastCookUpdated));

                AddTemperature((uid, microwave), (float)timeElapsed.TotalSeconds);
            }
        }
    }

    /// <summary>
    ///     Initializes the microwave's storage container.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnComponentInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
    {
        // this really does have to be in ComponentInit
        ent.Comp.Storage = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    /// <summary>
    ///     Adds an "on" port to this microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnMapInit(Entity<MicrowaveComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
    }

    /// <summary>
    ///     Updates the microwave's appearance state.
    /// </summary>
    /// <param name="uid">The microwave entity.</param>
    /// <param name="state">The visual state of the microwave.</param>
    /// <param name="component">The entity's microwave component.</param>
    /// <param name="appearanceComponent">The microwave's appearance component.</param>
    private void SetAppearance(Entity<MicrowaveComponent?> ent,
        MicrowaveVisualState state,
        AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, ref appearanceComponent, logMissing: false))
            return;

        var display = ent.Comp.Broken ? MicrowaveVisualState.Broken : state;
        Appearance.SetData(ent.Owner,
            PowerDeviceVisuals.VisualState,
            display,
            appearanceComponent);
    }
}

[Serializable, NetSerializable]
public enum MicrowaveVisualState
{
    Idle,
    Cooking,
    Broken,
    Bloody
}

[NetSerializable, Serializable]
public enum MicrowaveUiKey
{
    Key
}
