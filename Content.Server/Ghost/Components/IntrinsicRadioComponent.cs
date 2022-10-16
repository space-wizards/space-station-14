using Content.Server.Radio.Components;

namespace Content.Server.Ghost.Components
{
    /// <summary>
    /// Add to a particular entity to let it directly receive messages. Note that this does not automatically add an <see cref="ActiveRadioComponent"/>
    /// </summary>
    [RegisterComponent]
    public sealed class IntrinsicRadioComponent : Component
    {
    }
}
