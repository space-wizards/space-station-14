using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Muting
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MutedComponent : Component
    {

      /// <summary>
      /// Whether the entity should be able to be unmuted
      /// </summary>
      [DataField]
      public bool removable = true;

    }
}
