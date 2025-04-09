## UI
cargo-console-menu-title = Cargo request console
cargo-console-menu-account-name-label = Account:{" "}
cargo-console-menu-account-name-none-text = None
cargo-console-menu-account-name-format = [bold][color={$color}]{$name}[/color][/bold] [font="Monospace"]\[{$code}\][/font]
cargo-console-menu-shuttle-name-label = Shuttle name:{" "}
cargo-console-menu-shuttle-name-none-text = None
cargo-console-menu-points-label = Balance:{" "}
cargo-console-menu-points-amount = ${$amount}
cargo-console-menu-shuttle-status-label = Shuttle status:{" "}
cargo-console-menu-shuttle-status-away-text = Away
cargo-console-menu-order-capacity-label = Order capacity:{" "}
cargo-console-menu-call-shuttle-button = Activate telepad
cargo-console-menu-permissions-button = Permissions
cargo-console-menu-categories-label = Categories:{" "}
cargo-console-menu-search-bar-placeholder = Search
cargo-console-menu-requests-label = Requests
cargo-console-menu-orders-label = Orders
cargo-console-menu-order-reason-description = Reasons: {$reason}
cargo-console-menu-populate-categories-all-text = All
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = {$productName} (x{$orderAmount}) by {$orderRequester}
cargo-console-menu-cargo-order-row-approve-button = Approve
cargo-console-menu-cargo-order-row-cancel-button = Cancel

# Orders
cargo-console-order-not-allowed = Access not allowed
cargo-console-station-not-found = No available station
cargo-console-invalid-product = Invalid product ID
cargo-console-too-many = Too many approved orders
cargo-console-snip-snip = Order trimmed to capacity
cargo-console-insufficient-funds = Insufficient funds (require {$cost})
cargo-console-unfulfilled = No room to fulfill order
cargo-console-trade-station = Sent to {$destination}
cargo-console-unlock-approved-order-broadcast = [bold]{$productName} x{$orderAmount}[/bold], which cost [bold]{$cost}[/bold], was approved by [bold]{$approver}[/bold]

cargo-console-paper-reason-default = None
cargo-console-paper-approver-default = Self
cargo-console-paper-print-name = Order #{$orderNumber}
cargo-console-paper-print-text = [head=2]Order #{$orderNumber}[/head]
    {"[bold]Item:[/bold]"} {$itemName} (x{$orderQuantity})
    {"[bold]Requested by:[/bold]"} {$requester}

    {"[head=3]Order Information[/head]"}
    {"[bold]Payer[/bold]:"} {$account} [font="Monospace"]\[{$accountcode}\][/font]
    {"[bold]Reason:[/bold]"} {$reason}
    {"[bold]Approved by:[/bold]"} {$approver}

# Cargo shuttle console
cargo-shuttle-console-menu-title = Cargo shuttle console
cargo-shuttle-console-station-unknown = Unknown
cargo-shuttle-console-shuttle-not-found = Not found
cargo-shuttle-console-organics = Detected organic lifeforms on the shuttle
cargo-no-shuttle = No cargo shuttle found!
