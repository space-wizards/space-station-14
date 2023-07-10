namespace Content.Server.Anomaly.Effects.Components;

/// <summary>
/// This is used for a spreader that's controlled by <see cref="SpreaderAnomalyComponent"/>
/// </summary>
[RegisterComponent, Access(typeof(SpreaderAnomalySystem))]
public sealed class AnomalyControlledSpreaderComponent : Component
{

}
