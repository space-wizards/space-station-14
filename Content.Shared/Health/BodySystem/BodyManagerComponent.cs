using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Shared.BodySystem {
    public enum BodyPartCompatibility { Mechanical, Biological, Universal };
    public enum BodyPartType { Other, Torso, Head, Arm, Hand, Leg, Foot };
    public enum SurgeryToolType { Incision, Retraction, Cauterization }


}

namespace Content.Shared.BodySystem {

    /// <summary>
    ///     Component representing the many BodyParts attached to each other. 
    /// </summary>
    [RegisterComponent]
    public class BodyManagerComponent : Component {

#pragma warning disable CS0649
        [Dependency]
        private IPrototypeManager _prototypeManager;
#pragma warning restore

        [ViewVariables]
        private BodyTemplate _template;
        [ViewVariables]
        private Dictionary<string, BodyPart> _partDictionary = new Dictionary<string, BodyPart>();

        public sealed override string Name => "BodyManager";
        /// <summary>
        ///     The BodyTemplate that this BodyManagerComponent is adhering to.
        /// </summary>
        public BodyTemplate Template => _template;
        /// <summary>
        ///     Maps BodyTemplate slot name to the BodyPart object filling it (if there is one). 
        /// </summary>
        public Dictionary<string, BodyPart> PartDictionary => _partDictionary;
        /// <summary>
        ///     List of all BodyParts in this body, taken from the keys of _parts.
        /// </summary>
        public List<BodyPart> Parts {
            get
            {
                List<BodyPart> temp = new List<BodyPart>();
                foreach (var (key, value) in _partDictionary)
                    temp.Add(value);
                return temp;
            }
        }


        public override void ExposeData(ObjectSerializer serializer) {
            base.ExposeData(serializer);

            string templateName = "";
            serializer.DataField(ref templateName, "BaseTemplate", "bodyTemplate.Humanoid");
            if (!_prototypeManager.TryIndex(templateName, out BodyTemplatePrototype templateData))
                throw new InvalidOperationException("No BodyTemplatePrototype was found with the name " + templateName + " while loading a BodyTemplate!"); //Should never happen unless you fuck up the prototype.

            string presetName = "";
            serializer.DataField(ref presetName, "BasePreset", "bodyPreset.BasicHuman");
            if (!_prototypeManager.TryIndex(presetName, out BodyPresetPrototype presetData))
                throw new InvalidOperationException("No BodyPresetPrototype was found with the name " + presetName + " while loading a BodyPreset!"); //Should never happen unless you fuck up the prototype.

            _template = new BodyTemplate(templateData);
            LoadBodyPreset(new BodyPreset(presetData));
        }

        /// <summary>
        ///     Loads the given preset - forcefully changes all limbs found in both the preset and this template!
        /// </summary>		
        public void LoadBodyPreset(BodyPreset preset)
        {
            foreach (var (slotName, type) in _template.Slots)
            {
                if (!preset.PartIDs.TryGetValue(slotName, out string partID))
                { //For each slot in our BodyManagerComponent's template, try and grab what the ID of what the preset says should be inside it.
                    continue; //If the preset doesn't define anything for it, continue. 
                }
                if (!_prototypeManager.TryIndex(partID, out BodyPartPrototype newPartData))
                { //Get the BodyPartPrototype corresponding to the BodyPart ID we grabbed.
                    throw new InvalidOperationException("BodyPart prototype with ID " + partID + " could not be found!");
                }
                _partDictionary.Remove(slotName); //Try and remove an existing limb if that exists. 
                _partDictionary.Add(slotName, new BodyPart(newPartData)); //Add a new BodyPart with the BodyPartPrototype as a baseline to our BodyComponent.
            }
        }

        /// <summary>
        ///     Changes the current BodyTemplate to the new BodyTemplate. Attempts to keep previous BodyParts if there is a slot for them in both BodyTemplates.
        /// </summary>				
        public void ChangeBodyTemplate(BodyTemplatePrototype newTemplate)
        {
            foreach (KeyValuePair<string, BodyPart> part in _partDictionary)
            {
                //TODO: Make this work.
            }
        }

        /// <summary>
        ///     Tries to remove the given Mechanism from the given BodyPart. Returns null if there was an error in spawning the entity, otherwise returns a reference to the DroppedMechanismComponent on the newly spawned entity.
        /// </summary>	
        public DroppedMechanismComponent DropMechanism(BodyPart bodyPartTarget, Mechanism mechanismTarget)
        {
            if (!bodyPartTarget.RemoveMechanism(mechanismTarget))
                return null;
            var mechanismEntity = Owner.EntityManager.SpawnEntity("BaseDroppedMechanism", Owner.Transform.GridPosition);
            var droppedMechanismComponent = mechanismEntity.GetComponent<DroppedMechanismComponent>();
            droppedMechanismComponent.Initialize(mechanismTarget);
            return droppedMechanismComponent;
        }

        /// <summary>
        ///     Tries to destroy the given Mechanism in the given BodyPart. Returns false if there was an error, true otherwise. Does NOT spawn a dropped entity.
        /// </summary>	
        public bool DestroyMechanism(BodyPart bodyPartTarget, Mechanism mechanismTarget)
        {
            return bodyPartTarget.RemoveMechanism(mechanismTarget);
        }


        /// <summary>
        ///     Grabs all limbs of the given type in this body.
        /// </summary>	
        public List<BodyPart> GetBodyPartsOfType(BodyPartType type)
        {
            List<BodyPart> toReturn = new List<BodyPart>();
            foreach (var (slotName, bodyPart) in _partDictionary)
            {
                if (bodyPart.PartType == type)
                    toReturn.Add(bodyPart);
            }
            return toReturn;
        }

        /// <summary>
        ///     Disconnects the given BodyPart, potentially dropping other BodyParts if they were hanging off it. 
        /// </summary>
        public void DisconnectBodyPart(BodyPart part, bool dropEntity)
        {
            if (!_partDictionary.ContainsValue(part))
                return;
            if (part != null)
            {
                string slotName = _partDictionary.FirstOrDefault(x => x.Value == part).Key;
                if (TryGetBodyPartConnections(slotName, out List<string> connections)) //Call disconnect on all limbs that were hanging off this limb.
                {
                    foreach (string connectionName in connections) //This loop is an unoptimized travesty. TODO: optimize to be less shit
                    {
                        if (TryGetBodyPart(connectionName, out BodyPart result) && !ConnectedToCenterPart(result))
                        {
                            DisconnectBodyPartByName(connectionName, dropEntity);
                        }
                    }
                }
                if (dropEntity)
                {
                    var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                    partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                }
            }
        }


        /// <summary>
        ///     Attempts to perform surgery on the given BodyPart with the given tool. Returns false if there was an error, true if successful.
        /// </summary>
        public bool AttemptSurgery(BodyPart target, SurgeryToolType toolType) {
            if (!_partDictionary.ContainsValue(target))
                return false;
            return target.AttemptSurgery(toolType);
        }




        /// <summary>
        ///     Internal string version of DisconnectBodyPart for performance purposes.
        /// </summary>
        private void DisconnectBodyPartByName(string name, bool dropEntity)
        {
            if (!TryGetBodyPart(name, out BodyPart part))
                return;
            if (part != null)
            {
                if (TryGetBodyPartConnections(name, out List<string> connections)) 
                {
                    foreach (string connectionName in connections)
                    {
                        if (TryGetBodyPart(connectionName, out BodyPart result) && !ConnectedToCenterPart(result))
                        {
                            DisconnectBodyPartByName(connectionName, dropEntity);
                        }
                    }
                }
                if (dropEntity)
                {
                    var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                    partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                }
            }
        }

        /// <summary>
        ///     Returns the central BodyPart of this body based on the BodyTemplate. For humans, this is the torso. Returns null if not found.
        /// </summary>			
        private BodyPart GetCenterBodyPart()
        {
            _partDictionary.TryGetValue(_template.CenterSlot, out BodyPart center);
            return center;
        }

        /// <summary>
        ///     Recursive search that returns whether a given BodyPart is connected to the center BodyPart. Not efficient (O(n^2)), but most bodies don't have a ton of BodyParts.
        /// </summary>	
        private bool ConnectedToCenterPart(BodyPart target)
        {
            List<string> searchedSlots = new List<string> { };
            if (TryGetSlotName(target, out string result))
                return false;
            return ConnectedToCenterPartRecursion(searchedSlots, result);
        }

        private bool ConnectedToCenterPartRecursion(List<string> searchedSlots, string slotName)
        {
            TryGetBodyPart(slotName, out BodyPart part);
            if (part == GetCenterBodyPart())
                return true;
            TryGetBodyPartConnections(slotName, out List<string> connections);
            foreach (string connection in connections) {
                if (!searchedSlots.Contains(connection) && ConnectedToCenterPartRecursion(searchedSlots, slotName))
                    return true;
            }
            return false;
               
        }

        /// <summary>
        ///     Grabs the BodyPart in the given slotName if there is one. Returns true if a BodyPart is found, false otherwise.
        /// </summary>		
        private bool TryGetBodyPart(string slotName, out BodyPart result)
        {
            return _partDictionary.TryGetValue(slotName, out result);
        }

        /// <summary>
        ///     Grabs the slotName that the given BodyPart resides in. Returns true if the BodyPart is part of this body, false otherwise. 
        /// </summary>		
        private bool TryGetSlotName(BodyPart part, out string result)
        {
            result = _partDictionary.FirstOrDefault(x => x.Value == part).Key; //We enforce that there is only one of each value in the dictionary, so we can iterate through the dictionary values to get the key from there. 
            return result == null;
        }

        /// <summary>
        ///     Grabs the names of all connected slots to the given slotName from the template. Returns true if connections are found to the slotName, false otherwise. 
        /// </summary>	
        private bool TryGetBodyPartConnections(string slotName, out List<string> connections)
        {
            return _template.Connections.TryGetValue(slotName, out connections);
        }


    }
}
