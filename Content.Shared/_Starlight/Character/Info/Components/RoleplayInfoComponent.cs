// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Character.Info.Components;

/// <summary>
/// Stores Out-of-character info associated with this entity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedGameTicker), typeof(SLSharedCharacterInfoSystem))]
public sealed partial class RoleplayInfoComponent : Component
{
    [DataField, AutoNetworkedField] public string OOCNotes = string.Empty;
}