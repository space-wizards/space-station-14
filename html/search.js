function selectTextField(){
	var filter_text = document.getElementById('filter');
	filter_text.focus();
	filter_text.select();
}
function updateSearch(){
	var input_form = document.getElementById('filter');
	var filter = input_form.value.toLowerCase();
	input_form.value = filter;
	var table = document.getElementById('searchable');
	var alt_style = 'norm';
	for(var i = 0; i < table.rows.length; i++){
		try{
			var row = table.rows[i];
			if(row.className == 'title') continue;
			var found=0;
			for(var j = 0; j < row.cells.length; j++){
				var cell = row.cells[j];
				if(cell.innerText.toLowerCase().indexOf(filter) != -1){
					found=1;
					break;
				}
			}
			if(found == 0) row.style.display='none';
			else{
				row.style.display='block';
				row.className = alt_style;
				if(alt_style == 'alt') alt_style = 'norm';
				else alt_style = 'alt';
			}
		}catch(err) { }
	}
}