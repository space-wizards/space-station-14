using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for "Lambda Devices". These devices interact with Cosmic Cult in some way.
/// This component allows for functionality to be added to entities when the associated gamerules come into effect through event-driven systems.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CosmicLambdaDeviceComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Upgraded;

    [DataField]
    public SoundSpecifier UpgradeSound = new SoundPathSpecifier("/Audio/Cosmic/device-upgraded.ogg");
}
