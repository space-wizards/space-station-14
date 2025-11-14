cmd-deletecomponent-desc = Удаляет все случаи указаного компонента.
cmd-deletecomponent-help = Использование: deletecomponent <name>"
cmd-deletecomponent-no-component-exists = Компонента с именем { $name } не существует.
cmd-deletecomponent-success =
    { $count ->
        [one] Удалён { $count } компонент
        [few] Удалено { $count } компонента
       *[other] Удалено { $count } компонентов
    } с именем { $name }.
