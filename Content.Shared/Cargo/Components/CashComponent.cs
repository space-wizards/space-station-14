// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Can be inserted into a <see cref="CargoOrderConsoleComponent"/> to increase the station's bank account.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CashComponent : Component
{

}
