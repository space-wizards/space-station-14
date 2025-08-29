// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Character.Info.Components;

/// <summary>
/// Stores information that is only accessible to the player controlling this mind
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedGameTicker), typeof(SLSharedCharacterInfoSystem))]
public sealed partial class MindSecretsComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField] public string PersonalNotes = string.Empty;
}