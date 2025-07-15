using Content.Shared.Trigger.Components.Effects;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Activates 'trigger' for <see cref="ExplodeOnTriggerComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(XAETriggerExplosivesSystem))]
public sealed partial class XAETriggerExplosivesComponent : Component;
