lathe-menu-title = Menú del Fabricador
lathe-menu-queue = Cola
lathe-menu-server-list = Lista de servidores
lathe-menu-sync = Sincronizar
lathe-menu-search-designs = Buscar diseños
lathe-menu-category-all = Todos
lathe-menu-search-filter = Filtro:
lathe-menu-amount = Cantidad:
lathe-menu-reagent-slot-examine = Tiene un compartimento para un matraz en el costado.
lathe-reagent-dispense-no-container = ¡El líquido sale de {THE($name)} y cae al suelo!
lathe-menu-result-reagent-display = {$reagent} ({$amount}u)
lathe-menu-material-display = {$material} ({$amount})
lathe-menu-tooltip-display = {$amount} de {$material}
lathe-menu-description-display = [italic]{$description}[/italic]
lathe-menu-material-amount = { $amount ->
    [1] {NATURALFIXED($amount, 2)} {$unit}
    *[other] {NATURALFIXED($amount, 2)} {MAKEPLURAL($unit)}
}
lathe-menu-material-amount-missing = { $amount ->
    [1] {NATURALFIXED($amount, 2)} {$unit} de {$material} ([color=red]{NATURALFIXED($missingAmount, 2)} {$unit} faltante[/color])
    *[other] {NATURALFIXED($amount, 2)} {MAKEPLURAL($unit)} de {$material} ([color=red]{NATURALFIXED($missingAmount, 2)} {MAKEPLURAL($unit)} faltante[/color])
}
lathe-menu-no-materials-message = No hay materiales cargados.
lathe-menu-fabricating-message = Fabricando...
lathe-menu-materials-title = Materiales
lathe-menu-queue-title = Cola de Construcción
