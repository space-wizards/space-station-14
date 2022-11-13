using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
public sealed class BodyCoveringComponentState : ComponentState
{
    public string PrimaryBodyCoveringId;

    public string SecondaryBodyCoveringId;

    public float SecondaryCoveringPercentage;

    public BodyCoveringComponentState(string primaryBodyCoveringId, string secondaryBodyCoveringId, float secondaryCoveringPercentage)
    {
        PrimaryBodyCoveringId = primaryBodyCoveringId;
        SecondaryBodyCoveringId = secondaryBodyCoveringId;
        SecondaryCoveringPercentage = secondaryCoveringPercentage;
    }
}
