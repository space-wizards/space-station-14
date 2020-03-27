function toggle_head(source, ext) {
    document.getElementById(source.id.slice(0, -4) + ext).checked = source.checked;
}

function toggle_checkboxes(source, ext) {
    var checkboxes = document.getElementsByClassName(source.name);
    for (var i = 0, n = checkboxes.length; i < n; i++) {
        checkboxes[i].checked = source.checked;
        if (checkboxes[i].id) {
            var idfound = document.getElementById(checkboxes[i].id.slice(0, -4) + ext);
            if (idfound) {
                idfound.checked = source.checked;
            }
        }
    }
}
