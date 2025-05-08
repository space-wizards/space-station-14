thruster-comp-enabled = The thruster is turned [color=green]on[/color].
thruster-comp-disabled = The thruster is turned [color=red]off[/color].
thruster-comp-nozzle-direction = The nozzle is facing [color=yellow]{$direction}[/color].
thruster-comp-nozzle-exposed = The nozzle [color=green]exposed[/color] to space.
thruster-comp-nozzle-not-exposed = The nozzle [color=red]is not exposed[/color] to space.

-thruster-setting-name =
    { $setting ->
        [maximum] maximum
        [high] high
        [medium] medium
        [low] low
       *[other] unknown
    }

thruster-examined = It is set to { $setting ->
    [maximum] [color=green]{ -thruster-setting-name(setting: "maximum") }[/color]
    [high] [color=yellow]{ -thruster-setting-name(setting: "high") }[/color]
    [medium] [color=orange]{ -thruster-setting-name(setting: "medium") }[/color]
    [low] [color=red]{ -thruster-setting-name(setting: "low") }[/color]
   *[other] [color=purple]{ -thruster-setting-name(setting: "other") }[/color]
}.
thruster-switch-setting = Switch to { -thruster-setting-name(setting: $setting) }
thruster-switched-setting = Switched to { -thruster-setting-name(setting: $setting) }.

