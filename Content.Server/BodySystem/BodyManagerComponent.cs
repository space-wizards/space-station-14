using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Robust.Shared.BodySystem {
    public enum BodyPartCompatability { mechanical, biological, universal };	
	public enum BodyPartType { other, torso, head, arm, hand, leg, foot };	  

	public class BodyManagerComponent : Component {
		private BodyTemplate _template;
		private Dictionary<string, BodyPart> _parts;

        public override string Name => "BodyManager";

        public override void ExposeData(ObjectSerializer serializer) {
            base.ExposeData(serializer);
            //serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }

        public BodyManagerComponent(BodyTemplate _template){
			this._template = _template;
            _parts = new Dictionary<string, BodyPart>();
		}
		
        /// <summary>
        ///     Loads the given preset - forcefully sets all limbs on the attached entity to match it!
        /// </summary>		
		public void LoadBodyPreset(BodyPreset preset){
			if(_template == null || preset.TemplateID != _template.ID) //Check that the preset's template and our template are equivalent.
				return;
			var prototypeManger = IoCManager.Resolve<IPrototypeManager>();
			foreach(KeyValuePair<BodyPartType, string> slot in _template.Slots){ //Iterate through all limb slots in the template.
				if(!preset.PartIDs.TryGetValue(slot.Value, out string partID)){ //Get the BodyPart ID that the preset defines.
					return; 
				}
				if(!prototypeManger.TryIndex(partID, out BodyPart newPart)){ //Get the BodyPart corresponding to the BodyPart ID.
					return;
				}
				_parts.Add(slot.Value, newPart); //Add it to our BodyParts.
			}
			foreach(KeyValuePair<string, string> connection in _template.Connections){ //Connect all the BodyParts together.
				GetBodyPartBySlotName(connection.Key).ConnectTo(GetBodyPartBySlotName(connection.Value));
			}
		}
		
        /// <summary>
        ///     Changes the current BodyTemplate to the new BodyTemplate. Attempts to keep previous BodyParts if there is a slot for them in both BodyTemplates.
        /// </summary>				
		public void ChangeBodyTemplate(BodyTemplate newTemplate){
			foreach(KeyValuePair<string, BodyPart> part in _parts){
				//TODO: Make this work.
			}
		}
		
        /// <summary>
        ///     Returns the central BodyPart of this body based on the BodyTemplate. For humans, this is the torso.
        /// </summary>			
		public BodyPart GetCenterBodyPart(){
			_parts.TryGetValue(_template.CenterSlot, out BodyPart center);
            return center;
		}
		
        /// <summary>
        ///     Disconnects the given BodyPart, potentially disconnecting other BodyParts if they were hanging off it.
        /// </summary>
		public void DisconnectBodyPart(string name){
			BodyPart part = GetBodyPartBySlotName(name);
            if(part != null) { 
			    _parts.Remove(name);
			    part.DisconnectFromAll();
            }
		}
		
		/// <summary>
        ///     Returns the BodyPart currently in the given BodyTemplate slot name (if there is none, returns null).
        /// </summary>		
		private BodyPart GetBodyPartBySlotName(string slotName){
			_parts.TryGetValue(slotName, out BodyPart result);
			return result;
		}
		

	}
}
