reagent-effect-condition-guidebook-total-damage =
    { $max ->
        [2147483648] тело имеет по крайней мере { NATURALFIXED($min, 2) } общего урона
       *[other]
            { $min ->
                [0] имеет не более { NATURALFIXED($max, 2) } общего урона
               *[other] имеет между { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } общего урона
            }
    }
reagent-effect-condition-guidebook-total-hunger =
    { $max ->
        [2147483648] цель имеет по крайней мере { NATURALFIXED($min, 2) } общего голода
       *[other]
            { $min ->
                [0] цель имеет не более { NATURALFIXED($max, 2) } общего голода
               *[other] цель имеет между  { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } общего голода
            }
    }
reagent-effect-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] в кровеносной системе имеется по крайней мере { NATURALFIXED($min, 2) } ед. { $reagent }
       *[other]
            { $min ->
                [0] имеется не более { NATURALFIXED($max, 2) } ед. { $reagent }
               *[other] имеет между { NATURALFIXED($min, 2) } ед. и { NATURALFIXED($max, 2) } ед. { $reagent }
            }
    }
