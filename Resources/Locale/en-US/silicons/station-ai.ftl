# General
ai-wire-snipped = One of your systems' wires has been cut at {$source}.
wire-name-ai-vision-light = AIV
wire-name-ai-act-light = AIA
station-ai-takeover = AI takeover
station-ai-eye-name = AI eye - {$name}
station-ai-has-no-power-for-upload = Upload failed - the AI core is unpowered.
station-ai-is-too-damaged-for-upload = Upload failed - the AI core must be repaired.
station-ai-core-losing-power = Your AI core is now running on reserve battery power.
station-ai-core-critical-power = Your AI core is critically low on power. External power must be re-established or severe data corruption may occur!
station-ai-leave-round-sender = Automated AI diagnostics
station-ai-leave-round-announcement = Core purge procedure for {$name} is complete. The consciousness matrix has been unloaded and the core is now in standby.
station-ai-leave-round-no-mind = Unable to find an active AI mind.
station-ai-leave-round-failed = Unable to leave the AI core.
station-ai-leave-round-confirmation-title = AI Core Purge
station-ai-leave-round-confirmation-warning-title = Warning!
station-ai-leave-round-confirmation-warning-body =
    Purging the core will permanently disconnect your consciousness from the AI.
    After confirmation you will leave the round: you will not be able to return as the AI or return to the lobby.
station-ai-leave-round-confirmation-cancel = Cancel
station-ai-leave-round-confirmation-confirm = Purge core
station-ai-admin-erase-core = Erase AI core
station-ai-admin-erase-core-description = Forcefully purge the AI core, ghost the current player, and free the role.

# Ghost role
station-ai-ghost-role-name = Station AI
station-ai-ghost-role-description = Serve the station crew as its ever watchful AI.

# Radial actions
ai-open = Open actions
ai-close = Close actions

bolt-close = Close bolt
bolt-open = Open bolt

emergency-access-on = Enable emergency access
emergency-access-off = Disable emergency access

electrify-door-on = Enable overcharge
electrify-door-off = Disable overcharge

toggle-light = Toggle light

ai-device-not-responding = Device is not responding
ai-device-no-access = You have no access to this device

ai-consciousness-download-warning = Your consciousness is being downloaded.

# UI
station-ai-customization-menu = AI customization
station-ai-customization-categories = Categories
station-ai-customization-options = Options (choice of one)
station-ai-customization-core = AI core displays
station-ai-customization-hologram = Holographic avatars
station-ai-camera-window-title = Camera viewer
station-ai-camera-tab-cameras = Cameras
station-ai-camera-tab-search = Search
station-ai-camera-map-legend-camera = Camera
station-ai-camera-not-working = Camera is offline!
station-ai-camera-search-placeholder = Crew member or item name
station-ai-camera-search-button = Search
station-ai-camera-search-hint = Enter 2 to 64 characters.
station-ai-camera-search-too-short = Enter at least {$count} characters.
station-ai-camera-search-too-long = Enter no more than {$count} characters.
station-ai-camera-search-empty = No targets found.
station-ai-camera-search-count = Targets found: {$count}.
station-ai-camera-search-cooldown = Camera search module is recalibrating. Try again in {$seconds}s.
station-ai-camera-search-invalid = Target is unavailable.
station-ai-camera-search-no-eye = AI eye is unavailable.
station-ai-camera-search-not-visible = Target was not found on accessible cameras.
station-ai-camera-jump-cooldown = Camera targeting array is recalibrating. Try again in {$seconds}s.
station-ai-camera-search-type-all = All
station-ai-camera-search-type-characters = Crew
station-ai-camera-search-type-items = Items
station-ai-camera-search-result-character = crew
station-ai-camera-search-result-item = item
station-ai-camera-search-result = [{$type}] {$name}
station-ai-centcomm-fax-window-title = AI-CC Facsimile Communications Line
station-ai-centcomm-fax-channel = AI-CC / Central Command
station-ai-centcomm-fax-route = Secure Central Command facsimile line
station-ai-centcomm-fax-buffer-label = Address buffer
station-ai-centcomm-fax-footer-route = [sai@nanotrasen] $ uplink --route ai-cc-fax
station-ai-centcomm-fax-footer-code = AI-CC
station-ai-centcomm-fax-content-placeholder = Enter an address for Central Command...
station-ai-centcomm-fax-content-limit = Buffer: { $count }/{ $max }
station-ai-centcomm-fax-send = Send fax
station-ai-centcomm-fax-status-initializing = Awaiting input
station-ai-centcomm-fax-status-ready = AI-CC line is ready for transmission.
station-ai-centcomm-fax-status-empty = Address buffer is empty.
station-ai-centcomm-fax-status-cooldown = AI-CC line is resynchronizing: { $seconds }s.
station-ai-centcomm-fax-status-unavailable = AI-CC transmission was not confirmed by the Central Command facsimile node.
station-ai-centcomm-fax-status-sent = Address accepted by the Central Command facsimile node.
station-ai-centcomm-fax-notice-deactivation = [color=#d6a54b]Use of this channel without substantial cause will result in immediate AI core deactivation.[/color]
station-ai-centcomm-fax-notice-no-response = [color=#7fa7b8]Central Command does not guarantee a response to received messages.[/color]
station-ai-centcomm-fax-station-unknown = unknown station
station-ai-centcomm-fax-source-name = Station AI { $name }
station-ai-centcomm-fax-stamp = AI-CC channel
station-ai-centcomm-fax-document-name = AI-CC - station AI address
station-ai-centcomm-fax-document =
    -- [head=3]{ $station }[/head] --
    -- [head=3]AI-CC[/head] --
    ═════════════════════════════════════
    :: [bold]Machine address from station AI[/bold]
    ═════════════════════════════════════
    Shift time and date: { $time } { $date }
    Document compiler: { $sender }
    Compiler position: Station artificial intelligence
    ─────────────────────────────────────
    Communications line code: AI-CC
    Route: secure Central Command facsimile line
    Delivery status: accepted by the Central Command facsimile node
    ─────────────────────────────────────
    Address text:
    { $content }
    ═════════════════════════════════════
    -- [italic]Stamp area[/italic] --

cmd-ai-track-entity-desc = Moves the AI eye to a target available through cameras or coordinate sensors.
cmd-ai-track-entity-help = Usage: {$command} <netEntity>

# Customizations
station-ai-icon-ai = Ghost in the machine
station-ai-icon-angel = Guardian angel
station-ai-icon-bliss = Simpler times
station-ai-icon-clown = Clownin' around
station-ai-icon-dorf = Adventure awaits
station-ai-icon-heartline = Lifeline
station-ai-icon-smiley = All smiles
station-ai-icon-murica = Murica
station-ai-icon-nanotrasen = Nanotrasen

station-ai-hologram-female = Female appearance
station-ai-hologram-male = Male appearance
station-ai-hologram-face = Disembodied head
station-ai-hologram-cat = Cat form
station-ai-hologram-dog = Corgi form
