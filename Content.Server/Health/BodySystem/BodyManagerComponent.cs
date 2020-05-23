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
using Content.Server.GameObjects.EntitySystems;

namespace Content.Server.BodySystem {

    /// <summary>
    ///     Component representing the many BodyParts attached to each other. 
    /// </summary>
    [RegisterComponent]
    public class BodyManagerComponent : Component, IInteractHand {

        public sealed override string Name => "BodyManager";
#pragma warning disable CS0649
        [Dependency]
        private IPrototypeManager _prototypeManager;
#pragma warning restore

        [ViewVariables]
        private BodyTemplate _template;

        [ViewVariables]
        private Dictionary<string, BodyPart> _partDictionary = new Dictionary<string, BodyPart>();

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
        public IEnumerable<BodyPart> Parts
        {
            get
            {
                return _partDictionary.Values;
            }
        }

        /// <summary>
        ///     Recursive search that returns whether a given BodyPart is connected to the center BodyPart. Not efficient (O(n^2)), but most bodies don't have a ton of BodyParts.
        /// </summary>	
        protected bool ConnectedToCenterPart(BodyPart target)
        {
            List<string> searchedSlots = new List<string> { };
            if (TryGetSlotName(target, out string result))
                return false;
            return ConnectedToCenterPartRecursion(searchedSlots, result);
        }

        protected bool ConnectedToCenterPartRecursion(List<string> searchedSlots, string slotName)
        {
            TryGetBodyPart(slotName, out BodyPart part);
            if (part == GetCenterBodyPart())
                return true;
            searchedSlots.Add(slotName);
            if (TryGetBodyPartConnections(slotName, out List<string> connections))
            {
                foreach (string connection in connections)
                {
                    if (!searchedSlots.Contains(connection) && ConnectedToCenterPartRecursion(searchedSlots, connection))
                        return true;
                }
            }
            return false;

        }

        /// <summary>
        ///     Returns the central BodyPart of this body based on the BodyTemplate. For humans, this is the torso. Returns null if not found.
        /// </summary>			
        protected BodyPart GetCenterBodyPart()
        {
            _partDictionary.TryGetValue(_template.CenterSlot, out BodyPart center);
            return center;
        }

        /// <summary>
        ///     Grabs the BodyPart in the given slotName if there is one. Returns true if a BodyPart is found, false otherwise. If false, result will be null.
        /// </summary>		
        protected bool TryGetBodyPart(string slotName, out BodyPart result)
        {
            return _partDictionary.TryGetValue(slotName, out result);
        }

        /// <summary>
        ///     Grabs the slotName that the given BodyPart resides in. Returns true if the BodyPart is part of this body, false otherwise. If false, result will be null.
        /// </summary>		
        protected bool TryGetSlotName(BodyPart part, out string result)
        {
            result = _partDictionary.FirstOrDefault(x => x.Value == part).Key; //We enforce that there is only one of each value in the dictionary, so we can iterate through the dictionary values to get the key from there. 
            return result == null;
        }

        /// <summary>
        ///     Grabs the names of all connected slots to the given slotName from the template. Returns true if connections are found to the slotName, false otherwise. If false, connections will be null.
        /// </summary>	
        protected bool TryGetBodyPartConnections(string slotName, out List<string> connections)
        {
            return _template.Connections.TryGetValue(slotName, out connections);
        }

        /////////
        /////////  Server-specific stuff
        /////////

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            //TODO: remove organs? 
            return false;
        }

        public override void ExposeData(ObjectSerializer serializer) {
            base.ExposeData(serializer);

            string templateName = "";
            serializer.DataField(ref templateName, "BaseTemplate", "bodyTemplate.Humanoid");
            if (serializer.Reading)
            {
                if (!_prototypeManager.TryIndex(templateName, out BodyTemplatePrototype templateData))
                    throw new InvalidOperationException("No BodyTemplatePrototype was found with the name " + templateName + " while loading a BodyTemplate!"); //Should never happen unless you fuck up the prototype.

                string presetName = "";
                serializer.DataField(ref presetName, "BasePreset", "bodyPreset.BasicHuman");
                if (!_prototypeManager.TryIndex(presetName, out BodyPresetPrototype presetData))
                    throw new InvalidOperationException("No BodyPresetPrototype was found with the name " + presetName + " while loading a BodyPreset!"); //Should never happen unless you fuck up the prototype.

                _template = new BodyTemplate(templateData);
                LoadBodyPreset(new BodyPreset(presetData));
            }
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
        ///     Disconnects the given BodyPart reference, potentially dropping other BodyParts if they were hanging off it. 
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
                _partDictionary.Remove(slotName);
                if (dropEntity)
                {
                    var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                    partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                }
            }
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
                _partDictionary.Remove(name);
                if (dropEntity)
                {
                    var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                    partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                }
            }
        }
    }
}
