## SuspicionGui.xaml.cs

# Shown when clicking your Role Button in Suspicion
suspicion-ally-count-display = {$allyCount ->
    *[zero] No tienes aliados
    [one] Tu aliado es {$allyNames}
    [other] Tus aliados son {$allyNames}
}