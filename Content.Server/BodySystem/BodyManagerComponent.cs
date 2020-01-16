
	enum BodyPartCompatability { mechanical, biological, universal };	
	enum BodyPartType { other, torso, head, arm, hand, leg, foot };	  

	public class BodyManagerComponent : Component {
		private BodyTemplate _template;
		private Dictionary<string, BodyPart> _parts;
		
		public BodyManagerComponent(BodyTemplate _template){
			this._template = _template;
		}
		
        /// <summary>
        ///     Loads the given preset - forcefully sets all limbs on the attached entity to match it!
        /// </summary>		
		public void LoadBodyPreset(BodyPreset preset){
			if(_template == null || preset.TemplateID != _template.ID); //Check that the preset's template and our template are equivalent.
				return;
			var prototypeManger = IoCManager.Resolve<IPrototypeManager>();
			foreach(KeyValuePair<BodyPartType, string> slot in _template.Slots){ //Create all the desired limbs.
				if(!preset.PartIDs.TryIndex(slot.value, out string partID)){
					return; //TODO: Throw error! Our template should match the preset template, as we JUST checked above!
				}
				if(!prototypeManger.TryIndex(partID, out BodyPart newPart)){
					return; //TODO: Throw error! The preset is using an non-existant BodyPart prototype!
				}
				_parts.Add(slotName, newPart);
			}
			foreach(KeyValuePair<string, string> connection in _template.Connections){ //Connect all the limbs together.
				GetBodyPartBySlotName(connection.key).ConnectTo(GetBodyPartBySlotName(connection.value));
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
			return _template.centerSlot;
		}
		
        /// <summary>
        ///     Disconnects the given body part, potentially disconnecting other body parts if they were hanging off it.
        /// </summary>
		public void DisconnectBodyPart(string name){
			BodyPart part = GetBodyPartBySlotName(name);
			_parts.Remove(part);
			part.DisconnectFromAll();
		}
		
		/// <summary>
        ///     Returns the body part currently in the given BodyTemplate slot (if there is one).
        /// </summary>		
		private BodyPart GetBodyPartBySlotName(string slotName){
			_parts.TryGetValue(slotName, out result);
			return result;
		}
		

	}