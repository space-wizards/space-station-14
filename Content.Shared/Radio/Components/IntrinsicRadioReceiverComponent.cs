using Robust.Shared.GameStates;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component allows an entity to directly translate radio messages into chat messages. Note that this does not
///     automatically add an <see cref="ActiveRadioComponent"/>, which is required to receive radio messages on specific
///     channels.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IntrinsicRadioReceiverComponent : Component;
