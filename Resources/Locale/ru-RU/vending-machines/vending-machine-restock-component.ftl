vending-machine-restock-invalid-inventory = { CAPITALIZE($this) } не подходит для того, чтобы пополнить { $target }.
vending-machine-restock-needs-panel-open = Техническая панель { $target } должна быть открыта.
vending-machine-restock-start-self = Вы начинаете пополнять { $target }.
vending-machine-restock-start-others = { CAPITALIZE($user) } начинает пополнять { $target }.
vending-machine-restock-done-self = Вы закончили пополнять { $target }.
vending-machine-restock-done-others =
    { CAPITALIZE($user) } { GENDER($user) ->
        [male] закончил
        [female] закончила
        [epicene] закончили
       *[neuter] закончило
    } пополнять { $target }.
