using Content.Shared.Sound.Components;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Whenever this item is used upon by a specific entity prototype in the hand of a user, play a sound
    /// </summary>
    [RegisterComponent]
    public sealed partial class EmitSoundOnInteractUsingComponent : BaseEmitSoundComponent
    {
        [DataField("UsedItemID")]
        public string UsedItemID = "";
    }
}
