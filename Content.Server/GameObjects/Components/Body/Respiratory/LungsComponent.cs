using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Respiratory
{
    [RegisterComponent]
    public class LungsComponent : Component
    {
        public ReagentUnit MaxVolume
        {
            get => _lungsContents.MaxVolume;
            set => _lungsContents.MaxVolume = value;
        }

        /// <summary>
        ///     Internal solution storage
        /// </summary>
        [ViewVariables]
        private SolutionComponent _lungsContents;

        protected override void Startup()
        {
            base.Startup();

            _lungsContents = Owner.GetComponent<SolutionComponent>();
        }

        public override string Name => "Lungs";

        public void Update(float frameTime)
        {
            // TODO
        }
    }
}
