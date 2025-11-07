## UI

cargo-console-menu-title = Консоль заказа грузов
cargo-console-menu-account-name-label = Аккаунт:{ " " }
cargo-console-menu-account-name-none-text = Нет
cargo-console-menu-account-name-format = [bold][color={ $color }]{ $name }[/color][/bold] [font="Monospace"]\[{ $code }\][/font]
cargo-console-menu-shuttle-name-label = Название шаттла:{ " " }
cargo-console-menu-shuttle-name-none-text = Нет
cargo-console-menu-points-label = Баланс:{ " " }
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
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = { $productName } (x{ $orderAmount }) от { $orderRequester } со счёта [color={ $accountColor }]{ $account }[/color]
cargo-console-menu-cargo-order-row-approve-button = Одобрить
cargo-console-menu-cargo-order-row-cancel-button = Отменить
cargo-console-menu-tab-title-orders = Заказы
cargo-console-menu-tab-title-funds = Переводы
cargo-console-menu-account-action-transfer-limit = [bold]Лимит перевода:[/bold] ${ $limit }
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Безлимитно)[/color]
cargo-console-menu-account-action-select = [bold]Действие аккаунта:[/bold]
cargo-console-menu-account-action-amount = [bold]Сумма:[/bold] $
cargo-console-menu-account-action-button = Перевести
cargo-console-menu-toggle-account-lock-button = Переключить лимит перевода
cargo-console-menu-account-action-option-withdraw = Обналичить
cargo-console-menu-account-action-option-transfer = Перевести деньги на { $code }
# Orders
cargo-console-order-not-allowed = Доступ запрещён
cargo-console-station-not-found = Нет доступной станции
cargo-console-invalid-product = Неверный ID продукта
cargo-console-too-many = Слишком много одобренных заказов
cargo-console-snip-snip = Заказ урезан до вместимости
cargo-console-insufficient-funds = Недостаточно средств (требуется { $cost })
cargo-console-unfulfilled = Нет места для выполнения заказа
cargo-console-trade-station = Отправлено на { $destination }
cargo-console-unlock-approved-order-broadcast = [bold]Заказ на { $productName } x{ $orderAmount }[/bold], стоимостью [bold]{ $cost }[/bold], был одобрен [bold]{ $approver }[/bold]
cargo-console-fund-withdraw-broadcast = [bold]{ $name } снял { $amount } кредитов с { $name1 } \[{ $code1 }\]
cargo-console-fund-transfer-broadcast = [bold]{ $name } перевёл { $amount } кредитов { $name1 } \[{ $code1 }\] на { $name2 } \[{ $code2 }\][/bold]
cargo-console-fund-transfer-user-unknown = Неизвестно
cargo-console-paper-reason-default = Отсутствует
cargo-console-paper-approver-default = Самостоятельно
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
cargo-funding-alloc-console-menu-title = Консоль распределения финансов
cargo-funding-alloc-console-label-account = [bold]Аккаунт[/bold]
cargo-funding-alloc-console-label-code = [bold] Код [/bold]
cargo-funding-alloc-console-label-balance = [bold] Баланс [/bold]
cargo-funding-alloc-console-label-cut = [bold] Разделение дохода (%) [/bold]
cargo-funding-alloc-console-label-primary-cut = Доля дохода отдела снабжения от всего, кроме ящиков с замком (%):
cargo-funding-alloc-console-label-lockbox-cut = Доля дохода отдела снабжения от продажи ящиков с замком (%):
cargo-funding-alloc-console-label-help-non-adjustible = Отдел снабжения получает { $percent }% от всех доходов, кроме ящиков с замком. Остаток распределяется следующим образом:
cargo-funding-alloc-console-label-help-adjustible = Остаток доходов от всего, кроме ящиков с замком, распределяется следующим образом:
cargo-funding-alloc-console-button-save = Сохранить изменения
cargo-funding-alloc-console-label-save-fail = [bold]Разделение выручки невалидно![/bold] [color=red]({ $pos ->
        [1] +
       *[-1] -
    }{ $val }%)[/color]
# Slip template
cargo-acquisition-slip-body = [head=3]Детали актива[/head]
    { "[bold]Товар:[/bold]" } { $product }
    { "[bold]Описание:[/bold]" } { $description }
    { "[bold]Цена за единицу:[/bold" }] ${ $unit }
    { "[bold]Количество:[/bold]" } { $amount }
    { "[bold]Сумма:[/bold]" } ${ $cost }
    
    { "[head=3]Детали покупки[/head]" }
    { "[bold]Заказчик:[/bold]" } { $orderer }
    { "[bold]Причина:[/bold]" } { $reason }
