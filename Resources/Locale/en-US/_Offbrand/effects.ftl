reagent-guidebook-status-effect = Causes { $effect } during metabolism{ $conditionCount ->
        [0] .
        *[other] {" "}when { $conditions }.
    }

entity-effect-guidebook-status-effect-remove = { $chance ->
        [1] Removes { LOC($key) }
   *[other] remove { LOC($key) }
}

entity-effect-guidebook-modify-brain-damage-heals = { $chance ->
        [1] Heals { $amount } brain activity
   *[other] heal { $amount } brain activity
}
entity-effect-guidebook-modify-brain-damage-deals = { $chance ->
        [1] Deals { $amount } brain damage
   *[other] deal { $amount } brain damage
}
entity-effect-guidebook-modify-heart-damage-heals = { $chance ->
        [1] Heals { $amount } heart health
   *[other] heal { $amount } heart health
}
entity-effect-guidebook-modify-heart-damage-deals = { $chance ->
        [1] Deals { $amount } heart damage
   *[other] deal { $amount } heart damage
}
entity-effect-guidebook-modify-lung-damage-heals = { $chance ->
        [1] Heals { $amount } lung health
   *[other] heal { $amount } lung health
}
entity-effect-guidebook-modify-lung-damage-deals = { $chance ->
        [1] Deals { $amount } lung damage
   *[other] deal { $amount } lung damage
}
entity-effect-guidebook-clamp-wounds = { $probability ->
        [1] Stops bleeding in wounds with { NATURALPERCENT($chance, 2) } chance per wound
   *[other] stop bleeding in wounds with { NATURALPERCENT($chance, 2) } chance per wound
}
entity-condition-guidebook-heart-damage = { $max ->
    [2147483648] it has at least {NATURALFIXED($min, 2)} heart damage
    *[other] { $min ->
                [0] it has at most {NATURALFIXED($max, 2)} heart damage
                *[other] it has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} heart damage
             }
}
entity-condition-guidebook-lung-damage = { $max ->
    [2147483648] it has at least {NATURALFIXED($min, 2)} lung damage
    *[other] { $min ->
                [0] it has at most {NATURALFIXED($max, 2)} lung damage
                *[other] it has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} lung damage
             }
}
entity-condition-guidebook-brain-damage = { $max ->
    [2147483648] it has at least {NATURALFIXED($min, 2)} brain damage
    *[other] { $min ->
                [0] it has at most {NATURALFIXED($max, 2)} brain damage
                *[other] it has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} brain damage
             }
}
entity-condition-guidebook-total-group-damage = { $max ->
    [2147483648] it has at least {NATURALFIXED($min, 2)} { $name } damage
    *[other] { $min ->
                [0] it has at most {NATURALFIXED($max, 2)} { $name } damage
                *[other] it has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} { $name } damage
             }
}
entity-effect-guidebook-modify-brain-oxygen-heals = { $chance ->
        [1] Replenishes { $amount } brain oxygenation
   *[other] replenish { $amount } brain oxygenation
}
entity-effect-guidebook-modify-brain-oxygen-deals = { $chance ->
        [1] Depletes { $amount } brain oxygenation
   *[other] deplete { $amount } brain oxygenation
}

entity-effect-guidebook-start-heart = { $chance ->
        [1] Restarts the target's heart
   *[other] restart the target's heart
}
entity-effect-guidebook-zombify = { $chance ->
        [1] Zombifies the target
   *[other] zombify the target
}

entity-condition-guidebook-total-dosage-threshold =
    { $max ->
        [2147483648] the total dosage of {$reagent} is at least {NATURALFIXED($min, 2)}u
        *[other] { $min ->
                    [0] the total dosage of {$reagent} is at most {NATURALFIXED($max, 2)}u
                    *[other] the total dosage of {$reagent} is between {NATURALFIXED($min, 2)}u and {NATURALFIXED($max, 2)}u
                 }
    }

entity-condition-guidebook-metabolite-threshold =
    { $max ->
        [2147483648] there's at least {NATURALFIXED($min, 2)}u of {$reagent} metabolites
        *[other] { $min ->
                    [0] there's at most {NATURALFIXED($max, 2)}u of {$reagent} metabolites
                    *[other] there's between {NATURALFIXED($min, 2)}u and {NATURALFIXED($max, 2)}u of {$reagent} metabolites
                 }
    }

entity-condition-guidebook-is-zombie-immune =
    the target { $invert ->
                    [true] is not immunized against zombie infections
                   *[false] is immunized against zombie infections
                }

entity-condition-guidebook-is-zombie =
    the target { $invert ->
                    [true] is not a zombie
                   *[false] is a zombie
                }

entity-condition-guidebook-this-metabolite = this reagent's

entity-effect-guidebook-adjust-reagent-gaussian =
    { $chance ->
        [1] { $deltasign ->
                [1] Typically adds
                *[-1] Typically removes
            }
        *[other]
            { $deltasign ->
                [1] typically add
                *[-1] typically remove
            }
    } {NATURALFIXED($mu, 2)}u of {$reagent} { $deltasign ->
        [1] to
        *[-1] from
    } the solution, with the actual amount varying by around {NATURALFIXED($sigma, 2)}u
