using Content.Shared.Body.Part;

namespace Content.Shared._Starlight.Medical.Body;

public static class BodyParts
{
    public static BodyPartType GetBodyPart(string slotId) => slotId switch
    {
        "left arm" => BodyPartType.Arm,
        "right arm" => BodyPartType.Arm,
        "left hand" => BodyPartType.Hand,
        "right hand" => BodyPartType.Hand,
        "left leg" => BodyPartType.Leg,
        "right leg" => BodyPartType.Leg,
        "left foot" => BodyPartType.Foot,
        "right foot" => BodyPartType.Foot,
        "tail" => BodyPartType.Tail,
        _ => BodyPartType.Other,
    };
}