atmos-debug-command-range-description = Sets the atmos debug range (as two floats, start [red] and end [blue])
atmos-debug-command-range-help = Usage: {$command} <start> <end>
atmos-debug-command-range-error-start = Bad float START
atmos-debug-command-range-error-end = Bad float END
atmos-debug-command-range-error-zero = Scale cannot be zero, as this would cause a division by zero in AtmosDebugOverlay.

atmos-debug-command-mode-description = Sets the atmos debug mode. This will automatically reset the scale.
atmos-debug-command-mode-help = Usage: {$command} <TotalMoles/GasMoles/Temperature> [<gas ID (for GasMoles)>]
atmos-debug-command-mode-error-invalid = Invalid mode
atmos-debug-command-mode-error-target-gas = A target gas must be provided for this mode.
atmos-debug-command-mode-error-out-of-range = Gas ID not parsable or out of range.
atmos-debug-command-mode-error-info = No further information is required for this mode.

atmos-debug-command-cbm-description = Changes from red/green/blue to greyscale
atmos-debug-command-cbm-help = Usage: {$command} <true/false>
atmos-debug-command-cbm-error = Invalid flag
