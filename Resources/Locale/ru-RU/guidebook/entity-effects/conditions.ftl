entity-condition-guidebook-total-damage =
    { $max ->
        [2147483648] тело имеет по крайней мере { NATURALFIXED($min, 2) } общего урона
       *[other]
            { $min ->
                [0] тело имеет не более { NATURALFIXED($max, 2) } общего урона
               *[other] тело имеет между { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } общего урона
            }
    }
entity-condition-guidebook-type-damage =
    { $max ->
        [2147483648] тело имеет по крайней мере { NATURALFIXED($min, 2) } урона типа { $type }
       *[other]
            { $min ->
                [0] тело имеет не более { NATURALFIXED($max, 2) } урона типа { $type }
               *[other] тело имеет между { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } урона типа { $type }
            }
    }
entity-condition-guidebook-group-damage =
    { $max ->
        [2147483648] тело имеет по крайней мере { NATURALFIXED($min, 2) } урона группы { $type }
       *[other]
            { $min ->
                [0] тело имеет не более { NATURALFIXED($max, 2) } урона группы { $type }
               *[other] тело имеет между { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } урона группы { $type }
            }
    }
entity-condition-guidebook-total-hunger =
    { $max ->
        [2147483648] цель имеет по крайней мере { NATURALFIXED($min, 2) } общего голода
       *[other]
            { $min ->
                [0] цель имеет не более { NATURALFIXED($max, 2) } общего голода
               *[other] цель имеет между  { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } общего голода
            }
    }
entity-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] в кровеносной системе имеется по крайней мере { NATURALFIXED($min, 2) } ед. { $reagent }
       *[other]
            { $min ->
                [0] имеется не более { NATURALFIXED($max, 2) } ед. { $reagent }
               *[other] имеет между { NATURALFIXED($min, 2) } ед. и { NATURALFIXED($max, 2) } ед. { $reagent }
            }
    }
entity-condition-guidebook-mob-state-condition = пациент в { $state }
entity-condition-guidebook-job-condition = должность цели — { $job }
entity-condition-guidebook-solution-temperature =
    температура раствора составляет { $max ->
        [2147483648] не менее { NATURALFIXED($min, 2) }k
       *[other]
            { $min ->
                [0] не более { NATURALFIXED($max, 2) }k
               *[other] между { NATURALFIXED($min, 2) }k и { NATURALFIXED($max, 2) }k
            }
    }
entity-condition-guidebook-body-temperature =
    температура тела составляет { $max ->
        [2147483648] не менее { NATURALFIXED($min, 2) }k
       *[other]
            { $min ->
                [0] не более { NATURALFIXED($max, 2) }k
               *[other] между { NATURALFIXED($min, 2) }k и { NATURALFIXED($max, 2) }k
            }
    }
entity-condition-guidebook-organ-type =
    метаболизирующий орган { $shouldhave ->
        [true] это
       *[false] это не
    } { $name } орган
entity-condition-guidebook-has-tag =
    цель { $invert ->
        [true] не имеет
       *[false] имеет
    } метку { $tag }
entity-condition-guidebook-this-reagent = этот реагент
entity-condition-guidebook-breathing =
    цель { $isBreathing ->
        [true] дышит нормально
       *[false] задыхается
    }
entity-effect-condition-guidebook-internals =
    цель { $usingInternals ->
        [true] использует дыхательную маску
       *[false] дышит атмосферным газом
    }
