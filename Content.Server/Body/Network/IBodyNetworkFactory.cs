using System;

namespace Content.Server.Body.Network
{
    public interface IBodyNetworkFactory
    {
        void DoAutoRegistrations();

        BodyNetwork GetNetwork(string name);

        BodyNetwork GetNetwork(Type type);
    }
}
