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
using Robust.Shared.Interfaces.Serialization;
using Content.Shared.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.DamageSystem;
using Content.Shared.DamageSystem;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Server.BodySystem {

    /// <summary>
    ///     Component representing a collection of <see cref="BodyPart">BodyParts</see> attached to each other.
    /// </summary>
    [RegisterComponent]
    public class BodyManagerComponent : IDamageableComponent, IBodyPartContainer {

        public sealed override string Name => "BodyManager";
#pragma warning disable CS0649
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore

        [ViewVariables]
        private BodyTemplate _template;

        [ViewVariables]
        private Dictionary<string, BodyPart> _partDictionary = new Dictionary<string, BodyPart>();

        [ViewVariables]
        private Dictionary<Type, BodyNetwork> _networks = new Dictionary<Type, BodyNetwork>();

        /// <summary>
        ///     All <see cref="BodyPart">BodyParts</see> with <see cref="LegProperty">LegProperties</see> that are currently affecting movespeed, mapped to how big that leg they're on is.
        /// </summary>
        [ViewVariables]
        private Dictionary<BodyPart, float> _activeLegs = new Dictionary<BodyPart, float>();

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

        public override void ExposeData(ObjectSerializer serializer) {
            base.ExposeData(serializer);

            string templateName = null;
            serializer.DataField(ref templateName, "BaseTemplate", "bodyTemplate.Humanoid");
            if (serializer.Reading) {
                if (!_prototypeManager.TryIndex(templateName, out BodyTemplatePrototype templateData))
                    throw new InvalidOperationException("No BodyTemplatePrototype was found with the name " + templateName + " while loading a BodyTemplate!"); //Should never happen unless you fuck up the prototype.

                string presetName = null;
                serializer.DataField(ref presetName, "BasePreset", "bodyPreset.BasicHuman");
                if (!_prototypeManager.TryIndex(presetName, out BodyPresetPrototype presetData))
                    throw new InvalidOperationException("No BodyPresetPrototype was found with the name " + presetName + " while loading a BodyPreset!"); //Should never happen unless you fuck up the prototype.

                _template = new BodyTemplate(templateData);
                LoadBodyPreset(new BodyPreset(presetData));
            }
        }

        /// <summary>
        ///     Loads the given <see cref="BodyPreset"/> - forcefully changes all limbs found in both the preset and this template!
        /// </summary>
        public void LoadBodyPreset(BodyPreset preset) {
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
            OnBodyChanged();
        }

        /// <summary>
        ///     Changes the current <see cref="BodyTemplate"/> to the given <see cref="BodyTemplate"/>. Attempts to keep previous <see cref="BodyPart">BodyParts</see>
        ///     if there is a slot for them in both <see cref="BodyTemplate"/>.
        /// </summary>
        public void ChangeBodyTemplate(BodyTemplatePrototype newTemplate) {
            foreach (KeyValuePair<string, BodyPart> part in _partDictionary) {
                //TODO: Make this work.
            }
            OnBodyChanged();
        }

        /// <summary>
        ///     This function is called by <see cref="BodySystem"/> every tick.
        /// </summary>
        public void Tick(float frameTime)
        {
            foreach (var (key, value) in PartDictionary)
            {
                value.Tick(frameTime);
            }
        }

        /// <summary>
        ///     Called when the layout of this body changes.
        /// </summary>
        private void OnBodyChanged()
        {
            if (Owner.TryGetComponent(out MovementSpeedModifierComponent playerMover)) //Calculate movespeed based on this body.
            {
                _activeLegs.Clear();
                IEnumerable<BodyPart> legParts = Parts.Cast<BodyPart>().Where(x => x.HasProperty(typeof(LegProperty)));
                foreach (BodyPart part in legParts)
                {
                    float footDistance = DistanceToNearestFoot(this, part);
                    if (footDistance != float.MinValue)
                        _activeLegs.Add(part, footDistance);
                }
                CalculateSpeed();
            }
        }

        private void CalculateSpeed()
        {
            if (Owner.TryGetComponent(out MovementSpeedModifierComponent playerMover))
            {
                float speedSum = 0;
                foreach (var (key, value) in _activeLegs)
                {
                    if (!key.HasProperty<LegProperty>())
                        _activeLegs.Remove(key);
                }
                foreach (var (key, value) in _activeLegs)
                {
                    if (key.TryGetProperty<LegProperty>(out LegProperty legProperty))
                    {
                        speedSum += legProperty.Speed * (1 + (float) Math.Log(value, (double) 1024.0)); //Speed of a leg = base speed * (1+log1024(leg length))
                    }
                }
                if (speedSum <= 0.001f || _activeLegs.Count <= 0) //Case: no way of moving. Fall down.
                {
                    if (Owner.TryGetComponent(out StunnableComponent stunnable))
                    {
                        stunnable.Stun(1f);
                        stunnable.Knockdown(5f);
                    }
                    playerMover.BaseWalkSpeed = 0.8f;
                    playerMover.BaseSprintSpeed = 2.0f;
                }
                else //Case: have at least one leg. Set movespeed.
                {
                    playerMover.BaseWalkSpeed = speedSum / (_activeLegs.Count - (float) (Math.Log((double) _activeLegs.Count, (double) 4.0))); //Extra legs stack diminishingly. Final speed = speed sum/(leg count-log4(leg count))
                    playerMover.BaseSprintSpeed = playerMover.BaseWalkSpeed * 1.75f;
                }
            }
        }


        #region IDamageableComponent Implementation

        public override int TotalDamage => 0;

        public override List<DamageState> SupportedDamageStates => null;

        public override DamageState CurrentDamageState {
            get {
                return _currentDamageState;
            }
            protected set {
                _currentDamageState = value;
            }
        }
        private DamageState _currentDamageState;

        public override bool ChangeDamage(DamageType damageType, int amount, IEntity source, bool ignoreResistances, HealthChangeParams extraParams = null)
        {
            CurrentDamageState = DamageState.Dead;
            List<HealthChangeData> data = new List<HealthChangeData> { new HealthChangeData(damageType, 0, 0) };
            TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
            return true;
        }

        public override bool ChangeDamage(DamageClass damageClass, int amount, IEntity source, bool ignoreResistances, HealthChangeParams extraParams = null)
        {
            return true;
        }

        public override bool SetDamage(DamageType damageType, int newValue, IEntity source, HealthChangeParams extraParams = null)
        {
            return true;
        }

        public override void HealAllDamage()
        {
            return;
        }

        public override void ForceHealthChangedEvent()
        {
            return;
        }

        #endregion

        #region BodyPart Functions

        /// <summary>
        ///     Recursive search that returns whether a given <see cref="BodyPart"/> is connected to the center <see cref="BodyPart"/>.
        ///     Not efficient (O(n^2)), but most bodies don't have a ton of <see cref="BodyPart">BodyParts</see>.
        /// </summary>
        public bool ConnectedToCenterPart(BodyPart target)
        {
            List<string> searchedSlots = new List<string> { };
            if (!TryGetSlotName(target, out string result))
                return false;
            return ConnectedToCenterPartRecursion(searchedSlots, result);
        }
        private bool ConnectedToCenterPartRecursion(List<string> searchedSlots, string slotName)
        {
            TryGetBodyPart(slotName, out BodyPart part);
            if (part == null)
                return false;
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
        ///     Returns the central <see cref="BodyPart"/> of this body based on the <see cref="BodyTemplate"/>. For humans, this is the torso. Returns null if not found.
        /// </summary>
        public BodyPart GetCenterBodyPart()
        {
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
        public bool TryGetBodyPart(string slotName, out BodyPart result)
        {
            return _partDictionary.TryGetValue(slotName, out result);
        }

        /// <summary>
        ///     Grabs the slotName that the given <see cref="BodyPart"/> resides in. Returns true if the <see cref="BodyPart"/> is
        ///     part of this body and a slot is found, false otherwise. If false, result will be null.
        /// </summary>
        public bool TryGetSlotName(BodyPart part, out string result)
        {
            result = _partDictionary.FirstOrDefault(x => x.Value == part).Key; //We enforce that there is only one of each value in the dictionary, so we can iterate through the dictionary values to get the key from there.
            return result != null;
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
        public bool TryGetBodyPartConnections(string slotName, out List<string> connections)
        {
            return _template.Connections.TryGetValue(slotName, out connections);
        }

        /// <summary>
        ///     Grabs all occupied slots connected to the given slot, regardless of whether the given slotName is occupied. Returns true if successful, false if there was an error or no connected BodyParts were found.
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

        /// <summary>
        ///     Grabs all occupied slots connected to the given slot, regardless of whether the given slotName is occupied. Returns true if successful, false if there was an error or no connected BodyParts were found.
        /// </summary>
        public bool TryGetBodyPartConnections(BodyPart part, out List<BodyPart> result)
        {
            result = null;
            if (TryGetSlotName(part, out string slotName))
                return TryGetBodyPartConnections(slotName, out result);
            return false;
        }

        /// <summary>
        ///     Grabs all <see cref="BodyPart">BodyParts</see> of the given type in this body.
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
        ///     Installs the given <see cref="BodyPart"/> into the given slot. Returns true if successful, false otherwise.
        /// </summary>
        public bool InstallBodyPart(BodyPart part, string slotName)
        {
            if (!SlotExists(slotName)) //Make sure the given slot exists
                return false;
            if (TryGetBodyPart(slotName, out BodyPart result)) //And that nothing is in it
                return false;
            _partDictionary.Add(slotName, part);
            OnBodyChanged();
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
                OnBodyChanged();
                return partEntity;
            }
            return null;
        }

        /// <summary>
        /// Disconnects the given <see cref="BodyPart"/> reference, potentially dropping other <see cref="BodyPart">BodyParts</see> if they were hanging off it. 
        /// </summary>
        public void DisconnectBodyPart(BodyPart part, bool dropEntity)
        {
            if (!_partDictionary.ContainsValue(part))
                return;
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
                            DisconnectBodyPartByName(connectionName, dropEntity);
                        }
                    }
                }
                if (dropEntity)
                {
                    var partEntity = Owner.EntityManager.SpawnEntity("BaseDroppedBodyPart", Owner.Transform.GridPosition);
                    partEntity.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(part);
                }
                OnBodyChanged();
            }
        }

        /// <summary>
        ///     Internal string version of DisconnectBodyPart for performance purposes. Yes, it is actually more performant. 
        /// </summary>
        private void DisconnectBodyPartByName(string name, bool dropEntity)
        {
            if (!TryGetBodyPart(name, out BodyPart part))
                return;
            if (part != null)
            {
                _partDictionary.Remove(name);
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
                OnBodyChanged();
            }
        }

        #endregion

        #region BodyNetwork Functions

        /// <summary>
        ///     Attempts to add a <see cref="BodyNetwork"/> of the given type to this body. Returns true if successful, false
        ///     if there was an error (such as passing in an invalid type or a network of that type already existing).
        /// </summary>
        public bool AddBodyNetwork(Type networkType)
        {
            if (!networkType.IsSubclassOf(typeof(BodyNetwork)))
                return false;
            if (!_networks.ContainsKey(networkType))
            {
                BodyNetwork newNetwork = (BodyNetwork) Activator.CreateInstance(networkType);
                _networks.Add(networkType, newNetwork);
                newNetwork.OnCreate();
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Deletes the <see cref="BodyNetwork"/> of the given type in this body, if there is one.
        /// </summary>
        public void DeleteBodyNetwork(Type networkType)
        {
            _networks.Remove(networkType);
        }

        /// <summary>
        ///     Attempts to get the <see cref="BodyNetwork"/> of the given type in this body. Returns true if succesful, false
        ///     if not (result will be null in this case).
        /// </summary>
        public bool TryGetBodyNetwork(Type networkType, out BodyNetwork result)
        {
            return _networks.TryGetValue(networkType, out result);
        }

        #endregion

        #region Recursion Functions

        /// <summary>
        ///     Returns the combined length of the distance to the nearest <see cref="BodyPart"/> with a <see cref="FootProperty"/>. Returns <see cref="float.MinValue"/>
        ///     if there is no foot found. If you consider a <see cref="BodyManagerComponent"/> a node map, then it will look for a foot node from the given node. It can
        ///     only search through BodyParts with <see cref="ExtensionProperty"/>.
        /// </summary>
        private static float DistanceToNearestFoot(BodyManagerComponent body, BodyPart source)
        {
            if (source.HasProperty<FootProperty>() && source.TryGetProperty<ExtensionProperty>(out ExtensionProperty property))
                return property.ReachDistance;
            return LookForFootRecursion(body, source, new List<BodyPart>());
        }

        private static float LookForFootRecursion(BodyManagerComponent body, BodyPart current, List<BodyPart> searchedParts)
        {
            //This function is quite messy but it works as intended.
            if (current.TryGetProperty<ExtensionProperty>(out ExtensionProperty extProperty))
            { //If the current BodyPart has an ExtensionProperty...
                if (body.TryGetBodyPartConnections(current, out List<BodyPart> connections)) //Get all connected BodyParts...
                {
                    foreach (BodyPart connection in connections) //If a connected BodyPart is a foot, return this BodyPart's length.
                    {
                        if (!searchedParts.Contains(connection) && connection.HasProperty<FootProperty>())
                            return extProperty.ReachDistance;
                    }
                    List<float> distances = new List<float>();
                    foreach (BodyPart connection in connections) //Otherwise, get the recursion values of all connected BodyParts and store them in a list.
                    {
                        if (searchedParts.Contains(connection))
                        {
                            float result = LookForFootRecursion(body, connection, searchedParts);
                            if (result != float.MinValue)
                                distances.Add(result);
                        }
                    }
                    if (distances.Count > 0) //If one or more of the searches found a foot, return the smallest one and add this one's length.
                    {
                        return distances.Min<float>() + extProperty.ReachDistance;
                    }
                    else //There are no foots connected to this one, return false.
                    {
                        return float.MinValue;
                    }
                }
                else //No connections, return false.
                    return float.MinValue;
            }
            else
            { //No extensionproperty, no go.
                return float.MinValue;
            }
        }

        #endregion

    }

    public class BodyManagerHealthChangeParams : HealthChangeParams
    {

    }
}

