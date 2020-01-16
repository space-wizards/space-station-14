using System;
using System.Collections.Generic;


namespace Robust.Shared.BodySystem {


    public partial class BodyPart {
		private List<BodyPart> _connections;
		private BodyManagerComponent _parent;
		public BodyPart(){
			_connections = new List<BodyPart>();
		}
		
        /// <summary>
        ///     Sets a reference to the parent BodyManagerComponent. You shouldn't have to touch this: BodyManagerComponent has functions to add limbs.
        /// </summary>					
		public void SetParent(BodyManagerComponent _parent){ 
			this._parent = _parent;
		}






        /// <summary>
        ///     Returns the current durability of this limb.
        /// </summary>	
		public float GetDurability(){
			return _durability;
		}
		        
		/// <summary>
        ///     Heals the durability of this limb by the given amount. Only heals up to its max.
        /// </summary>	
		public void HealDamage(float heal){
			Math.Clamp(_currentDurability+heal,int.MinValue,_durability); 
			DurabilityCheck();
		}
		
		/// <summary>
        ///     Damages this limb, potentially breaking or destroying it.
        /// </summary>	
		public void DealDamage(float dmg){
			_currentDurability -= dmg;
			DurabilityCheck();
		}
		
		private void DurabilityCheck(){
			if(_currentDurability <= _destroyThreshold){
				//Destroy
				DisconnectFromAll();
			}
			else if(_currentDurability <= 0){
				//Be broken
			}
			else{
				//Be normal
			}
		}
		
		
		
		
		
		
		
		/// <summary>
        ///     Forms a connection to the given BodyPart.
        /// </summary>	
		public void ConnectTo(BodyPart other){
			if(!_connections.Contains(other)){
				_connections.Add(other);
				other.FinishConnect(this);
				ConnectionCheck();
			}
		}
		public void FinishConnect(BodyPart other){
			if(!_connections.Contains(other)){
				_connections.Add(other);
				ConnectionCheck();
			}
		}
		
		
		/// <summary>
        ///     Disconnects from the given BodyPart.
        /// </summary>	
		public void DisconnectFrom(BodyPart other){
			if(_connections.Contains(other)){
				_connections.Remove(other);
				other.FinishConnect(this);
				ConnectionCheck();
			}			
		}
		public void FinishDisconnect(BodyPart other){
			if(_connections.Contains(other)){
				_connections.Remove(other);
				ConnectionCheck();
			}
		}
		
		/// <summary>
        ///     Disconnects from all attached BodyPart. Drops this BodyPart on the ground and possibly other BodyPart with it.
        /// </summary>	
		public void DisconnectFromAll(){
			foreach(BodyPart part in _connections){
				DisconnectFrom(part);
			}
		}
		
		/// <summary>
        ///     Checks whether there's anything for this BodyPart to hang off of, then falls to the ground if there is not.
        /// </summary>			
		private void ConnectionCheck(){
			if(_parent == null)
				return;
			if(_connections.Count == 0 || !ConnectedToCenterPart(new List<BodyPart>())){
				//BodyPartEntity partEntity = _parent.Owner.EntityManager.SpawnEntityAt(id, _parent.Owner.Transform.GridPosition); //Spawn a physical limb entity
				//partEntity.BodyPartData = this;
				//_parent = null;
			}
		}
		
		/// <summary>
        ///     Recursive search that returns whether this BodyPart is connected to the _parent's center BodyPart. Not efficient, but most bodies don't have a ton of BodyParts.
        /// </summary>	
		private bool ConnectedToCenterPart(List<BodyPart> searchedParts){
			searchedParts.Add(this);
			foreach(BodyPart connection in _connections){
				if(connection == _parent.GetCenterBodyPart())
					return true;
				else if(!searchedParts.Contains(connection))
					if(connection.ConnectedToCenterPart(searchedParts))
						return true;
			}
			return false;
		}
    }
}
