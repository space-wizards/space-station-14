using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Shared.PDA
{
    public class SharedPDAComponent : Component
    {
        public override string Name => "PDA";
        public override uint? NetID => ContentNetIDs.PDA;

        public override void Initialize()
        {
            base.Initialize();
        }


    }
}
