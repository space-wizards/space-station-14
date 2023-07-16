using System.Linq;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [Access(typeof(DisposalTubeSystem), typeof(DisposableSystem))]
    public sealed class DisposalTubeComponent : Component
    {
        [DataField("containerId")] public string ContainerId { get; set; } = "DisposalTube";

        [Dependency] private readonly IEntityManager _entMan = default!;

        public static readonly TimeSpan ClangDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastClang;

        public bool Connected;
        [DataField("clangSound")] public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        /// <summary>
        ///     Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; private set; } = default!;

        // TODO: Make disposal pipes extend the grid
        // ???
        public void Connect()
        {
            if (Connected)
            {
                return;
            }

            Connected = true;
        }

        public void Disconnect()
        {
            if (!Connected)
            {
                return;
            }

            Connected = false;

            foreach (var entity in Contents.ContainedEntities.ToArray())
            {
                if (!_entMan.TryGetComponent(entity, out DisposalHolderComponent? holder))
                {
                    continue;
                }

                EntitySystem.Get<DisposableSystem>().ExitDisposals((holder).Owner);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            Contents = ContainerHelpers.EnsureContainer<Container>(Owner, ContainerId);
            Owner.EnsureComponent<AnchorableComponent>();
        }

        protected override void OnRemove()
        {
            base.OnRemove();

            Disconnect();
        }
    }
}
