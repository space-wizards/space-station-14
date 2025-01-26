plant-analyzer-component-no-seed = no plant found

plant-analyzer-component-health = Health:
plant-analyzer-component-age = Age:
plant-analyzer-component-water = Water:
plant-analyzer-component-nutrition = Nutrition:
plant-analyzer-component-toxins = Toxins:
plant-analyzer-component-pests = Pests:
plant-analyzer-component-weeds = Weeds:

plant-analyzer-component-alive = [color=green]ALIVE[color]
plant-analyzer-component-dead = [color=red]DEAD[color]
plant-analyzer-component-unviable = [color=red]UNVIABLE[color]
plant-analyzer-component-mutating = [color=#00ff5f]MUTATING[color]

plant-analyzer-component-environemt = This [color=green]{$seedName}[/color] requires an atmosphere at a pressure level of [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color], temperature of [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color] and a light level of [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-void = This [color=green]{$seedName}[/color] has to be grown [bolditalic]in the vacuum of space[/bolditalic] at a light level of [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-gas = This [color=green]{$seedName}[/color] requires an atmosphere containing [bold]{$gases}[/bold] at a pressure level of [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color], temperature of [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color] and a light level of [color=white]{$lightLevel} ± {$lightTolerance}[/color].

plant-analyzer-output = It has [color=lightgreen]{$n} {$potency}[/color]{$seedless} {$n ->
    [one]flower
    *[other]flowers
} that will turn into [color=#a4885c]{$produce}[/color].
plant-analyzer-output-gas = It has [color=lightgreen]{$n} {$potency}[/color]{$seedless} {$n ->
    [one]flower
    *[other]flowers
} that emit [bold]{$gases}[/bold] and will turn into [color=#a4885c]{$produce}[/color].
plant-analyzer-output-nothing = The only thing it seems to do is consume water and nutrients.
plant-analyzer-output-nothing-gas = The only thing it seems to do is turn water and nutrients into [bold]{$gases}[/bold].
plant-analyzer-chemicals = There are trace amounts of [color=white]{$chemicals}[/color] in its stem.
plant-analyzer-seedless = {$space}but [color=red]seedless[/color]
plant-analyzer-produce-plural = {MAKEPLURAL($thing)}

plant-analyzer-potency-tiny = tiny
plant-analyzer-potency-small = small
plant-analyzer-potency-below-average = below average sized
plant-analyzer-potency-average = average sized
plant-analyzer-potency-above-average = above average sized
plant-analyzer-potency-large = rather large
plant-analyzer-potency-huge = huge
plant-analyzer-potency-gigantic = gigantic
plant-analyzer-potency-ludicrous = ludicrously large
plant-analyzer-potency-immeasurable = immeasurably large

plant-analyzer-print = Print
plant-analyzer-printout-missing = N/A
plant-analyzer-printout-l0 = [color=#9FED58][head=2]Plant Analyzer Report[/head][/color]
plant-analyzer-printout-l1 = ──────────────────────────────
plant-analyzer-printout-l2 = [bullet/] Species: {$seedName}
plant-analyzer-printout-l3 = {$indent}[bullet/] Endurance: {$endurance}
plant-analyzer-printout-l4 = {$indent}[bullet/] Lifespan: {$lifespan}
plant-analyzer-printout-l5 = {$indent}[bullet/] Product: [color=#a4885c]{$produce}[/color]
plant-analyzer-printout-l6 = [bullet/] Growth profile:
plant-analyzer-printout-l7 = {$indent}[bullet/] Water: [color=cyan]{$water}[/color]
plant-analyzer-printout-l8 = {$indent}[bullet/] Nutrition: [color=orange]{$nutrients}[/color]
plant-analyzer-printout-l9 = {$indent}[bullet/] Toxins: [color=yellowgreen]{$toxins}[/color]
plant-analyzer-printout-l10 = {$indent}[bullet/] Pests: [color=magenta]{$pests}[/color]
plant-analyzer-printout-l11 = {$indent}[bullet/] Weeds: [color=red]{$weeds}[/color]
plant-analyzer-printout-l12 = [bullet/] Environmental profile:
plant-analyzer-printout-l13 = {$indent}[bullet/] Composition: [bold]{$gasesIn}[/bold]
plant-analyzer-printout-l14 = {$indent}[bullet/] Pressure: [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color]
plant-analyzer-printout-l15 = {$indent}[bullet/] Temperature: [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color]
plant-analyzer-printout-l16 = {$indent}[bullet/] Light: [color=gray][bold]{$lightLevel} ± {$lightTolerance}[/bold][/color]
plant-analyzer-printout-l17 = [bullet/] Flowers: [color=lightgreen]{$n} {$potency}[/color]{$seedless}
plant-analyzer-printout-l18 = [bullet/] Chemicals: [color=gray][bold]{$chemicals}[/bold][/color]
plant-analyzer-printout-l19 = [bullet/] Emissions: [bold]{$gasesOut}[/bold]
