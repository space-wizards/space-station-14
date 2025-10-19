// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Client.UserInterface.Fragments;

/// <summary>
/// The component used for defining a ui fragment to attach to an entity
/// </summary>
/// <remarks>
/// This is used primarily for PDA cartridges.
/// </remarks>
/// <seealso cref="UIFragment"/>
[RegisterComponent]
public sealed partial class UIFragmentComponent : Component
{
    [DataField("ui", true)]
    public UIFragment? Ui;
}
