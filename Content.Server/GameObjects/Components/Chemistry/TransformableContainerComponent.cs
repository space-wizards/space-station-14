#nullable enable
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class TransformableContainerComponent : Component, ISolutionChange
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "TransformableContainer";

        private SpriteSpecifier? _initialSprite;
        private string _initialName = default!;
        private string _initialDescription = default!;
        private ReagentPrototype? _currentReagent;

        public bool Transformed { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out SpriteComponent? sprite) &&
                sprite.BaseRSIPath != null)
            {
                _initialSprite = new SpriteSpecifier.Rsi(new ResourcePath(sprite.BaseRSIPath), "icon");
            }

            _initialName = Owner.Name;
            _initialDescription = Owner.Description;
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponentWarn(out SolutionContainerComponent solution);

            solution.Capabilities |= SolutionContainerCaps.FitsInDispenser;
        }

        public void CancelTransformation()
        {
            _currentReagent = null;
            Transformed = false;

            if (Owner.TryGetComponent(out SpriteComponent? sprite) &&
                _initialSprite != null)
            {
                sprite.LayerSetSprite(0, _initialSprite);
            }

            Owner.Name = _initialName;
            Owner.Description = _initialDescription;
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            var solution = eventArgs.Owner.GetComponent<SolutionContainerComponent>();
            //Transform container into initial state when emptied
            if (_currentReagent != null && solution.ReagentList.Count == 0)
            {
                CancelTransformation();
            }

            //the biggest reagent in the solution decides the appearance
            var reagentId = solution.Solution.GetPrimaryReagentId();

            //If biggest reagent didn't changed - don't change anything at all
            if (_currentReagent != null && _currentReagent.ID == reagentId)
            {
                return;
            }

            //Only reagents with spritePath property can change appearance of transformable containers!
            if (!string.IsNullOrWhiteSpace(reagentId) &&
                _prototypeManager.TryIndex(reagentId, out ReagentPrototype proto) &&
                !string.IsNullOrWhiteSpace(proto.SpriteReplacementPath))
            {
                var spriteSpec = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Consumable/Drinks/" + proto.SpriteReplacementPath),"icon");

                if (Owner.TryGetComponent(out SpriteComponent? sprite))
                {
                    sprite?.LayerSetSprite(0, spriteSpec);
                }

                Owner.Name = proto.Name + " glass";
                Owner.Description = proto.Description;
                _currentReagent = proto;
                Transformed = true;
            }
        }
    }
}
