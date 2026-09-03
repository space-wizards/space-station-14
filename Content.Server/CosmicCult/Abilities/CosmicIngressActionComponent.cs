using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.CosmicCult.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicIngressActionComponent : Component
{
    [DataField]
    public SoundSpecifier IngressSfx = new SoundPathSpecifier("/Audio/Cosmic/Abilities/ability-ingress.ogg");
}
