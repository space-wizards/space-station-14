using Content.Shared.GameObjects.Components.Strap;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Strap
{
    [RegisterComponent]
    public class StrapComponent : SharedStrapComponent
    {
        private StrapPosition _position;

        public override StrapPosition Position
        {
            get => _position;
            set
            {
                _position = value;
                Dirty();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _position, "position", StrapPosition.None);
        }
    }
}
