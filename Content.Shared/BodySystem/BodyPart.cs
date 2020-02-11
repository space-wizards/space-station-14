using Robust.Shared.Interfaces.Serialization;
using System;
using System.Collections.Generic;
    public enum BodyPartCompatability { Mechanical, Biological, Universal };
    public enum BodyPartType { Other, Torso, Head, Arm, Hand, Leg, Foot };


namespace Content.Shared.BodySystem {


    public class BodyPart {
        private List<BodyPart> _connections;
        private BodyManagerComponent _parent;

        private string ID;
        private string Name;
        private string Plural;
        private BodyPartType Type;
        private int Durability;
        private float CurrentDurability;
        private int DestroyThreshold;
        private float Resistance;
        private int Size;
        private BodyPartCompatability _compatability;
        private List<IExposeData> _properties;




        public BodyPart() {
            _connections = new List<BodyPart>();
        }

        public BodyPart(BodyPartPrototype data) {
            LoadPrototype(data);
        }

        /// <summary>
        ///    Loads the given BodyPartPrototype - forcefully sets this limb to match it!
        /// </summary>	
        public void LoadPrototype(BodyPartPrototype data) {
            ID = data.ID;
            Name = data.Name;
            Plural = data.Plural;
            Type = data.PartType;
            Durability = data.Durability;
            CurrentDurability = Durability; //Starts at full HP.
            DestroyThreshold = data.DestroyThreshold;
            Resistance = data.Resistance;
            Size = data.Size;
            _compatability = data.Compatability;
            _properties = (List<IExposeData>)data.Properties;

        }

        /// <summary>
        ///     Sets a reference to the parent BodyManagerComponent. You likely don't have to touch this: BodyManagerComponent functions that modify limbs already handle it.
        /// </summary>					
        public void SetParent(BodyManagerComponent _parent) {
            this._parent = _parent;
        }






        /// <summary>
        ///     Returns the current durability of this limb.
        /// </summary>	
		public float GetDurability() {
            return Durability;
        }

        /// <summary>
        ///     Heals the durability of this limb by the given amount. Only heals up to its max.
        /// </summary>	
        public void HealDamage(float heal) {
            Math.Clamp(CurrentDurability + heal, int.MinValue, Durability);
            DurabilityCheck();
        }

        /// <summary>
        ///     Damages this limb, potentially breaking or destroying it.
        /// </summary>	
        public void DealDamage(float dmg) {
            CurrentDurability -= dmg;
            DurabilityCheck();
        }

        private void DurabilityCheck() {
            if (CurrentDurability <= DestroyThreshold) {
                //Destroy
                DisconnectFromAll();
            }
            else if (CurrentDurability <= 0) {
                //Be broken
            }
            else {
                //Be normal
            }
        }







        /// <summary>
        ///     Forms a connection to the given BodyPart.
        /// </summary>	
        public void ConnectTo(BodyPart other) {
            if (!_connections.Contains(other)) {
                _connections.Add(other);
                other.ConnectTo(this);
                ConnectionCheck();
            }
        }


        /// <summary>
        ///     Disconnects from the given BodyPart.
        /// </summary>	
        public void DisconnectFrom(BodyPart other) {
            if (_connections.Contains(other)) {
                _connections.Remove(other);
                other.DisconnectFrom(this);
                ConnectionCheck();
            }
        }

        /// <summary>
        ///     Disconnects from all attached BodyPart. Drops this BodyPart on the ground and possibly other BodyPart with it.
        /// </summary>	
        public void DisconnectFromAll() {
            foreach (BodyPart part in _connections) {
                DisconnectFrom(part);
            }
        }

        /// <summary>
        ///     Checks whether there's anything for this BodyPart to hang off of, then falls to the ground if there is not.
        /// </summary>			
        private void ConnectionCheck() {
            if (_parent == null)
                return;
            if (_connections.Count == 0 || !ConnectedToCenterPart(new List<BodyPart>())) {
                //BodyPartEntity partEntity = _parent.Owner.EntityManager.SpawnEntityAt(id, _parent.Owner.Transform.GridPosition); //Spawn a physical limb entity
                //partEntity.BodyPartData = this;
                //_parent = null;
            }
        }

        /// <summary>
        ///     Recursive search that returns whether this BodyPart is connected to the _parent's center BodyPart. Not efficient (O(n^2)), but most bodies don't have a ton of BodyParts.
        /// </summary>	
        private bool ConnectedToCenterPart(List<BodyPart> searchedParts) {
            if (_parent == null)
                return false;
            searchedParts.Add(this);
            foreach (BodyPart connection in _connections) {
                if (connection == _parent.GetCenterBodyPart())
                    return true;
                else if (!searchedParts.Contains(connection))
                    if (connection.ConnectedToCenterPart(searchedParts))
                        return true;
            }
            return false;
        }
    }
}
