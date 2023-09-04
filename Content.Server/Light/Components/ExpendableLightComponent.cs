using Content.Shared.Light.Components;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a handheld expendable light which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ExpendableLightComponent : SharedExpendableLightComponent
    {
        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated => CurrentState is ExpendableLightState.Lit or ExpendableLightState.Fading;

        [ViewVariables] public float StateExpiryTime = default;
    }
}
