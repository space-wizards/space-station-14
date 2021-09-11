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
        public EntityUid Console {get;}
        public Dictionary<int,EntityUid> CameraList {get;}
    
        public SecurityCameraConnectEvent(EntityUid user, Dictionary<int,EntityUid> cameraList,EntityUid console)
        {
            User = user;
            Console = console;
            CameraList = cameraList;
        }
    }

    [Serializable, NetSerializable]
    public class SecurityCameraDisconnectEvent : EntityEventArgs
    {
        public EntityUid User {get;}

        public SecurityCameraDisconnectEvent(EntityUid user){User = user;}
    }

    [Serializable, NetSerializable]
    public class PowerChangedDisconnectEvent : EntityEventArgs
    {
        public EntityUid Console {get;}

        public PowerChangedDisconnectEvent(EntityUid console){Console = console;}
    }
    [Serializable, NetSerializable]
    public class SecurityCameraConnectionChangedEvent : EntityEventArgs
    {
        public EntityUid Uid {get;}
        public bool Connected {get;}

        public SecurityCameraConnectionChangedEvent(EntityUid uid,bool connected)
        {
            Uid = uid;
            Connected = connected;
        }
    }
}