using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class UnremoveableComponent : Component 
	{
        [DataField("deleteOnDrop")]
        public bool DeleteOnDrop = true;
    }
}
