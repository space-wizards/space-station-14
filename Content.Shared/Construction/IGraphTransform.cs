// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Construction;

public interface IGraphTransform
{
    public void Transform(EntityUid oldUid, EntityUid newUid, EntityUid? userUid, GraphTransformArgs args);
}

public readonly struct GraphTransformArgs
{
    public readonly IEntityManager EntityManager;

    public GraphTransformArgs(IEntityManager entityManager)
    {
        EntityManager = entityManager;
    }
}
