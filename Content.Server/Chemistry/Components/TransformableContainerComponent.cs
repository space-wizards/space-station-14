using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public class TransformableContainerComponent : Component
    {
        public override string Name => "TransformableContainer";

        public SpriteSpecifier? InitialSprite;
        public string InitialName = default!;
        public string InitialDescription = default!;
        public ReagentPrototype? CurrentReagent;

        public bool Transformed { get; internal set; }

        protected override void Initialize()
        {
            base.Initialize();

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Uid, out SpriteComponent? sprite) &&
                sprite.BaseRSIPath != null)
            {
                InitialSprite = new SpriteSpecifier.Rsi(new ResourcePath(sprite.BaseRSIPath), "icon");
            }

            InitialName = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityName;
            InitialDescription = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityDescription;
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponentWarn<SolutionContainerManagerComponent>();
            Owner.EnsureComponentWarn<FitsInDispenserComponent>();
        }
    }
}
