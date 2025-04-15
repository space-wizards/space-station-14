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
cargo-console-menu-tab-title-orders = Orders
cargo-console-menu-tab-title-funds = Transfers
cargo-console-menu-account-action-transfer-limit = [bold]Transfer Limit:[/bold] ${$limit}
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Unlimited)[/color]
cargo-console-menu-account-action-select = [bold]Account Action:[/bold]
cargo-console-menu-account-action-amount = [bold]Amount:[/bold] $
cargo-console-menu-account-action-button = Transfer
cargo-console-menu-toggle-account-lock-button = Toggle Transfer Limit
cargo-console-menu-account-action-option-withdraw = Withdraw Cash
cargo-console-menu-account-action-option-transfer = Transfer Funds to {$code}

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
cargo-console-fund-withdraw-broadcast = [bold]{$name} withdrew {$amount} spesos from {$name1} \[{$code1}\]
cargo-console-fund-transfer-broadcast = [bold]{$name} transferred {$amount} spesos from {$name1} \[{$code1}\] to {$name2} \[{$code2}\][/bold]
cargo-console-fund-transfer-user-unknown = Unknown

cargo-console-paper-reason-default = None
cargo-console-paper-approver-default = Self
cargo-console-paper-print-name = Order #{$orderNumber}
cargo-console-paper-print-text = [head=2]Order #{$orderNumber}[/head]
    {"[bold]Item:[/bold]"} {$itemName} (x{$orderQuantity})
    {"[bold]Requested by:[/bold]"} {$requester}

    {"[head=3]Order Information[/head]"}
    {"[bold]Payer[/bold]:"} {$account} [font="Monospace"]\[{$accountcode}\][/font]
    {"[bold]Approved by:[/bold]"} {$approver}
    {"[bold]Reason:[/bold]"} {$reason}

# Cargo shuttle console
cargo-shuttle-console-menu-title = Cargo shuttle console
cargo-shuttle-console-station-unknown = Unknown
cargo-shuttle-console-shuttle-not-found = Not found
cargo-shuttle-console-organics = Detected organic lifeforms on the shuttle
cargo-no-shuttle = No cargo shuttle found!

# Funding allocation console
cargo-funding-alloc-console-menu-title = Funding Allocation Console
cargo-funding-alloc-console-label-account = [bold]Account[/bold]
cargo-funding-alloc-console-label-code = [bold] Code [/bold]
cargo-funding-alloc-console-label-balance = [bold] Balance [/bold]
cargo-funding-alloc-console-label-cut = [bold] Revenue Division (%) [/bold]

cargo-funding-alloc-console-label-help = Cargo receives {$percent}% of all profits. The rest is split as specified below:
cargo-funding-alloc-console-button-save = Save Changes
cargo-funding-alloc-console-label-save-fail = [bold]Revenue Divisions Invalid![/bold] [color=red]({$pos ->
    [1] +
    *[-1] -
}{$val}%)[/color]
