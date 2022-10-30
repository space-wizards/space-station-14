using System.Linq;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Disposal.Tube.Components
{
    public abstract class DisposalTubeComponent : Component, IDisposalTubeComponent
    {
        public virtual string ContainerId => "DisposalTube";

        [Dependency] private readonly IEntityManager _entMan = default!;

        public static readonly TimeSpan ClangDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastClang;

        private bool _connected;
        [DataField("clangSound")] public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        /// <summary>
        ///     Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; private set; } = default!;

        /// <summary>
        ///     The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        protected abstract Direction[] ConnectableDirections();

        public abstract Direction NextDirection(DisposalHolderComponent holder);

        // TODO: Make disposal pipes extend the grid
        // ???
        public void Connect()
        {
            if (_connected)
            {
                return;
            }

            _connected = true;
        }

        public bool CanConnect(Direction direction, IDisposalTubeComponent with)
        {
            if (!_connected)
            {
                return false;
            }

            if (!ConnectableDirections().Contains(direction))
            {
                return false;
            }

            return true;
        }

        public void Disconnect()
        {
            if (!_connected)
            {
                return;
            }

            _connected = false;

            var disposableSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<DisposableSystem>();
            foreach (var entity in Contents.ContainedEntities.ToArray())
            {
                if (!_entMan.TryGetComponent(entity, out DisposalHolderComponent? holder))
                {
                    continue;
                }

                disposableSystem.ExitDisposals((holder).Owner);
            }
        }

        public void PopupDirections(EntityUid entity)
        {
            var directions = string.Join(", ", ConnectableDirections());

            _entMan.EntitySysManager.GetEntitySystem<SharedPopupSystem>()
                .PopupEntity(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), Owner, Filter.Entities(entity));
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
