namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for artifacts that are biased to move
/// in a particular direction via the <see cref="TraversalDistorterComponent"/>
/// </summary>
[RegisterComponent]
public sealed class BiasedArtifactComponent : Component
{
    [ViewVariables]
    public EntityUid Provider;
}
