using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    [ByRefEvent]
    public readonly struct CuffedStateChangeEvent { }

    [NetworkedComponent()]
    public abstract class SharedCuffableComponent : Component
    {
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => Container.ContainedEntities.Count * 2;

        public EntityUid LastAddedCuffs => Container.ContainedEntities[^1];

        public IReadOnlyList<EntityUid> StoredEntities => Container.ContainedEntities;

        /// <summary>
        ///     Container of various handcuffs currently applied to the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public Container Container { get; set; } = default!;

        protected override void Initialize()
        {
            base.Initialize();

            Container = _sysMan.GetEntitySystem<SharedContainerSystem>().EnsureContainer<Container>(Owner, _componentFactory.GetComponentName(GetType()));
        }

        [ViewVariables]
        public bool CanStillInteract { get; set; } = true;

        [Serializable, NetSerializable]
        protected sealed class CuffableComponentState : ComponentState
        {
            public bool CanStillInteract { get; }
            public int NumHandsCuffed { get; }
            public string? RSI { get; }
            public string IconState { get; }
            public Color Color { get; }

            public CuffableComponentState(int numHandsCuffed, bool canStillInteract, string? rsiPath, string iconState, Color color)
            {
                NumHandsCuffed = numHandsCuffed;
                CanStillInteract = canStillInteract;
                RSI = rsiPath;
                IconState = iconState;
                Color = color;
            }
        }
    }
}
