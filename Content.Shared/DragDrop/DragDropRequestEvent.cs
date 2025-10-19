// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.DragDrop
{
    /// <summary>
    /// Raised on the client to the server requesting a drag-drop.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class DragDropRequestEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that was dragged and dropped.
        /// </summary>
        public NetEntity Dragged { get; }

        /// <summary>
        ///     Entity that was drag dropped on.
        /// </summary>
        public NetEntity Target { get; }

        public DragDropRequestEvent(NetEntity dragged, NetEntity target)
        {
            Dragged = dragged;
            Target = target;
        }
    }
}
