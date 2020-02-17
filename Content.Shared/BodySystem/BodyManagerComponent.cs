using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;

namespace Content.Shared.BodySystem {
    public enum BodyPartCompatibility { Mechanical, Biological, Universal };
    public enum BodyPartType { Other, Torso, Head, Arm, Hand, Leg, Foot };

}

namespace Content.Shared.BodySystem {

    [RegisterComponent]
    public class BodyManagerComponent : Component {

        #pragma warning disable CS0649
            [Dependency]
            private IPrototypeManager _prototypeManager;
        #pragma warning restore

        [ViewVariables]
        private BodyTemplate _template;
        [ViewVariables]
        private Dictionary<string, BodyPart> _parts = new Dictionary<string, BodyPart>();


        /// <summary>
        ///     The BodyTemplate that this BodyManagerComponent is adhering to.
        /// </summary>
        public BodyTemplate Template => _template;
        /// <summary>
        ///     Maps BodyTemplate slot name to the BodyPart object filling it (if there is one). 
        /// </summary>
        public Dictionary<string, BodyPart> Parts => _parts;
        public sealed override string Name => "BodyManager";

        public BodyManagerComponent() {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        }

        public override void ExposeData(ObjectSerializer serializer) {
            base.ExposeData(serializer);

            string templateName = "";
            serializer.DataField(ref templateName, "BaseTemplate", "bodyTemplate.Humanoid");
            if(!_prototypeManager.TryIndex(templateName, out BodyTemplatePrototype templateData))
                throw new InvalidOperationException("No BodyTemplatePrototype was found with the name " + templateName + " while loading a BodyTemplate!"); //Should never happen unless you fuck up the prototype.

            string presetName = "";
            serializer.DataField(ref presetName, "BasePreset", "bodyPreset.BasicHuman");
            if(!_prototypeManager.TryIndex(presetName, out BodyPresetPrototype presetData))
                throw new InvalidOperationException("No BodyPresetPrototype was found with the name " + presetName + " while loading a BodyPreset!"); //Should never happen unless you fuck up the prototype.

            _template = new BodyTemplate(templateData);
            LoadBodyPreset(new BodyPreset(presetData));
        }


        /// <summary>
        ///     Loads the given preset - forcefully changes all limbs found in both the preset and this template!
        /// </summary>		
        public void LoadBodyPreset(BodyPreset preset) {
            foreach (var(slotName, type) in _template.Slots) {
                if (!preset.PartIDs.TryGetValue(slotName, out string partID)) { //For each slot in our BodyManagerComponent's template, try and grab what the ID of what the preset says should be inside it.
                    continue; //If the preset doesn't define anything for it, continue. 
                }
                if (!_prototypeManager.TryIndex(partID, out BodyPartPrototype newPartData)) { //Get the BodyPartPrototype corresponding to the BodyPart ID we grabbed.
                    throw new InvalidOperationException("BodyPart prototype with ID " + partID + " could not be found!");
                }
                _parts.Remove(slotName); //Try and remove an existing limb if that exists. 
                _parts.Add(slotName, new BodyPart(newPartData)); //Add a new BodyPart with the BodyPartPrototype as a baseline to our BodyComponent.
            }
        }

        /// <summary>
        ///     Changes the current BodyTemplate to the new BodyTemplate. Attempts to keep previous BodyParts if there is a slot for them in both BodyTemplates.
        /// </summary>				
        public void ChangeBodyTemplate(BodyTemplatePrototype newTemplate) {
            foreach (KeyValuePair<string, BodyPart> part in _parts) {
                //TODO: Make this work.
            }
        }

        /// <summary>
        ///     Performs a check to change the parent Entity's species based on what limbs it has. 
        /// </summary>
        public void SpeciesCheck()
        {
            throw new NotImplementedException();
            //TODO: Make this work.
        }

        /// <summary>
        ///     Returns the central BodyPart of this body based on the BodyTemplate. For humans, this is the torso.
        /// </summary>			
        public BodyPart GetCenterBodyPart() {
            _parts.TryGetValue(_template.CenterSlot, out BodyPart center);
            return center;
        }




        /// <summary>
        ///     Disconnects the given BodyPart, potentially dropping other BodyParts if they were hanging off it.
        /// </summary>
        public void DisconnectBodyPart(string name, bool dropEntity) {
            TryGetLimb(name, out BodyPart part);
            if(part != null) {
                _parts.Remove(name);
                if (TryGetLimbConnections(name, out List<string> connections)) //Call disconnect on all limbs that were hanging off this limb.
                {
                    foreach (string connectionName in connections) //This loop is an unoptimized travesty. TODO: optimize to be less shit
                    { 
                        if (TryGetLimb(connectionName, out BodyPart result) && !ConnectedToCenterPart(result))
                        {
                            DisconnectBodyPart(connectionName, dropEntity);
                        }
                    }
                }
                if(dropEntity)
                {
                    //TODO: Add limb entities and make this pseudocode work
                    //BodyPartEntity partEntity = Owner.EntityManager.SpawnEntityAt(id, _parent.Owner.Transform.GridPosition);
                    //partEntity.BodyPartData = this;
                    //_parent = null;
                }
            }
        }

        /// <summary>
        ///     Recursive search that returns whether a given BodyPart is connected to the center BodyPart. Not efficient (O(n^2)), but most bodies don't have a ton of BodyParts.
        /// </summary>	
        private bool ConnectedToCenterPart(BodyPart target) 
        {
            //TODO: optimize by adding limb search tree with the center BodyPart at top
            List<BodyPart> searchedParts = new List<BodyPart> { target };
            return ConnectedToCenterPartRecursion();
        }
        private bool ConnectedToCenterPartRecursion()
        {
            return false;
        }

        /// <summary>
        ///     Grabs the BodyPart in the given slotName if there is one. Returns true if a BodyPart is found, false otherwise.
        /// </summary>		
        public bool TryGetLimb(string slotName, out BodyPart result) {
            return _parts.TryGetValue(slotName, out result);
        }

        /// <summary>
        ///     Grabs the names of all connected slots to the given slotName from the template. Returns true if connections are found to the slotName, false otherwise. 
        /// </summary>	
        public bool TryGetLimbConnections(string slotName, out List<string> connections)
        {
            return _template.Connections.TryGetValue(slotName, out connections);
        }

    }
}
