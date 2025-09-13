# UI
mech-menu-title = mech control panel
mech-equipment-label = Equipment
mech-modules-label = Modules

# Verbs
mech-verb-enter = Enter
mech-verb-exit = Remove pilot
mech-ui-open-verb = Open control panel

# Installation
mech-install-begin-popup = Installing the {THE($item)}...
mech-install-finish-popup = Finished installing the {THE($item)}
mech-cannot-modify-closed-popup = You cannot modify while the cabin is closed!
mech-duplicate-installed-popup = Identical item already installed.
mech-cannot-insert-broken-popup = You cannot insert anything while the mech is in broken state.

mech-equipment-slot-full-popup = No free equipment slots.
mech-module-slot-full-popup = No free module slots.
mech-equipment-whitelist-fail-popup = Equipment not compatible with this mech.
mech-module-whitelist-fail-popup = Module not compatible with this mech.

# Selection
mech-select-popup = {$item} selected
mech-select-none-popup = Nothing selected

# Radial menu
mech-radial-no-equipment = No Equipment

# Status displays
mech-integrity-display-label = Integrity
mech-integrity-display = {$amount} %
mech-integrity-display-broken = BROKEN
mech-energy-display-label = Energy
mech-energy-display = {$amount} %
mech-energy-missing = MISSING

mech-equipment-slot-display = Equipment: {$used}/{$max} used
mech-module-slot-display = Modules: {$used}/{$max} used
mech-grabber-capacity = {$current}/{$max}
mech-no-data-status = No airtight

mech-generator-output = Output: {$rate} W
mech-generator-fuel = Fuel: {$amount} ({$name})

# Atmospheric system
mech-cabin-pressure-label = Cabin Air:
mech-cabin-pressure-level = {$level} kPa
mech-cabin-temperature-label = Temperature:
mech-cabin-temperature-level = {$tempC} °C
mech-air-toggle = Toggle
mech-cabin-purge = Purge
mech-airtight-unavailable = Not airtight cabin

mech-tank-pressure-label = Tank Air:
mech-tank-pressure-level = { $state ->
    [ok] {$pressure} kPa
    *[na] N/A
}

# Fan system
mech-fan-label = Fan:
mech-fan-on = On
mech-fan-off = Off
mech-fan-toggle = Toggle Fan
mech-fan-status-label = Fan Status:
mech-fan-status = { $state ->
    [on] On
    [idle] Idle
    [off] Off
    *[na] N/A
}
mech-fan-missing = No fan module
mech-filter-enabled = Filter

# Access restriction
mech-no-enter-popup = You cannot pilot this.

# Alert
mech-eject-pilot-alert-popup = {$user} is pulling the pilot out of the {$item}!

# Lock system
mech-lock-dna-label = DNA Lock:
mech-lock-card-label = ID Lock:

mech-lock-register = Register Lock
mech-lock-activate = Activate
mech-lock-deactivate = Deactivate
mech-lock-reset = Reset

mech-lock-no-dna-popup = You don't have DNA to lock with!
mech-lock-no-card-popup = You don't have an ID card to lock with!
mech-lock-access-denied-popup = Access denied! This mech is locked.

mech-lock-dna-registered-popup = DNA lock registered!
mech-lock-card-registered-popup = ID lock registered!
mech-lock-activated-popup = Lock activated!
mech-lock-deactivated-popup = Lock deactivated!
mech-lock-reset-success-popup = Lock reset!
mech-lock-not-set = Not set

# Settings access banner
mech-settings-no-access = Access denied
mech-remove-disabled-tooltip = Cannot remove while a pilot is inside.

# Other
mech-construction-guide-string = All mech parts must be attached to the harness.
