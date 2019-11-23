using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Interfaces;

namespace Content.Shared.Utility
{
    /// <summary>
    /// Implementation of IModuleManager. Provides easy way to check if
    /// shared code is being run by the server or the client.
    /// </summary>
    public class ModuleManager : IModuleManager
    {
        private bool _isClientModule = false;
        private bool _isServerModule = false;

        public ModuleManager()
        {
            string ModuleName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            switch (ModuleName)
            {
                case "Robust.Client":
                    _isClientModule = true;
                    break;
                case "Robust.Server":
                    _isServerModule = true;
                    break;
            }
        }

        bool IModuleManager.IsClientModule()
        {
            return _isClientModule;
        }

        bool IModuleManager.IsServerModule()
        {
            return _isServerModule;
        }
    }
}
