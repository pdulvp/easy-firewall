/** 
 This Code is published under the terms and conditions of the CC-BY-NC-ND-4.0
 (https://creativecommons.org/licenses/by-nc-nd/4.0)
 
 Please contribute to the current project.
 
 SPDX-License-Identifier: CC-BY-NC-ND-4.0
 @author: pdulvp@laposte.net
*/

var fonts = undefined;

function random(min, max, count) {
	let result = [];
	while (count > 0) {
		let int = Math.floor(Math.random()*(max - min + 1))+min;
		if (result.length > 0) {
			while (result[result.length - 1] == int) {
				int = Math.floor(Math.random()*(max - min + 1))+min;
			}
		}
		result.push(int);
		count --;
	}
	return result;
}

function hasClass(item, value) {
	return item.getAttribute("class") != null && (item.getAttribute("class").includes(value));
}

function removeClass(item, value) {
	if (hasClass(item, value)) {
		item.setAttribute("class", item.getAttribute("class").replace(value, "").trim());
	}
}

function addClass(item, value) {
    if (!hasClass(item, value)) {
        let current = item.getAttribute("class");
        current = current == null ? "" : current;
        item.setAttribute("class", (current+ " "+value+" ").trim());
    }
}

let ticking = false;
let timeout = null;

function hideModal() {
	addClass(document.getElementById("modal-back"), "modal-hide");
	//addClass(document.getElementById("modal-about"), "modal-hide");
	//addClass(document.getElementById("modal-license"), "modal-hide");
	addClass(document.getElementById("modal-sponsor"), "modal-hide");
}

document.getElementById("modal-back").onclick = function(e) {
	hideModal();
}
/*document.getElementById("modal-about").onclick = function(e) {
	hideModal();
}
document.getElementById("modal-license").onclick = function(e) {
	hideModal();
}*/
document.getElementById("modal-sponsor").onclick = function(e) {
	hideModal();
}

function showModal(e) {
	let modalId = e.target.getAttribute("modal");
	let view = document.getElementById(modalId);
	removeClass(document.getElementById("modal-back"), "modal-hide");
	removeClass(view, "modal-hide");
}

//document.getElementById("link-about").onclick = showModal;
document.getElementById("link-sponsor").onclick = showModal;
//document.getElementById("link-license").onclick = showModal;


function updateWindow(event) {
	if (!ticking) {
		window.requestAnimationFrame(function() {
		  ticking = false;
		});
		ticking = true;
	}
}
window.addEventListener('scroll', updateWindow);
window.onresize = updateWindow;


var httpq = require("@pdulvp/httpquery");
httpq.get("https://raw.githubusercontent.com/pdulvp/easy-firewall/site/README.md").then(e => {
	

	e = e.replace(/### ([^(###)\n]+) ###/g, "<h3>$1</h3>");
	e = e.replace(/## ([^(##)\n]+) ##/g, "<h2>$1</h2>");
	e = e.replace(/# ([^(#)\n]+) #/g, "<h1>$1</h1>");
	e = e.replace(/`([a-zA-Z0-9]+)`/g, "<span class=\"span-whoa1\">$1</span>");
	e = e.replace(/!\[([\w ]+)\]\(([^\)]+)\)/g, "<img title=\"$1\" src=\"$2\"/>");
	e = e.replace(/\[([\w ]+)\]\(([^\)]+)\)/g, "<a title=\"$1\" href=\"$2\">$1</a>");
	document.getElementById("main-section").innerHTML = e;
	
	colorize(document.getElementsByTagName("h1")[0]);
	
}).catch(e => {
	console.log(e);
});

function colorize(element) {
	let str=  element.innerText;
	let result = "";
	let colors = random(1,3,str.length);
	console.log(colors);
	for (var i = 0; i < str.length; i++) {
		result+='<span class="span-color'+colors[i]+'">'+str.charAt(i)+'</span>';
	}
	element.innerHTML=result;
}

//colorize(document.getElementById("whaoo"));

function animate(element) {
	window.setTimeout(function(e) {
		element.setAttribute("style", "display:none");
		window.setTimeout(function(e) {
			element.setAttribute("style", "display:inline");
			animate(element);
		}, 500);
	}, 500);
}

//animate(document.getElementById("alert2"));