using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Server.Chemistry.Components;

/// <summary>
/// Used for embeddable entities that should try to inject a
/// contained solution into a target when they become embedded in it.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionInjectWhileEmbedComponent : BaseSolutionInjectOnEventComponent {
	
		[DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate;
		
        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

	
}

