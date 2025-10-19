// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised on an action entity to get its event.
/// </summary>
[ByRefEvent]
public record struct ActionGetEventEvent(BaseActionEvent? Event = null);
