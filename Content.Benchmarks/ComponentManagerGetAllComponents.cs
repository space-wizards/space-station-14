using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Moq;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Benchmarks
{
    public class ComponentManagerGetAllComponents
    {
        private readonly List<IEntity> _entities = new List<IEntity>();

        private IComponentManager _componentManager;

        [Params(500, 1000, 5000)] public int N { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Initialize component manager.
            IoCManager.InitThread();

            IoCManager.Register<IComponentManager, ComponentManager>();

            var dummyReg = new Mock<IComponentRegistration>();
            dummyReg.SetupGet(p => p.Name).Returns("Dummy");
            dummyReg.SetupGet(p => p.Type).Returns(typeof(DummyComponent));
            dummyReg.SetupGet(p => p.NetID).Returns((uint?) null);
            dummyReg.SetupGet(p => p.NetworkSynchronizeExistence).Returns(false);
            dummyReg.SetupGet(p => p.References).Returns(new [] {typeof(DummyComponent)});

            var componentFactory = new Mock<IComponentFactory>();
            componentFactory.Setup(p => p.GetComponent<DummyComponent>()).Returns(new DummyComponent());
            componentFactory.Setup(p => p.GetRegistration(It.IsAny<DummyComponent>())).Returns(dummyReg.Object);

            IoCManager.RegisterInstance<IComponentFactory>(componentFactory.Object);

            IoCManager.BuildGraph();

            _componentManager = IoCManager.Resolve<IComponentManager>();

            // Initialize N entities with one component.
            for (var i = 0; i < N; i++)
            {
                var entity = new Entity();
                entity.SetUid(new EntityUid(i + 1));
                _entities.Add(entity);

                _componentManager.AddComponent<DummyComponent>(entity);
            }
        }

        [Benchmark]
        public int Run()
        {
            var count = 0;

            foreach (var _ in _componentManager.GetAllComponents<DummyComponent>())
            {
                count += 1;
            }

            return count;
        }

        private class DummyComponent : Component
        {
            public override string Name => "Dummy";
        }
    }
}
