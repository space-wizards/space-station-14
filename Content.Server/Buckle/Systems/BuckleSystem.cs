using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Buckle;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Buckle.Systems;

[UsedImplicitly]
public sealed partial class BuckleSystem : SharedBuckleSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly InteractionSystem _interactions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly Shared.Standing.StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(InteractionSystem));
        UpdatesAfter.Add(typeof(InputSystem));

        InitializeBuckle();
        InitializeStrap();
    }
}
