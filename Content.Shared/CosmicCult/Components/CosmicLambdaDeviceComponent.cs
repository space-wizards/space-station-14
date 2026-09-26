using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CosmicCult.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CosmicLambdaDeviceComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Upgraded;

    [DataField]
    public SoundSpecifier UpgradeSound = new SoundPathSpecifier("/Audio/Cosmic/device-upgraded.ogg");
}
