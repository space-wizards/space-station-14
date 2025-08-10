# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

reagent-effect-condition-guidebook-stamina-damage-threshold =
    { $max ->
        [2147483648] the target has at least {NATURALFIXED($min, 2)} stamina damage
        *[other] { $min ->
                    [0] the target has at most {NATURALFIXED($max, 2)} stamina damage
                    *[other] the target has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} stamina damage
                 }
    }

reagent-effect-condition-guidebook-unique-bloodstream-chem-threshold =
    { $max ->
        [2147483648] { $min ->
                        [1] there's at least {$min} reagent
                        *[other] there's at least {$min} reagents
                     }
        [1] { $min ->
               [0] there's at most {$max} reagent
               *[other] there's between {$min} and {$max} reagents
            }
        *[other] { $min ->
                    [-1] there's at most {$max} reagents
                    *[other] there's between {$min} and {$max} reagents
                 }
    }

reagent-effect-condition-guidebook-typed-damage-threshold =
    { $inverse ->
        [true] the target has at most
        *[false] the target has at least
    } { $changes } damage
