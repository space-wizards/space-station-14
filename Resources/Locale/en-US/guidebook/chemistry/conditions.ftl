reagent-effect-condition-guidebook-total-damage =
    { $max ->
        [2147483648] it has at least {NATURALFIXED($min, 2)} total damage
        *[other] { $min ->
                    [0] it has at most {NATURALFIXED($max, 2)} total damage
                    *[other] it has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} total damage
                 }
    }

reagent-effect-condition-guidebook-total-hunger =
    { $max ->
        [2147483648] the target has at least {NATURALFIXED($min, 2)} total hunger
        *[other] { $min ->
                    [0] the target has at most {NATURALFIXED($max, 2)} total hunger
                    *[other] the target has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} total hunger
                 }
    }

reagent-effect-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] there's at least {NATURALFIXED($min, 2)}u of {$reagent}
        *[other] { $min ->
                    [0] there's at most {NATURALFIXED($max, 2)}u of {$reagent}
                    *[other] there's between {NATURALFIXED($min, 2)}u and {NATURALFIXED($max, 2)}u of {$reagent}
                 }
    }

reagent-effect-condition-guidebook-mob-state-condition =
    the mob is { $state }

reagent-effect-condition-guidebook-job-condition =
    the target's job is { $job }

reagent-effect-condition-guidebook-solution-temperature =
    the solution's temperature is { $max ->
            [2147483648] at least {NATURALFIXED($min, 2)}k
            *[other] { $min ->
                        [0] at most {NATURALFIXED($max, 2)}k
                        *[other] between {NATURALFIXED($min, 2)}k and {NATURALFIXED($max, 2)}k
                     }
    }

reagent-effect-condition-guidebook-body-temperature =
    the body's temperature is { $max ->
            [2147483648] at least {NATURALFIXED($min, 2)}k
            *[other] { $min ->
                        [0] at most {NATURALFIXED($max, 2)}k
                        *[other] between {NATURALFIXED($min, 2)}k and {NATURALFIXED($max, 2)}k
                     }
    }

reagent-effect-condition-guidebook-organ-type =
    the metabolizing organ { $shouldhave ->
                                [true] is
                                *[false] is not
                           } {INDEFINITE($name)} {$name} organ

reagent-effect-condition-guidebook-has-tag =
    the target { $invert ->
                 [true] does not have
                 *[false] has
                } the tag {$tag}

reagent-effect-condition-guidebook-this-reagent = this reagent

reagent-effect-condition-guidebook-breathing =
    the metabolizer is { $isBreathing ->
                [true] breathing normally
                *[false] suffocating
               }

reagent-effect-condition-guidebook-internals =
    the metabolizer is { $usingInternals ->
                [true] using internals
                *[false] breathing atmospheric air
               }
