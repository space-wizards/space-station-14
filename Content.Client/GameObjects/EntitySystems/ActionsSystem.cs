using Content.Client.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ActionsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // TODO: probably will use this for bindings in ClientActionsComponent?
    }
}
