using Content.Shared.GameObjects.Components.Strap;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Strap
{
    [RegisterComponent]
    public class StrapComponent : SharedStrapComponent
    {
        [Dependency] private readonly IEntitySystemManager _entitySystem;

        private StrapPosition _position;
        private string _buckleSound;
        private string _unbuckleSound;

        /// <summary>
        /// The change in position to the strapped mob
        /// </summary>
        public override StrapPosition Position
        {
            get => _position;
            set
            {
                _position = value;
                Dirty();
            }
        }

        /// <summary>
        /// The sound to be played when a mob is buckled
        /// </summary>
        [ViewVariables]
        public string BuckleSound => _buckleSound;

        /// <summary>
        /// The sound to be played when a mob is unbuckled
        /// </summary>
        [ViewVariables]
        public string UnbuckleSound => _unbuckleSound;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _position, "position", StrapPosition.None);
            serializer.DataField(ref _buckleSound, "buckleSound", "/Audio/effects/buckle.ogg");
            serializer.DataField(ref _unbuckleSound, "unbuckleSound", "/Audio/effects/unbuckle.ogg");
        }
    }
}
