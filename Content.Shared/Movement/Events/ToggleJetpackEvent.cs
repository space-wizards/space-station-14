// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Actions;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on a jetpack whenever it is toggled.
/// </summary>
public sealed partial class ToggleJetpackEvent : InstantActionEvent {}
