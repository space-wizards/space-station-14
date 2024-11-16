using Content.Shared.Explosion.Components.OnTrigger;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Activates 'trigger' for <see cref="ExplodeOnTriggerComponent"/>ю
/// </summary>
[RegisterComponent, Access(typeof(XAENodeTriggerExplosivesSystem))]
public sealed partial class XAENodeTriggerExplosivesComponent : Component;
