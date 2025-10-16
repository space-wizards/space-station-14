// SPDX-License-Identifier: MIT

using Content.Shared.Singularity.Components;

namespace Content.Server.ParticleAccelerator.Components;

[RegisterComponent]
public sealed partial class ParticleProjectileComponent : Component
{
    public ParticleAcceleratorPowerState State;
}
