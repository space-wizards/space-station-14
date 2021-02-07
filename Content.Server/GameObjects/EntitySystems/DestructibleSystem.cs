using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class DestructibleSystem : EntitySystem
    {
        [Dependency] public readonly IRobustRandom Random = default!;

        public AudioSystem AudioSystem { get; private set; }

        public ActSystem ActSystem { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            AudioSystem = Get<AudioSystem>();
            ActSystem = Get<ActSystem>();
        }
    }
}
