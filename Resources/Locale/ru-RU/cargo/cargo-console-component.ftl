## UI

cargo-console-menu-title = Консоль заказа грузов
cargo-console-menu-account-name-label = Имя аккаунта:{ " " }
cargo-console-menu-account-name-none-text = Нет
cargo-console-menu-account-name-format = [bold][color={ $color }]{ $name }[/color][/bold] [font="Monospace"]\[{ $code }\][/font]
cargo-console-menu-shuttle-name-label = Название шаттла:{ " " }
cargo-console-menu-shuttle-name-none-text = Нет
cargo-console-menu-points-label = Кредиты:{ " " }
cargo-console-menu-points-amount = ${ $amount }
cargo-console-menu-shuttle-status-label = Статус шаттла:{ " " }
cargo-console-menu-shuttle-status-away-text = Отбыл
cargo-console-menu-order-capacity-label = Объём заказов:{ " " }
cargo-console-menu-call-shuttle-button = Активировать телепад
cargo-console-menu-permissions-button = Доступы
cargo-console-menu-categories-label = Категории:{ " " }
cargo-console-menu-search-bar-placeholder = Поиск
cargo-console-menu-requests-label = Запросы
cargo-console-menu-orders-label = Заказы
cargo-console-menu-order-reason-description = Причина: { $reason }
cargo-console-menu-populate-categories-all-text = Все
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = { $productName } (x{ $orderAmount }) от { $orderRequester }
cargo-console-menu-cargo-order-row-approve-button = Одобрить
cargo-console-menu-cargo-order-row-cancel-button = Отменить
cargo-console-menu-tab-title-orders = Orders
cargo-console-menu-tab-title-funds = Transfers
cargo-console-menu-account-action-transfer-limit = [bold]Transfer Limit:[/bold] ${ $limit }
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Unlimited)[/color]
cargo-console-menu-account-action-select = [bold]Account Action:[/bold]
cargo-console-menu-account-action-amount = [bold]Amount:[/bold] $
cargo-console-menu-account-action-button = Transfer
cargo-console-menu-toggle-account-lock-button = Toggle Transfer Limit
cargo-console-menu-account-action-option-withdraw = Withdraw Cash
cargo-console-menu-account-action-option-transfer = Transfer Funds to { $code }
# Orders
cargo-console-order-not-allowed = Доступ запрещён
cargo-console-station-not-found = Нет доступной станции
cargo-console-invalid-product = Неверный ID продукта
cargo-console-too-many = Слишком много одобренных заказов
cargo-console-snip-snip = Заказ урезан до вместимости
cargo-console-insufficient-funds = Недостаточно средств (требуется { $cost })
cargo-console-unfulfilled = Нет места для выполнения заказа
cargo-console-trade-station = Отправить на { $destination }
cargo-console-unlock-approved-order-broadcast = [bold]Заказ на { $productName } x{ $orderAmount }[/bold], стоимостью [bold]{ $cost }[/bold], был одобрен [bold]{ $approver }[/bold]
cargo-console-fund-withdraw-broadcast = [bold]{ $name } withdrew { $amount } spesos from { $name1 } \[{ $code1 }\]
cargo-console-fund-transfer-broadcast = [bold]{ $name } transferred { $amount } spesos from { $name1 } \[{ $code1 }\] to { $name2 } \[{ $code2 }\][/bold]
cargo-console-fund-transfer-user-unknown = Unknown
cargo-console-paper-reason-default = None
cargo-console-paper-approver-default = Self
cargo-console-paper-print-name = Заказ #{ $orderNumber }
cargo-console-paper-print-text =
    Заказ #{ $orderNumber }
    Товар: { $itemName }
    Кол-во: { $orderQuantity }
    Запросил: { $requester }
    Причина: { $reason }
    Одобрил: { $approver }
# Cargo shuttle console
cargo-shuttle-console-menu-title = Консоль вызова грузового шаттла
cargo-shuttle-console-station-unknown = Неизвестно
cargo-shuttle-console-shuttle-not-found = Не найден
cargo-no-shuttle = Грузовой шаттл не найден!
cargo-shuttle-console-organics = На шаттле обнаружены органические формы жизни
# Funding allocation console
cargo-funding-alloc-console-menu-title = Funding Allocation Console
cargo-funding-alloc-console-label-account = [bold]Account[/bold]
cargo-funding-alloc-console-label-code = [bold] Code [/bold]
cargo-funding-alloc-console-label-balance = [bold] Balance [/bold]
cargo-funding-alloc-console-label-cut = [bold] Revenue Division (%) [/bold]
cargo-funding-alloc-console-label-primary-cut = Cargo's cut of funds from non-lockbox sources (%):
cargo-funding-alloc-console-label-lockbox-cut = Cargo's cut of funds from lockbox sales (%):
cargo-funding-alloc-console-label-help-non-adjustible = Cargo receives { $percent }% of profits from non-lockbox sales. The rest is split as specified below:
cargo-funding-alloc-console-label-help-adjustible = Remaining funds from non-lockbox sources are distributed as specified below:
cargo-funding-alloc-console-label-help = Cargo receives { $percent }% of all profits. The rest is split as specified below:
cargo-funding-alloc-console-button-save = Save Changes
# Slip template
cargo-acquisition-slip-body = [head=3]Asset Detail[/head]
    { "[bold]Product:[/bold]" } { $product }
    { "[bold]Description:[/bold]" } { $description }
    { "[bold]Unit cost:[/bold" }] ${ $unit }
    { "[bold]Amount:[/bold]" } { $amount }
    { "[bold]Cost:[/bold]" } ${ $cost }
    
    { "[head=3]Purchase Detail[/head]" }
    { "[bold]Orderer:[/bold]" } { $orderer }
    { "[bold]Reason:[/bold]" } { $reason }
cargo-funding-alloc-console-label-save-fail = [bold]Revenue Divisions Invalid![/bold] [color=red]({ $pos ->
        [1] +
       *[-1] -
    }{ $val }%)[/color]
