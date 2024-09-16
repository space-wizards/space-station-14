using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Used for embeddable entities that should try to inject a
/// contained solution into a target when they become embedded in it.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SolutionInjectWhileEmbeddedComponent : BaseSolutionInjectOnEventComponent {
	
		[DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate;
		
        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

	
}

