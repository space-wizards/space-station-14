airlock-controller-access-denied = Access Denied

# Status window

airlock-controller-ui-title = Airlock Controller
airlock-controller-ui-state-label = Status:
airlock-controller-ui-side-label = Chamber at:
airlock-controller-ui-pressure-label = Chamber pressure:
airlock-controller-ui-doors-label = Doors:
airlock-controller-ui-chamber-sensors-label = Chamber sensors:
airlock-controller-ui-address-label = Address:

airlock-controller-ui-pressure-value = {$pressure} kPa
airlock-controller-ui-pressure-unknown = No sensors reporting

airlock-controller-ui-cycle-a = Cycle to side A
airlock-controller-ui-cycle-b = Cycle to side B
airlock-controller-ui-cancel = Cancel cycle
airlock-controller-ui-cancel-emergency = Emergency release
airlock-controller-ui-configure = Config

airlock-controller-state-idle = Idle
airlock-controller-state-sealing = Sealing
airlock-controller-state-evacuating = Evacuating
airlock-controller-state-filling = Filling
airlock-controller-state-unsealing = Unsealing

airlock-controller-side-a = Side A
airlock-controller-side-b = Side B

airlock-controller-stall-no-vent = No vent configured for this direction.
airlock-controller-stall-no-sensors = No sensors reporting from the chamber.
airlock-controller-stall-not-progressing = Chamber pressure is not changing. Check the pipes.
airlock-controller-stall-seal-lost = A door is open. The cycle cannot continue.
airlock-controller-stall-not-sealing = Doors are not sealing. Check their power.
airlock-controller-stall-missing-doors = Needs a door assigned to each side.
airlock-controller-stall-not-opening = The doors will not open. Check their power.

# Examine

airlock-controller-examine-state = Status: [color=white]{$state}[/color]
airlock-controller-examine-side = Chamber holds [color=white]{$side}[/color]
airlock-controller-examine-error = [color=red]{$reason}[/color]
airlock-controller-examine-maintenance = [color=yellow]Under maintenance.[/color]

# Config window

airlock-controller-config-ui-title = Airlock Controller Configuration
airlock-controller-config-preset-a = Side A pressure (kPa)
airlock-controller-config-preset-b = Side B pressure (kPa)
airlock-controller-config-preset-hint = Assign a target sensor to a side to match it instead of the pressure above.
airlock-controller-config-device-header = [bold]{$name}[/bold] ({$address})
airlock-controller-config-door-side = Door side:
airlock-controller-config-cycler-side = Calls to side:
airlock-controller-config-sensor-custom = Custom
airlock-controller-config-sensor-pending = ~
airlock-controller-config-none = None
airlock-controller-config-vent-a = Vent A
airlock-controller-config-siphon-a = Siphon A
airlock-controller-config-vent-b = Vent B
airlock-controller-config-siphon-b = Siphon B
airlock-controller-config-maintenance = Maintenance mode
airlock-controller-config-force-label = Force chamber to:

# Cycler panel

airlock-cycler-ui-title = Airlock Cycler
airlock-cycler-ui-cycle = Call airlock
airlock-cycler-ui-unbound = Not assigned
airlock-cycler-examine-unbound = [color=yellow]No controller is using this.[/color]
