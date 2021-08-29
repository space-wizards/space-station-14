using Content.Shared.Acts;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Destructible
{
    [UsedImplicitly]
    public class DestructibleSystem : EntitySystem
    {
        [Dependency] public readonly IRobustRandom Random = default!;
        [Dependency] public readonly AudioSystem AudioSystem = default!;
        [Dependency] public readonly ActSystem ActSystem = default!;
    }
}
