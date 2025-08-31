// Starlight

using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Starlight;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid
{
    public enum CyberneticImplantType {
        Undefined,
        Limb,
        Organ
    }

    [Serializable, NetSerializable]
    public struct CyberneticImplant {
        public string ID;
        public string Name;
        public int Cost;
        public CyberneticImplantType Type;
        public List<string> AttachedParts;

        // Search for all entity prototypes that have 
        public static List<CyberneticImplant> GetAllCybernetics(IPrototypeManager _prototypeManager){
            return _prototypeManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => p.Components.TryGetValue("RoundstartImplantable", out _))
                .Select(p => {
                    if (p.Components.TryGetValue("RoundstartImplantable", out var implant) &&
                        implant.Component is RoundstartImplantableComponent implantComp &&
                        p.Parents is not null){
                            return new CyberneticImplant{
                                ID = p.ID,
                                Name = p.Name,
                                Cost = implantComp.Cost,
                                Type = p.Parents.Contains("PartCyber") ? CyberneticImplantType.Limb 
                                        : p.Parents.Contains("OrganCyber") ? CyberneticImplantType.Organ 
                                            : CyberneticImplantType.Undefined,
                                AttachedParts = p.Components.TryGetValue("WithAttachedBodyParts", out var parts) && parts.Component is WithAttachedBodyPartsComponent partComp ? 
                                                    partComp.Parts.Values.Select(p => (string)p).Distinct().ToList() : []
                                };
                    } else {
                        return new CyberneticImplant{
                            ID = "broken"
                        };
                    }
                })
                .Where(p => p.ID != "broken")
                .Where(p => p.Type != CyberneticImplantType.Undefined)
                .ToList();
        }

        // Gets supported HumanoidVisualLayer for sprite layer updates
        public static HumanoidVisualLayers LayerFromBodypart(BodyPartComponent part) {
            switch ((part.PartType, part.Symmetry))
            {
                case (BodyPartType.Arm, BodyPartSymmetry.Left):
                    return HumanoidVisualLayers.LArm;
                case (BodyPartType.Arm, BodyPartSymmetry.Right):
                    return HumanoidVisualLayers.RArm;
                case (BodyPartType.Hand, BodyPartSymmetry.Left):
                    return HumanoidVisualLayers.LHand;
                case (BodyPartType.Hand, BodyPartSymmetry.Right):
                    return HumanoidVisualLayers.RHand;
                case (BodyPartType.Leg, BodyPartSymmetry.Left):
                    return HumanoidVisualLayers.LLeg;
                case (BodyPartType.Leg, BodyPartSymmetry.Right):
                    return HumanoidVisualLayers.RLeg;
                case (BodyPartType.Foot, BodyPartSymmetry.Left):
                    return HumanoidVisualLayers.LFoot;
                case (BodyPartType.Foot, BodyPartSymmetry.Right):
                    return HumanoidVisualLayers.RFoot;
                default:
                    return HumanoidVisualLayers.Special;
            }
        }

        // Gets slot id for limb system
        public static string SlotIDFromBodypart(BodyPartComponent part){
            string slot = "";
            switch (part.Symmetry){
                case BodyPartSymmetry.None:
                    break;
                case BodyPartSymmetry.Left:
                    slot += "left ";
                    break;
                case BodyPartSymmetry.Right:
                    slot += "right ";
                    break;
            }
            switch (part.PartType){
                case BodyPartType.Arm:
                    slot += "arm";
                    break;
                case BodyPartType.Leg:
                    slot += "leg";
                    break;
                case BodyPartType.Hand:
                    slot += "hand";
                    break;
                case BodyPartType.Foot:
                    slot += "foot";
                    break;
                default:
                    return "";
            }
            return slot;
        }

    }

}
