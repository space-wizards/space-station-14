using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.Server.AI.Utility.Considerations
{
    public sealed class ConsiderationsManager
    {
        private readonly Dictionary<Type, Consideration> _considerations = new();

        public void Initialize()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();

            foreach (var conType in reflectionManager.GetAllChildren(typeof(Consideration)))
            {
                var con = (Consideration) typeFactory.CreateInstance(conType);
                _considerations.Add(conType, con);
            }
        }

        public T Get<T>() where T : Consideration
        {
            return (T) _considerations[typeof(T)];
        }
    }
}
