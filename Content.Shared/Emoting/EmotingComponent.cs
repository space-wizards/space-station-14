using Robust.Shared.GameStates;

namespace Content.Shared.Emoting
{
    [RegisterComponent, NetworkedComponent]
    public sealed class EmotingComponent : Component
    {
        [Access(typeof(EmoteSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
        [DataField("enabled")]
        public bool Enabled = true;
    }
}
