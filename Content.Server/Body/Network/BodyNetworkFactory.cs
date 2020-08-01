using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;

namespace Content.Server.Body.Network
{
    public class BodyNetworkFactory : IBodyNetworkFactory
    {
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        /// <summary>
        ///     Mapping of body network names to their types.
        /// </summary>
        private readonly Dictionary<string, Type> _names = new Dictionary<string, Type>();

        private void Register(Type type)
        {
            if (_names.ContainsValue(type))
            {
                throw new InvalidOperationException($"Type is already registered: {type}");
            }

            if (!type.IsSubclassOf(typeof(BodyNetwork)))
            {
                throw new InvalidOperationException($"{type} is not a subclass of {nameof(BodyNetwork)}");
            }

            var dummy = (BodyNetwork) Activator.CreateInstance(type);

            if (dummy == null)
            {
                throw new NullReferenceException();
            }

            var name = dummy.Name;

            if (name == null)
            {
                throw new NullReferenceException($"{type}'s name cannot be null.");
            }

            if (_names.ContainsKey(name))
            {
                throw new InvalidOperationException($"{name} is already registered.");
            }

            _names.Add(name, type);
        }

        public void DoAutoRegistrations()
        {
            var bodyNetwork = typeof(BodyNetwork);

            foreach (var child in _reflectionManager.GetAllChildren(bodyNetwork))
            {
                Register(child);
            }
        }

        public BodyNetwork GetNetwork(string name)
        {
            Type type;

            try
            {
                type = _names[name];
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException($"No {nameof(BodyNetwork)} exists with name {name}");
            }

            return _typeFactory.CreateInstance<BodyNetwork>(type);
        }

        public BodyNetwork GetNetwork(Type type)
        {
            if (!_names.ContainsValue(type))
            {
                throw new ArgumentException($"{type} is not registered.");
            }

            return _typeFactory.CreateInstance<BodyNetwork>(type);
        }
    }
}
