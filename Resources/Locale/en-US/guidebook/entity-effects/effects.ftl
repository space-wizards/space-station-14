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

entity-effect-guidebook-spawn-entity =
    { $chance ->
        [1] Creates
        *[other] create
    } { $amount ->
        [1] {INDEFINITE($entname)}
        *[other] {$amount} {MAKEPLURAL($entname)}
    }

entity-effect-guidebook-destroy =
    { $chance ->
        [1] Destroys
        *[other] destroy
    } the object

entity-effect-guidebook-break =
    { $chance ->
        [1] Breaks
        *[other] break
    } the object

entity-effect-guidebook-explosion =
    { $chance ->
        [1] Causes
        *[other] cause
    } an explosion

entity-effect-guidebook-emp =
    { $chance ->
        [1] Causes
        *[other] cause
    } an electromagnetic pulse

entity-effect-guidebook-flash =
    { $chance ->
        [1] Causes
        *[other] cause
    } a blinding flash

entity-effect-guidebook-foam-area =
    { $chance ->
        [1] Creates
        *[other] create
    } large quantities of foam

entity-effect-guidebook-smoke-area =
    { $chance ->
        [1] Creates
        *[other] create
    } large quantities of smoke

entity-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Satiates
        *[other] satiate
    } { $relative ->
        [1] thirst averagely
        *[other] thirst at {NATURALFIXED($relative, 3)}x the average rate
    }

entity-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Satiates
        *[other] satiate
    } { $relative ->
        [1] hunger averagely
        *[other] hunger at {NATURALFIXED($relative, 3)}x the average rate
    }

entity-effect-guidebook-health-change =
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

entity-effect-guidebook-even-health-change =
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

entity-effect-guidebook-status-effect-old =
    { $type ->
        [update]{ $chance ->
                    [1] Causes
                     *[other] cause
                 } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} without accumulation
        [add]   { $chance ->
                    [1] Causes
                    *[other] cause
                } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} with accumulation
        [set]  { $chance ->
                    [1] Causes
                    *[other] cause
                } {LOC($key)} for {NATURALFIXED($time, 3)} {MANY("second", $time)} without accumulation
        *[remove]{ $chance ->
                    [1] Removes
                    *[other] remove
                } {NATURALFIXED($time, 3)} {MANY("second", $time)} of {LOC($key)}
    }

entity-effect-guidebook-status-effect =
    { $type ->
        [update]{ $chance ->
                    [1] Causes
                    *[other] cause
                 } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} without accumulation
        [add]   { $chance ->
                    [1] Causes
                    *[other] cause
                } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} with accumulation
        [set]  { $chance ->
                    [1] Causes
                    *[other] cause
                } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} without accumulation
        *[remove]{ $chance ->
                    [1] Removes
                    *[other] remove
                } {NATURALFIXED($time, 3)} {MANY("second", $time)} of {LOC($key)}
    } { $delay ->
        [0] immediately
        *[other] after a {NATURALFIXED($delay, 3)} second delay
    }

entity-effect-guidebook-status-effect-indef =
    { $type ->
        [update]{ $chance ->
                    [1] Causes
                    *[other] cause
                 } permanent {LOC($key)}
        [add]   { $chance ->
                    [1] Causes
                    *[other] cause
                } permanent {LOC($key)}
        [set]  { $chance ->
                    [1] Causes
                    *[other] cause
                } permanent {LOC($key)}
        *[remove]{ $chance ->
                    [1] Removes
                    *[other] remove
                } {LOC($key)}
    } { $delay ->
        [0] immediately
        *[other] after a {NATURALFIXED($delay, 3)} second delay
    }

entity-effect-guidebook-knockdown =
    { $type ->
        [update]{ $chance ->
                    [1] Causes
                    *[other] cause
                    } {LOC($key)} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} without accumulation
        [add]   { $chance ->
                    [1] Causes
                    *[other] cause
                } knockdown for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} with accumulation
        *[set]  { $chance ->
                    [1] Causes
                    *[other] cause
                } knockdown for at least {NATURALFIXED($time, 3)} {MANY("second", $time)} without accumulation
        [remove]{ $chance ->
                    [1] Removes
                    *[other] remove
                } {NATURALFIXED($time, 3)} {MANY("second", $time)} of knockdown
    }

entity-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Sets
        *[other] set
    } the solution temperature to exactly {NATURALFIXED($temperature, 2)}k

entity-effect-guidebook-adjust-solution-temperature-effect =
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

entity-effect-guidebook-adjust-reagent-reagent =
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

entity-effect-guidebook-adjust-reagent-group =
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

entity-effect-guidebook-adjust-temperature =
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

entity-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Causes
        *[other] cause
    } the disease { $disease }

entity-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Causes
        *[other] cause
    } the diseases { $diseases }

entity-effect-guidebook-jittering =
    { $chance ->
        [1] Causes
        *[other] cause
    } jittering

entity-effect-guidebook-clean-bloodstream =
    { $chance ->
        [1] Cleanses
        *[other] cleanse
    } the bloodstream of other chemicals

entity-effect-guidebook-cure-disease =
    { $chance ->
        [1] Cures
        *[other] cure
    } diseases

entity-effect-guidebook-eye-damage =
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

entity-effect-guidebook-vomit =
    { $chance ->
        [1] Causes
        *[other] cause
    } vomiting

entity-effect-guidebook-create-gas =
    { $chance ->
        [1] Creates
        *[other] create
    } { $moles } { $moles ->
        [1] mole
        *[other] moles
    } of { $gas }

entity-effect-guidebook-drunk =
    { $chance ->
        [1] Causes
        *[other] cause
    } drunkness

entity-effect-guidebook-electrocute =
    { $chance ->
        [1] Electrocutes
        *[other] electrocute
    } the metabolizer for {NATURALFIXED($time, 3)} {MANY("second", $time)}

entity-effect-guidebook-emote =
    { $chance ->
        [1] Will force
        *[other] force
    } the metabolizer to [bold][color=white]{$emote}[/color][/bold]

entity-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Extinguishes
        *[other] extinguish
    } fire

entity-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Increases
        *[other] increase
    } flammability

entity-effect-guidebook-ignite =
    { $chance ->
        [1] Ignites
        *[other] ignite
    } the metabolizer

entity-effect-guidebook-make-sentient =
    { $chance ->
        [1] Makes
        *[other] make
    } the metabolizer sentient

entity-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Polymorphs
        *[other] polymorph
    } the metabolizer into a { $entityname }

entity-effect-guidebook-modify-bleed-amount =
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

entity-effect-guidebook-modify-blood-level =
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

entity-effect-guidebook-paralyze =
    { $chance ->
        [1] Paralyzes
        *[other] paralyze
    } the metabolizer for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

entity-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } movement speed by {NATURALFIXED($sprintspeed, 3)}x for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

entity-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Temporarily staves
        *[other] temporarily stave
    } off narcolepsy

entity-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Washes
        *[other] wash
    } off cream pie from one's face

entity-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Cures
        *[other] cure
    } an ongoing zombie infection

entity-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Gives
        *[other] give
    } an individual the zombie infection

entity-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Cures
        *[other] cure
    } an ongoing zombie infection, and provides immunity to future infections

entity-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Regenerates
        *[other] regenerate
    } {NATURALFIXED($time, 3)} {MANY("second", $time)} of rotting

entity-effect-guidebook-area-reaction =
    { $chance ->
        [1] Causes
        *[other] cause
    } a smoke or foam reaction for {NATURALFIXED($duration, 3)} {MANY("second", $duration)}

entity-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Causes
        *[other] cause
    } {$reagent} to be added to its internal solution container

entity-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Helps
        *[other] help
        } unlock an alien artifact.

entity-effect-guidebook-artifact-durability-restore =
    Restores {$restored} durability in active alien artifact nodes.

entity-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Adjusts
        *[other] adjust
    } {$attribute} by {$positive ->
    [true] [color=red]{$amount}[/color]
    *[false] [color=green]{$amount}[/color]
    }

entity-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Ages back
        *[other] age back
    } the plant, depending on the plant's age and time to grow

entity-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Restores
        *[other] restore
    } viability to a plant rendered nonviable by a mutation

entity-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Increases
        *[other] increase
    } the plant's lifespan and/or base health with 10% chance for each

entity-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Increases
        *[other] increase
    } the plant's potency by {$increase} up to a maximum of {$limit}. Causes the plant to lose its seeds once the potency reaches {$seedlesstreshold}. Trying to add potency over {$limit} may cause decrease in yield at a 10% chance

entity-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Restores the
        *[other] restore the
    } seeds of the plant

entity-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Removes the
        *[other] remove the
    } seeds of the plant

entity-effect-guidebook-plant-mutate-chemicals =
    { $chance ->
        [1] Mutates
        *[other] mutate
    } a plant to produce {$name}
