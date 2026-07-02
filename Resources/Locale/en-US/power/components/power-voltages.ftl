power-voltage-low = [color=green]Low[/color] voltage
power-voltage-medium = [color=yellow]Medium[/color] voltage
power-voltage-high = [color=orange]High[/color] voltage

power-voltage = { $voltage ->
    [HV] [color=orange]HV[/color]
    [MV] [color=yellow]MV[/color]
    *[LV] [color=green]LV[/color]
}
