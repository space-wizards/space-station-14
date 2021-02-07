#nullable enable
using System;
using System.Threading.Tasks;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Construction
{
    public interface IGraphAction : IExposeData
    {
        Task PerformAction(IEntity entity, IEntity? user);
    }
}
