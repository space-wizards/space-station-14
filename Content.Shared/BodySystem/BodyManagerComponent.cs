using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;

namespace Content.Shared.BodySystem {
    public enum BodyPartCompatability { Mechanical, Biological, Universal };
    public enum BodyPartType { Other, Torso, Head, Arm, Hand, Leg, Foot };

}

namespace Content.Shared.BodySystem {
    [RegisterComponent]
    public class BodyManagerComponent : Component {

        [ViewVariables(VVAccess.ReadWrite)]
        private BodyTemplate _template;

        [ViewVariables(VVAccess.ReadWrite)]
        private Dictionary<string, BodyPart> _parts = new Dictionary<string, BodyPart>();

        private IPrototypeManager _prototypeManager;

        public sealed override string Name => "BodyManager";

        public override void ExposeData(ObjectSerializer serializer) {
            base.ExposeData(serializer);
            string templateName = "";
            serializer.DataField(ref templateName, "BaseTemplate", "bodyTemplate.Humanoid");
            if(_prototypeManager.TryIndex(templateName, out BodyTemplate templateData))
                Startup(templateData);
            else
                throw new InvalidOperationException("No BodyTemplate was found with the name " + templateName + " while loading a new BodyTemplate!"); //Should never happen unless you fuck up the prototype.
        }

        public BodyManagerComponent() {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        }

        public void Startup(BodyTemplate _template) {
            this._template = _template;

        }

        /// <summary>
        ///     Loads the given preset - forcefully sets all limbs on the attached entity to match it!
        /// </summary>		
        public void LoadBodyPreset(BodyPreset preset) {
            if (_template == null || preset.TemplateID != _template.ID) //Check that the preset's template and our template are equivalent.
                return;
            foreach (var(slotName, type) in _template.Slots) { //Iterate through all limb slots in the template.
                if (!preset.PartIDs.TryGetValue(slotName, out string partID)) { //Get the BodyPart ID that the preset defines.
                    throw new InvalidOperationException("Current template and preset template are not equivalent!"); //This should never happen - we just checked that the templates are equal above.

                }
                if (!_prototypeManager.TryIndex(partID, out BodyPartPrototype newPartData)) { //Get the BodyPart corresponding to the BodyPart ID.
                    throw new InvalidOperationException("BodyPart prototype with ID " + partID + " does not exist!"); //If this happens, that means the prototype with this BodyPart ID doesn't exist in Resources. Which should never happen unless you change shit or fucked up the prototype. 
                }
                _parts.Add(slotName, new BodyPart(newPartData)); //Add it to our BodyParts.
            }
            foreach(var(firstID, connectionsList) in _template.Connections) { //Connect all the BodyParts together.
                foreach (var secondID in connectionsList) {
                    if (TryGetLimb(firstID, out BodyPart first) && TryGetLimb(secondID, out BodyPart second))
                        first.ConnectTo(second);
                }
            }
        }


        /// <summary>
        ///     Changes the current BodyTemplate to the new BodyTemplate. Attempts to keep previous BodyParts if there is a slot for them in both BodyTemplates.
        /// </summary>				
        public void ChangeBodyTemplate(BodyTemplate newTemplate) {
            foreach (KeyValuePair<string, BodyPart> part in _parts) {
                //TODO: Make this work.
            }
        }

        /// <summary>
        ///     Returns the central BodyPart of this body based on the BodyTemplate. For humans, this is the torso.
        /// </summary>			
        public BodyPart GetCenterBodyPart() {
            _parts.TryGetValue(_template.CenterSlot, out BodyPart center);
            return center;
        }

        /// <summary>
        ///     Disconnects the given BodyPart, potentially disconnecting other BodyParts if they were hanging off it.
        /// </summary>
        public void DisconnectBodyPart(string name) {
            TryGetLimb(name, out BodyPart part);
            if (part != null) {
                _parts.Remove(name);
                part.DisconnectFromAll();
            }
        }

        /// <summary>
        ///     Performs a check to change the parent Entity's species based on what limbs it has.
        /// </summary>
        public void SpeciesCheck() {
            //TODO: Make this owrk.
        }

        /// <summary>
        ///     Grabs the BodyPart in the given slotName if there is one. Returns true if a BodyPart is found, false otherwise.
        /// </summary>		
        public bool TryGetLimb(string slotName, out BodyPart result) {
            return _parts.TryGetValue(slotName, out result);
        }
    }
}
