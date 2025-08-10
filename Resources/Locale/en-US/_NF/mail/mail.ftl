# SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

mail-large-item-name-unaddressed = package
mail-large-item-name-addressed = package ({$recipient})
mail-large-desc-far = A large package.
mail-large-desc-close = A large package addressed to {CAPITALIZE($name)}, {$job}.

### Frontier: mailtestbulk
command-mailtestbulk = Sends one of each type of parcel to a given mail teleporter.  Implicitly calls mailnow.
command-mailtestbulk-help = Usage: {$command} <teleporter_id>
command-mailtestbulk-success = Success! All mail teleporters will be delivering another round of mail soon.
