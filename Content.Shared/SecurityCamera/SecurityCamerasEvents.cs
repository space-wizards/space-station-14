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
        public EntityUid Camera {get;}
    
        public SecurityCameraConnectEvent(EntityUid user, EntityUid camera)
        {
            User = user;
            Camera = camera;
        }
    }

    [Serializable, NetSerializable]
    public class SecurityCameraDisconnectEvent : EntityEventArgs
    {
        public EntityUid User {get;}

        public SecurityCameraDisconnectEvent(EntityUid user){User = user;}
    }
}