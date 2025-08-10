# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

reagent-effect-condition-guidebook-has-component =
    the target { $invert ->
                 [true] is not
                 *[false] is
                } {$comp}

reagent-effect-guidebook-drop-items =
    { $chance ->
        [1] Forces
        *[other] force
    } to drop held items

reagent-name-thick-smoke = thick smoke
reagent-desc-thick-smoke = Extremely thick smoke with magical properties. You don't want to inhale it.

reagent-name-mugwort = mugwort tea
reagent-desc-mugwort = A rather bitter herb once thought to hold magical protective properties.

reagent-comp-condition-wizard-or-apprentice = wizard or apprentice

reagent-physical-desc-magical = magical
