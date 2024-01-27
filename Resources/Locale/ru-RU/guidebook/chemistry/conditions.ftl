reagent-effect-condition-guidebook-total-damage =
    { $max ->
        [2147483648] тело имеет по крайней мере { NATURALFIXED($min, 2) } общего урона
       *[other]
            { $min ->
                [0] имеет не более { NATURALFIXED($max, 2) } общего урона
               *[other] имеет между { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } общего урона
            }
    }
reagent-effect-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] в кровеносной системе имеется по крайней мере { NATURALFIXED($min, 2) }ед. { $reagent }
       *[other]
            { $min ->
                [0] имеется не более { NATURALFIXED($max, 2) }ед. { $reagent }
               *[other] имеет между { NATURALFIXED($min, 2) }ед. и { NATURALFIXED($max, 2) }ед. { $reagent }
            }
    }
reagent-effect-condition-guidebook-mob-state-condition = пациент в { $state }
reagent-effect-condition-guidebook-solution-temperature =
    температура раствора составляет { $max ->
        [2147483648] не менее { NATURALFIXED($min, 2) }k
       *[other]
            { $min ->
                [0] не более { NATURALFIXED($max, 2) }k
               *[other] между { NATURALFIXED($min, 2) }k и { NATURALFIXED($max, 2) }k
            }
    }
reagent-effect-condition-guidebook-body-temperature =
    температура тела составляет { $max ->
        [2147483648] не менее { NATURALFIXED($min, 2) }k
       *[other]
            { $min ->
                [0] не более { NATURALFIXED($max, 2) }k
               *[other] между { NATURALFIXED($min, 2) }k и { NATURALFIXED($max, 2) }k
            }
    }
reagent-effect-condition-guidebook-organ-type =
    метаболизирующий орган { $shouldhave ->
        [true] это
       *[false] это не
    } { $name } орган
reagent-effect-condition-guidebook-has-tag =
    цель { $invert ->
        [true] не имеет
       *[false] имеет
    } метку { $tag }
