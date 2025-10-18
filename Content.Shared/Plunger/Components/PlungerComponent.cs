using Robust.Shared.GameStates;

namespace Content.Shared.Plunger.Components
{
    /// <summary>
    /// Allows entity to unblock target entity with PlungerUseComponent.
    /// </summary>
    [RegisterComponent, NetworkedComponent,AutoGenerateComponentState]
    public sealed partial class PlungerComponent : Component
    {
        /// <summary>
        /// Duration of plunger doafter event.
        /// </summary>
        [DataField]
        [AutoNetworkedField]
        public float PlungeDuration = 2f;
    }
}
