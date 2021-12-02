using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.CharacterInfo
{
    [NetworkedComponent()]
    public class SharedCharacterInfoComponent : Component
    {
        public override string Name => "CharacterInfo";
    }
}
