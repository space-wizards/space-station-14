using Content.Server.GameObjects.Components.NewPower;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Network
{
    //Todo: make not shit?
    public class NetworkInjector
    {
        public static BaseNetwork ReturnNewNetwork(NetworkNodeComponent sourceNode)
        {
            switch (sourceNode.NetworkType)
            {
                case NetworkType.HVPower:
                    return new PowerNetwork(sourceNode);
                case NetworkType.MVPower:
                    return new PowerNetwork(sourceNode);
                case NetworkType.LVPower:
                    return new PowerNetwork(sourceNode);
                default:
                    throw new Exception($"Invalid '{typeof(NetworkType)}'");
            }
        }
    }
}
