// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Interaction
{
    public interface ITargetedInteractEventArgs
    {
        /// <summary>
        /// Performer of the attack
        /// </summary>
        EntityUid User { get; }
        /// <summary>
        /// Target of the attack
        /// </summary>
        EntityUid Target { get; }

    }
}
