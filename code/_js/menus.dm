#define js_dropdowns {"
function dropdowns() {
    var divs = document.getElementsByTagName('div');
    var headers = new Array();
    var links = new Array();
    for(var i=0;i<divs.length;i++){
        if(divs\[i\].className=='header') {
            divs\[i\].className='header closed';
            divs\[i\].innerHTML = divs\[i\].innerHTML+' +';
            headers.push(divs\[i\]);
        }
        if(divs\[i\].className=='links') {
            divs\[i\].className='links hidden';
            links.push(divs\[i\]);
        }
    }
    for(var i=0;i<headers.length;i++){
        if(typeof(links\[i\])!== 'undefined' && links\[i\]!=null) {
            headers\[i\].onclick = (function(elem) {
                return function() {
                    if(elem.className.search('visible')>=0) {
                        elem.className = elem.className.replace('visible','hidden');
                        this.className = this.className.replace('open','closed');
                        this.innerHTML = this.innerHTML.replace('-','+');
                    }
                    else {
                        elem.className = elem.className.replace('hidden','visible');
                        this.className = this.className.replace('closed','open');
                        this.innerHTML = this.innerHTML.replace('+','-');
                    }
                return false;
                }
            })(links\[i\]);
        }
    }
}
"}
