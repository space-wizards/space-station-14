using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.SecurityCamera
{
    [Serializable, NetSerializable]
    public class SecurityCameraConnectEvent : EntityEventArgs
    {
        public EntityUid User {get;}
        public Dictionary<int,EntityUid> CameraList {get;}
    
        public SecurityCameraConnectEvent(EntityUid user, Dictionary<int,EntityUid> cameraList)
        {
            User = user;
            CameraList = cameraList;
        }
    }

    [Serializable, NetSerializable]
    public class SecurityCameraDisconnectEvent : EntityEventArgs
    {
        public EntityUid User {get;}

        public SecurityCameraDisconnectEvent(EntityUid user){User = user;}
    }
}