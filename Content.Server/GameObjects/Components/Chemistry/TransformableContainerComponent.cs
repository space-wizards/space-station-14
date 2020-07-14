using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Chemistry;
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
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public override string Name => "TransformableContainer";

        private bool _transformed = false;
        public bool Transformed { get => _transformed; }

        private SpriteSpecifier _initialSprite;
        private string _initialName;
        private string _initialDescription;
        private SpriteComponent _sprite;

        private ReagentPrototype _currentReagent;

        public override void Initialize()
        {
            base.Initialize();

            _sprite = Owner.GetComponent<SpriteComponent>();
            _initialSprite = new SpriteSpecifier.Rsi(new ResourcePath(_sprite.BaseRSIPath), "icon");
            _initialName = Owner.Name;
            _initialDescription = Owner.Description;
        }

        protected override void Startup()
        {
            base.Startup();
            Owner.GetComponent<SolutionComponent>().Capabilities |= SolutionCaps.FitsInDispenser;;
        }

        public void CancelTransformation()
        {
            _currentReagent = null;
            _transformed = false;
            _sprite.LayerSetSprite(0, _initialSprite);
            Owner.Name = _initialName;
            Owner.Description = _initialDescription;
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            var solution = eventArgs.Owner.GetComponent<SolutionComponent>();
            //Transform container into initial state when emptied
            if (_currentReagent != null && solution.ReagentList.Count == 0)
            {
                CancelTransformation();
            }

            //the biggest reagent in the solution decides the appearance
            var reagentId = solution.GetMajorReagentId();

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
                var spriteSpec = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Drinks/" + proto.SpriteReplacementPath),"icon");
                _sprite.LayerSetSprite(0, spriteSpec);
                Owner.Name = proto.Name + " glass";
                Owner.Description = proto.Description;
                _currentReagent = proto;
                _transformed = true;
            }
        }
    }
}
