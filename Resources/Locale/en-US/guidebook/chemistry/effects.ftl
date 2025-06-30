-create-3rd-person =
    { $chance ->
        [1] Creates
        *[other] create
    }

-cause-3rd-person =
    { $chance ->
        [1] Causes
        *[other] cause
    }

-satiate-3rd-person =
    { $chance ->
        [1] Satiates
        *[other] satiate
    }

reagent-effect-guidebook-create-entity-reaction-effect =
    { $chance ->
        [1] Creates
        *[other] create
    } { $amount ->
        [1] {INDEFINITE($entname)}
        *[other] {$amount} {MAKEPLURAL($entname)}
    }

reagent-effect-guidebook-explosion-reaction-effect =
    { $chance ->
        [1] Causes
        *[other] cause
    } an explosion

reagent-effect-guidebook-emp-reaction-effect =
    { $chance ->
        [1] Causes
        *[other] cause
    } an electromagnetic pulse

reagent-effect-guidebook-flash-reaction-effect =
    { $chance ->
        [1] Causes
        *[other] cause
    } a blinding flash

reagent-effect-guidebook-foam-area-reaction-effect =
    { $chance ->
        [1] Creates
        *[other] create
    } large quantities of foam

reagent-effect-guidebook-smoke-area-reaction-effect =
    { $chance ->
        [1] Creates
        *[other] create
    } large quantities of smoke

reagent-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Satiates
        *[other] satiate
    } { $relative ->
        [1] thirst averagely
        *[other] thirst at {NATURALFIXED($relative, 3)}x the average rate
    }

reagent-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Satiates
        *[other] satiate
    } { $relative ->
        [1] hunger averagely
        *[other] hunger at {NATURALFIXED($relative, 3)}x the average rate
    }

reagent-effect-guidebook-health-change =
    { $chance ->
        [1] { $healsordeals ->
                [heals] Heals
                [deals] Deals
                *[both] Modifies health by
             }
        *[other] { $healsordeals ->
                    [heals] heal
                    [deals] deal
                    *[both] modify health by
                 }
    } { $changes }

reagent-effect-guidebook-even-health-change =
    { $chance ->
        [1] { $healsordeals ->
            [heals] Evenly heals
            [deals] Evenly deals
            *[both] Evenly modifies health by
        }
        *[other] { $healsordeals ->
            [heals] evenly heal
            [deals] evenly deal
            *[both] evenly modify health by
        }
    } { $changes }


reagent-effect-guidebook-status-effect =
    { $type ->
        [add]   { $chance ->
                    [1] Causes
                    *[other] cause
                } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} with accumulation
        *[set]  { $chance ->
                    [1] Causes
                    *[other] cause
                } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} without accumulation
        [remove]{ $chance ->
                    [1] Removes
                    *[other] remove
                } {NATURALFIXED($time, 3)} {MANY("second", $time)} of {LOC($key)}
    }

reagent-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Sets
        *[other] set
    } the solution temperature to exactly {NATURALFIXED($temperature, 2)}k

reagent-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1] { $deltasign ->
                [1] Adds
                *[-1] Removes
            }
        *[other]
            { $deltasign ->
                [1] add
                *[-1] remove
            }
    } heat from the solution until it reaches { $deltasign ->
                [1] at most {NATURALFIXED($maxtemp, 2)}k
                *[-1] at least {NATURALFIXED($mintemp, 2)}k
            }

reagent-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1] { $deltasign ->
                [1] Adds
                *[-1] Removes
            }
        *[other]
            { $deltasign ->
                [1] add
                *[-1] remove
            }
    } {NATURALFIXED($amount, 2)}u of {$reagent} { $deltasign ->
        [1] to
        *[-1] from
    } the solution

reagent-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1] { $deltasign ->
                [1] Adds
                *[-1] Removes
            }
        *[other]
            { $deltasign ->
                [1] add
                *[-1] remove
            }
    } {NATURALFIXED($amount, 2)}u of reagents in the group {$group} { $deltasign ->
            [1] to
            *[-1] from
        } the solution

reagent-effect-guidebook-adjust-temperature =
    { $chance ->
        [1] { $deltasign ->
                [1] Adds
                *[-1] Removes
            }
        *[other]
            { $deltasign ->
                [1] add
                *[-1] remove
            }
    } {POWERJOULES($amount)} of heat { $deltasign ->
            [1] to
            *[-1] from
        } the body it's in

reagent-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Causes
        *[other] cause
    } the disease { $disease }

reagent-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Causes
        *[other] cause
    } the diseases { $diseases }

reagent-effect-guidebook-jittering =
    { $chance ->
        [1] Causes
        *[other] cause
    } jittering

reagent-effect-guidebook-chem-clean-bloodstream =
    { $chance ->
        [1] Cleanses
        *[other] cleanse
    } the bloodstream of other chemicals

reagent-effect-guidebook-cure-disease =
    { $chance ->
        [1] Cures
        *[other] cure
    } diseases

reagent-effect-guidebook-cure-eye-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Deals
                *[-1] Heals
            }
        *[other]
            { $deltasign ->
                [1] deal
                *[-1] heal
            }
    } eye damage

reagent-effect-guidebook-chem-vomit =
    { $chance ->
        [1] Causes
        *[other] cause
    } vomiting

reagent-effect-guidebook-create-gas =
    { $chance ->
        [1] Creates
        *[other] create
    } { $moles } { $moles ->
        [1] mole
        *[other] moles
    } of { $gas }

reagent-effect-guidebook-drunk =
    { $chance ->
        [1] Causes
        *[other] cause
    } drunkness

reagent-effect-guidebook-electrocute =
    { $chance ->
        [1] Electrocutes
        *[other] electrocute
    } the metabolizer for {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-emote =
    { $chance ->
        [1] Will force
        *[other] force
    } the metabolizer to [bold][color=white]{$emote}[/color][/bold]

reagent-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Extinguishes
        *[other] extinguish
    } fire

reagent-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Increases
        *[other] increase
    } flammability

reagent-effect-guidebook-ignite =
    { $chance ->
        [1] Ignites
        *[other] ignite
    } the metabolizer

reagent-effect-guidebook-make-sentient =
    { $chance ->
        [1] Makes
        *[other] make
    } the metabolizer sentient

reagent-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Polymorphs
        *[other] polymorph
    } the metabolizer into a { $entityname }

reagent-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1] { $deltasign ->
                [1] Induces
                *[-1] Reduces
            }
        *[other] { $deltasign ->
                    [1] induce
                    *[-1] reduce
                 }
    } bleeding

reagent-effect-guidebook-modify-blood-level =
    { $chance ->
        [1] { $deltasign ->
                [1] Increases
                *[-1] Decreases
            }
        *[other] { $deltasign ->
                    [1] increases
                    *[-1] decreases
                 }
    } blood level

reagent-effect-guidebook-paralyze =
    { $chance ->
        [1] Paralyzes
        *[other] paralyze
    } the metabolizer for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } movement speed by {NATURALFIXED($walkspeed, 3)}x for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Temporarily staves
        *[other] temporarily stave
    } off narcolepsy

reagent-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Washes
        *[other] wash
    } off cream pie from one's face

reagent-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Cures
        *[other] cure
    } an ongoing zombie infection

reagent-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Gives
        *[other] give
    } an individual the zombie infection

reagent-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Cures
        *[other] cure
    } an ongoing zombie infection, and provides immunity to future infections

reagent-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Regenerates
        *[other] regenerate
    } {NATURALFIXED($time, 3)} {MANY("second", $time)} of rotting

reagent-effect-guidebook-area-reaction =
    { $chance ->
        [1] Causes
        *[other] cause
    } a smoke or foam reaction for {NATURALFIXED($duration, 3)} {MANY("second", $duration)}

reagent-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Causes
        *[other] cause
    } chemicals applied to an object to be added to its internal solution container

reagent-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Helps
        *[other] help
        } unlock an alien artifact.

reagent-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Adjusts
        *[other] adjust
    } {$attribute} by [color={$colorName}]{$amount}[/color]

reagent-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Ages back
        *[other] age back
    } the plant, depending on the plant's age and time to grow

reagent-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Restores
        *[other] restore
    } viability to a plant rendered nonviable by a mutation

reagent-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Increases
        *[other] increase
    } the plant's lifespan and/or base health with 10% chance for each

reagent-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Increases
        *[other] increase
    } the plant's potency by {$increase} up to a maximum of {$limit}. Causes the plant to lose its seeds once the potency reaches {$seedlesstreshold}. Trying to add potency over {$limit} may cause decrease in yield at a 10% chance

reagent-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Restores the
        *[other] restore the
    } seeds of the plant

reagent-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Removes the
        *[other] remove the
    } seeds of the plant
