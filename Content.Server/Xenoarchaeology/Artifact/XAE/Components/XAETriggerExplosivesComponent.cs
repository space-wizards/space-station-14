using Content.Shared.Explosion.Components;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Activates <see cref="ExplosiveComponent"/> to explode.
/// </summary>
[RegisterComponent, Access(typeof(XAETriggerExplosivesSystem))]
public sealed partial class XAETriggerExplosivesComponent : Component;
