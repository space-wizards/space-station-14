using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;
using System.Linq;
using Content.Server.Interfaces.GameObjects.Components.Interaction;

namespace Content.Server.BodySystem {

    /// <summary>
    ///     Component representing a collection of <see cref="BodyPart">BodyParts</see> attached to each other.
    /// </summary>
    [RegisterComponent]
    public class BodyManagerComponent : Component, IBodyPartContainer {

        public sealed override string Name => "BodyManager";
#pragma warning disable CS0649
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore

        [ViewVariables]
        private BodyTemplate _template;

        [ViewVariables]
        private string _presetName;

        [ViewVariables]
        private Dictionary<string, BodyPart> _partDictionary = new Dictionary<string, BodyPart>();

        /// <summary>
        ///     The <see cref="BodyTemplate"/> that this BodyManagerComponent is adhering to.
        /// </summary>
        public BodyTemplate Template => _template;

        /// <summary>
        ///     Maps <see cref="BodyTemplate"/> slot name to the <see cref="BodyPart"/> object filling it (if there is one).
        /// </summary>
        public Dictionary<string, BodyPart> PartDictionary => _partDictionary;

        /// <summary>
        ///     List of all occupied slots in this body, taken from the values of _parts.
        /// </summary>
        public IEnumerable<string> AllSlots
        {
            get
            {
                return _template.Slots.Keys;
            }
        }

        /// <summary>
        ///     List of all occupied slots in this body, taken from the values of _parts.
        /// </summary>
        public IEnumerable<string> OccupiedSlots
        {
            get
            {
                return _partDictionary.Keys;
            }
        }

        /// <summary>
        ///     List of all <see cref="BodyPart">BodyParts</see> in this body, taken from the keys of _parts.
        /// </summary>
        public IEnumerable<BodyPart> Parts {
            get {
                return _partDictionary.Values;
            }
        }

        /// <summary>
        ///     Recursive search that returns whether a given <see cref="BodyPart"/> is connected to the center <see cref="BodyPart"/>.
        ///     Not efficient (O(n^2)), but most bodies don't have a ton of <see cref="BodyPart">BodyParts</see>.
        /// </summary>
        public bool ConnectedToCenterPart(BodyPart target) {
            List<string> searchedSlots = new List<string> { };
            if (TryGetSlotName(target, out string result))
                return false;
            return ConnectedToCenterPartRecursion(searchedSlots, result);
        }
        private bool ConnectedToCenterPartRecursion(List<string> searchedSlots, string slotName) {
            TryGetBodyPart(slotName, out BodyPart part);
            if (part != null && part == GetCenterBodyPart())
                return true;
            searchedSlots.Add(slotName);
            if (TryGetBodyPartConnections(slotName, out List<string> connections)) {
                foreach (string connection in connections) {
                    if (!searchedSlots.Contains(connection) && ConnectedToCenterPartRecursion(searchedSlots, connection))
                        return true;
                }
            }
            return false;

        }

        /// <summary>
        ///     Returns the central <see cref="BodyPart"/> of this body based on the <see cref="BodyTemplate"/>. For humans, this is the torso. Returns null if not found.
        /// </summary>
        public BodyPart GetCenterBodyPart() {
            _partDictionary.TryGetValue(_template.CenterSlot, out BodyPart center);
            return center;
        }

        /// <summary>
        ///     Returns whether the given slot name exists within the current <see cref="BodyTemplate"/>.
        /// </summary>
        public bool SlotExists(string slotName)
        {
            return _template.SlotExists(slotName);
        }


        /// <summary>
        ///     Grabs the <see cref="BodyPart"/> in the given slotName if there is one. Returns true if a <see cref="BodyPart"/> is found,
        ///     false otherwise. If false, result will be null.
        /// </summary>
        public bool TryGetBodyPart(string slotName, out BodyPart result) {
            return _partDictionary.TryGetValue(slotName, out result);
        }

        /// <summary>
        ///     Grabs the slotName that the given <see cref="BodyPart"/> resides in. Returns true if the <see cref="BodyPart"/> is
        ///     part of this body and a slot is found, false otherwise. If false, result will be null.
        /// </summary>
        public bool TryGetSlotName(BodyPart part, out string result) {
            result = _partDictionary.FirstOrDefault(x => x.Value == part).Key; //We enforce that there is only one of each value in the dictionary, so we can iterate through the dictionary values to get the key from there.
            return result == null;
        }

        /// <summary>
        ///     Grabs the <see cref="BodyPartType"/> of the given slotName if there is one. Returns true if the slot is found, false otherwise. If false, result will be null.
        /// </summary>
        public bool TryGetSlotType(string slotName, out BodyPartType result)
        {
            return _template.Slots.TryGetValue(slotName, out result);
        }

        /// <summary>
        ///     Grabs the names of all connected slots to the given slotName from the template. Returns true if connections are found to the slotName, false otherwise. If false, connections will be null.
        /// </summary>
        public bool TryGetBodyPartConnections(string slotName, out List<string> connections) {
            return _template.Connections.TryGetValue(slotName, out connections);
        }

        /// <summary>
        ///     Grabs all occupied slots connected to the given slot, regardless of whether the target slot is occupied. Returns true if successful, false if there was an error or no connected BodyParts were found.
        /// </summary>
        public bool TryGetBodyPartConnections(string slotName, out List<BodyPart> result)
        {
            result = null;
            if (!_template.Connections.TryGetValue(slotName, out List<string> connections))
                return false;
            List<BodyPart> toReturn = new List<BodyPart>();
            foreach (string connection in connections)
            {
                if (TryGetBodyPart(connection, out BodyPart bodyPartResult))
                {
                    toReturn.Add(bodyPartResult);
                }
            }
            if (toReturn.Count <= 0)
                return false;
            result = toReturn;
            return true;
        }







        /////////
        /////////  Server-specific stuff
        /////////

        public override void ExposeData(ObjectSerializer serializer) {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "BaseTemplate",
                "bodyTemplate.Humanoid",
                template =>
                {
                    if (!_prototypeManager.TryIndex(template, out BodyTemplatePrototype templateData))
                    {
                        throw new InvalidOperationException("No BodyTemplatePrototype was found with the name " + template + " while loading a BodyTemplate!"); //Should never happen unless you fuck up the prototype.
                    }

                    _template = new BodyTemplate(templateData);
                },
                () => _template.Name);

            serializer.DataReadWriteFunction(
                "BasePreset",
                "bodyPreset.BasicHuman",
                preset =>
                {
                    if (!_prototypeManager.TryIndex(preset, out BodyPresetPrototype presetData))
                    {
                        throw new InvalidOperationException("No BodyPresetPrototype was found with the name " + preset + " while loading a BodyPreset!"); //Should never happen unless you fuck up the prototype.
                    }

                    LoadBodyPreset(new BodyPreset(presetData));
                },
                () => _presetName);
        }

        /// <summary>
        ///     Loads the given <see cref="BodyPreset"/> - forcefully changes all limbs found in both the preset and this template!
        /// </summary>
        public void LoadBodyPreset(BodyPreset preset)
        {
            _presetName = preset.Name;

            foreach (var (slotName, type) in _template.Slots) {
                if (!preset.PartIDs.TryGetValue(slotName, out string partID)) { //For each slot in our BodyManagerComponent's template, try and grab what the ID of what the preset says should be inside it.
                    continue; //If the preset doesn't define anything for it, continue.
                }
                if (!_prototypeManager.TryIndex(partID, out BodyPartPrototype newPartData)) { //Get the BodyPartPrototype corresponding to the BodyPart ID we grabbed.
                    throw new InvalidOperationException("BodyPart prototype with ID " + partID + " could not be found!");
                }
                _partDictionary.Remove(slotName); //Try and remove an existing limb if that exists.
                _partDictionary.Add(slotName, new BodyPart(newPartData)); //Add a new BodyPart with the BodyPartPrototype as a baseline to our BodyComponent.
            }
        }

        /// <summary>
        ///     Changes the current <see cref="BodyTemplate"/> to the given <see cref="BodyTemplate"/>. Attempts to keep previous <see cref="BodyPart">BodyParts</see>
        ///     if there is a slot for them in both <see cref="BodyTemplate"/>.
        /// </summary>
        public void ChangeBodyTemplate(BodyTemplatePrototype newTemplate) {
            foreach (KeyValuePair<string, BodyPart> part in _partDictionary) {
                //TODO: Make this work.
            }
        }

        /// <summary>
        ///     Grabs all <see cref="BodyPart">BodyParts</see> of the given type in this body.
        /// </summary>
        public List<BodyPart> GetBodyPartsOfType(BodyPartType type) {
            List<BodyPart> toReturn = new List<BodyPart>();
            foreach (var (slotName, bodyPart) in _partDictionary) {
                if (bodyPart.PartType == type)
                    toReturn.Add(bodyPart);
            }
            return toReturn;
        }



        /// <summary>
        ///     Installs the given <see cref="BodyPart"/> into the given slot. Returns true if successful, false otherwise.
        /// </summary>
        public bool InstallBodyPart(BodyPart part, string slotName)
        {
            if (!SlotExists(slotName)) //Make sure the given slot exists
                return false;
            if (TryGetBodyPart(slotName, out BodyPart result)) //And that nothing is in it
                return false;
            _partDictionary.Add(slotName, part);
            BodyPartAdded(part, slotName);

            return true;
        }
        /// <summary>
        ///     Installs the given <see cref="DroppedBodyPartComponent"/> into the given slot, deleting the <see cref="IEntity"/> afterwards. Returns true if successful, false otherwise.
        /// </summary>
        public bool InstallDroppedBodyPart(DroppedBodyPartComponent part, string slotName)
        {
            if (!InstallBodyPart(part.ContainedBodyPart, slotName))
                return false;
            part.Owner.Delete();
            return true;
        }



        /// <summary>
        ///     Disconnects the given <see cref="BodyPart"/> reference, potentially dropping other <see cref="BodyPart">BodyParts</see>
        ///     if they were hanging off it. Returns the IEntity representing the dropped BodyPart.
        /// </summary>
        public IEntity DropBodyPart(BodyPart part)
        {
            if (!_partDictionary.ContainsValue(part))
                return null;
            if (part != null)
            {
                string slotName = _partDictionary.FirstOrDefault(x => x.Value == part).Key;
                _partDictionary.Remove(slotName);
                if (TryGetBodyPartConnections(slotName, out List<string> connections)) //Call disconnect on all limbs that were hanging off this limb.
                {
                    foreach (string connectionName in connections) //This loop is an unoptimized travesty. TODO: optimize to be less shit
                    {
                        if (TryGetBodyPart(connectionName, out BodyPart result) && !ConnectedToCenterPart(result))
                        {
                            DisconnectBodyPartByName(connectionName, true);
                        }
                    }
                }
                var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                return partEntity;
            }
            return null;
        }

        /// <summary>
        /// Disconnects the given <see cref="BodyPart"/> reference, potentially dropping other <see cref="BodyPart">BodyParts</see> if they were hanging off it.
        /// </summary>
        public void DisconnectBodyPart(BodyPart part, bool dropEntity) {
            if (!_partDictionary.ContainsValue(part))
                return;
            if (part != null) {
                string slotName = _partDictionary.FirstOrDefault(x => x.Value == part).Key;
                if (_partDictionary.Remove(slotName, out var partRemoved))
                {
                    BodyPartRemoved(partRemoved, slotName);
                }

                if (TryGetBodyPartConnections(slotName, out List<string> connections)) //Call disconnect on all limbs that were hanging off this limb.
                {
                    foreach (string connectionName in connections) //This loop is an unoptimized travesty. TODO: optimize to be less shit
                    {
                        if (TryGetBodyPart(connectionName, out BodyPart result) && !ConnectedToCenterPart(result)) {
                            DisconnectBodyPartByName(connectionName, dropEntity);
                        }
                    }
                }
                if (dropEntity) {
                    var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                    partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                }
            }
        }

        /// <summary>
        ///     Internal string version of DisconnectBodyPart for performance purposes. Yes, it is actually more performant.
        /// </summary>
        private void DisconnectBodyPartByName(string name, bool dropEntity) {
            if (!TryGetBodyPart(name, out BodyPart part))
                return;
            if (part != null) {
                if (_partDictionary.Remove(name, out var partRemoved))
                {
                    BodyPartRemoved(partRemoved, name);
                }

                if (TryGetBodyPartConnections(name, out List<string> connections)) {
                    foreach (string connectionName in connections) {
                        if (TryGetBodyPart(connectionName, out BodyPart result) && !ConnectedToCenterPart(result)) {
                            DisconnectBodyPartByName(connectionName, dropEntity);
                        }
                    }
                }
                if (dropEntity) {
                    var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                    partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                }
            }
        }

        private void BodyPartAdded(BodyPart part, string slotName)
        {
            var argsAdded = new BodyPartAddedEventArgs(part, slotName);

            foreach (var component in Owner.GetAllComponents<IBodyPartAdded>().ToArray())
            {
                component.BodyPartAdded(argsAdded);
            }
        }

        private void BodyPartRemoved(BodyPart part, string slotName)
        {
            var args = new BodyPartRemovedEventArgs(part, slotName);

            foreach (var component in Owner.GetAllComponents<IBodyPartRemoved>())
            {
                component.BodyPartRemoved(args);
            }
        }
    }
}
