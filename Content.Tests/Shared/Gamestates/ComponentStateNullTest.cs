using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Tests.Shared.Gamestates
{
    [TestFixture]
    public sealed class ComponentStateNullTest
    {
        [Test]
        public void HandleComponentState_NullStates_NotThrow()
        {
            var reflection = ReflectionManagerFactory();
            var comps = reflection.GetAllChildren<Component>();

            foreach (var compType in comps)
            {
                // Any component should be able to be instantiated without DI injection.
                var compInstance = (IComponent) Activator.CreateInstance(compType);

                // Any component should treat this as a null function.
                compInstance.HandleComponentState(null, null);
            }
        }

        private static IReflectionManager ReflectionManagerFactory()
        {
            AppDomain.CurrentDomain.Load("Robust.Client");
            AppDomain.CurrentDomain.Load("Content.Client");
            AppDomain.CurrentDomain.Load("Robust.Server");
            AppDomain.CurrentDomain.Load("Content.Server");
            AppDomain.CurrentDomain.Load("Robust.Shared");
            AppDomain.CurrentDomain.Load("Content.Shared");

            var assemblies = new List<Assembly>(7);
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Client"));
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Content.Client"));
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server"));
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Content.Server"));
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"));
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Content.Shared"));
            assemblies.Add(Assembly.GetExecutingAssembly());

            var reflection = new FullReflectionManager();

            reflection.LoadAssemblies(assemblies);

            return reflection;
        }

        private sealed class FullReflectionManager : ReflectionManager
        {
            protected override IEnumerable<string> TypePrefixes => Prefixes;

            private static readonly string[] Prefixes = {
                "",

                "Robust.Client.",
                "Content.Client.",

                "Robust.Shared.",
                "Content.Shared.",

                "Robust.Server.",
                "Content.Server.",
            };
        }
    }
}
