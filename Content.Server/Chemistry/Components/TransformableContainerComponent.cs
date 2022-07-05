using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class TransformableContainerComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public SpriteSpecifier? InitialSprite;
        public string InitialName = default!;
        public string InitialDescription = default!;
        public ReagentPrototype? CurrentReagent;

        public bool Transformed { get; internal set; }

        protected override void Initialize()
        {
            base.Initialize();

            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite) &&
                sprite.BaseRSIPath != null)
            {
                InitialSprite = new SpriteSpecifier.Rsi(new ResourcePath(sprite.BaseRSIPath), "icon");
            }

            InitialName = _entMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            InitialDescription = _entMan.GetComponent<MetaDataComponent>(Owner).EntityDescription;
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponentWarn<SolutionContainerManagerComponent>();
            Owner.EnsureComponentWarn<FitsInDispenserComponent>();
        }
    }
}
