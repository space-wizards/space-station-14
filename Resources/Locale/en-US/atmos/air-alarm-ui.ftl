# UI

## Window

air-alarm-ui-title = Air Alarm

air-alarm-ui-access-denied = Insufficient access!

air-alarm-ui-window-pressure-label = Pressure
air-alarm-ui-window-temperature-label = Temperature
air-alarm-ui-window-alarm-state-label = Status

air-alarm-ui-window-address-label = Address
air-alarm-ui-window-device-count-label = Total Devices
air-alarm-ui-window-resync-devices-label = Resync

air-alarm-ui-window-mode-label = Mode
air-alarm-ui-window-mode-select-locked-label = [bold][color=red] Mode selector failure! [/color][/bold]
air-alarm-ui-window-auto-mode-label = Auto mode

-air-alarm-state-name = { $state ->
    [normal] Normal
    [warning] Warning
    [danger] Danger
    [emagged] Emagged
   *[invalid] Invalid
}

air-alarm-ui-window-listing-title = {$address} : {-air-alarm-state-name(state:$state)}
air-alarm-ui-window-pressure = {$pressure} kPa
air-alarm-ui-window-pressure-indicator = Pressure: [color={$color}]{$pressure} kPa[/color]
air-alarm-ui-window-temperature = {$tempC} C ({$temperature} K)
air-alarm-ui-window-temperature-indicator = Temperature: [color={$color}]{$tempC} C ({$temperature} K)[/color]
air-alarm-ui-window-alarm-state = [color={$color}]{-air-alarm-state-name(state:$state)}[/color]
air-alarm-ui-window-alarm-state-indicator = Status: [color={$color}]{-air-alarm-state-name(state:$state)}[/color]

air-alarm-ui-window-tab-vents = Vents
air-alarm-ui-window-tab-scrubbers = Scrubbers
air-alarm-ui-window-tab-sensors = Sensors

air-alarm-ui-gases = {$gas}: {$amount} mol ({$percentage}%)
air-alarm-ui-gases-indicator = {$gas}: [color={$color}]{$amount} mol ({$percentage}%)[/color]

air-alarm-ui-mode-filtering = Filtering
air-alarm-ui-mode-wide-filtering = Filtering (wide)
air-alarm-ui-mode-fill = Fill
air-alarm-ui-mode-panic = Panic
air-alarm-ui-mode-none = None


air-alarm-ui-pump-direction-siphoning = Siphoning
air-alarm-ui-pump-direction-scrubbing = Scrubbing
air-alarm-ui-pump-direction-releasing = Releasing

air-alarm-ui-pressure-bound-nobound = No Bound
air-alarm-ui-pressure-bound-internalbound = Internal Bound
air-alarm-ui-pressure-bound-externalbound = External Bound
air-alarm-ui-pressure-bound-both = Both

air-alarm-ui-widget-gas-filters = Gas Filters

## Widgets

### General

air-alarm-ui-widget-enable = Enabled
air-alarm-ui-widget-copy = Copy settings to similar devices
air-alarm-ui-widget-copy-tooltip = Copies the settings of this device to all devices in this air alarm tab.
air-alarm-ui-widget-ignore = Ignore
air-alarm-ui-atmos-net-device-label = Address: {$address}

### Vent pumps

air-alarm-ui-vent-pump-label = Vent direction
air-alarm-ui-vent-pressure-label = Pressure bound
air-alarm-ui-vent-external-bound-label = External bound
air-alarm-ui-vent-internal-bound-label = Internal bound

### Scrubbers

air-alarm-ui-scrubber-pump-direction-label = Direction
air-alarm-ui-scrubber-volume-rate-label = Rate (L)
air-alarm-ui-scrubber-wide-net-label = WideNet
air-alarm-ui-scrubber-select-all-gases-label = Select all
air-alarm-ui-scrubber-deselect-all-gases-label = Deselect all

### Thresholds

air-alarm-ui-sensor-gases = Gases
air-alarm-ui-sensor-thresholds = Thresholds
air-alarm-ui-thresholds-pressure-title = Thresholds (kPa)
air-alarm-ui-thresholds-temperature-title = Thresholds (K)
air-alarm-ui-thresholds-gas-title = Thresholds (%)
air-alarm-ui-thresholds-upper-bound = Danger above
air-alarm-ui-thresholds-lower-bound = Danger below
air-alarm-ui-thresholds-upper-warning-bound = Warning above
air-alarm-ui-thresholds-lower-warning-bound = Warning below
air-alarm-ui-thresholds-copy = Copy thresholds to all devices
air-alarm-ui-thresholds-copy-tooltip = Copies the sensor thresholds of this device to all devices in this air alarm tab.
