// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.DoAfter;

/// <summary>
///     Added to entities that are currently performing any doafters.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveDoAfterComponent : Component
{
}
