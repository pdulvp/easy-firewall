/**
 * Author: pdulvp@laposte.net
 * Licence: CC-BY-NC-SA-4.0
 *
 * This module can be used both on nodeJs server and http client. It defined a http module with GET access
 * based on existing components. 
 * 
 * On NodeJS, it use the http module.
 * On JQuery, it use $.ajax
 */

if (typeof require !== "undefined" && typeof exports === 'object' && typeof module === 'object') { 

	var http = require("http");
	var https = require("https");
	var url = require('url'); 
	var fs = require("fs");

	var httpquery = {
		get: function(uri) {
			return httpquery.request(uri);
		},
		
		post: function(uri, object) {
			return httpquery.request(uri, object);
		},
		
		request: function(uri, object) {
			if (typeof uri === 'string' || uri instanceof String) {
				uri = url.parse(uri, true);
			}
			return new Promise((resolve, reject) => {
				let data = undefined; 
				if (object != undefined) {
					data = JSON.stringify(object); //querystring.stringify();
				}
				let options = {
					host: uri.hostname,
					port: uri.port != undefined ? uri.port : (uri.protocol == "http:" ? 80: 443),
					path: uri.path,
					method: (data == undefined ? 'GET': 'POST'),
					headers: { }
				};
				if (data != undefined) {
					options.headers['Content-Type'] = 'application/x-www-form-urlencoded';
					options.headers['Content-Length'] = Buffer.byteLength(data);
				}
				let which = uri.protocol == "http:" ? http: https;
				let req = which.request(options, function(res) {
				    let body = '';
				    res.on('data', function(chunk) {
				    	body += chunk;
				    });
				    res.on('end', function() {
						if (res.statusCode == 404) {
							reject(res.statusCode);
						} else {
							resolve(body);
						}
				    });
					
				}).on('error', function(e) {
					console.log("Got error: " + e.message);
					reject(e);
				});
				if (data != undefined) {
					req.write(data);
				}
				req.end();
			});
		},
		
		saveTo: function(uri, filename) {
			if (typeof uri === 'string' || uri instanceof String) {
				uri = url.parse(uri, true);
			}
			let which = uri.protocol == "http:" ? http: https;
			
			return new Promise((resolve, reject) => {
				const file = fs.createWriteStream(filename);
				let options = {
					host: uri.hostname,
					port: uri.port != undefined ? uri.port : (uri.protocol == "http:" ? 80: 443),
					path: uri.path,
					method: 'GET',
					headers: { }
				};
				const request = which.get(options, function(response) {
					response.pipe(file);
				});
				request.on("finish", function() {
					resolve(filename);
				});
				request.on("error", function(e) {
					reject(e);
				});
			});
		}
	};

	//export to nodeJs
	if(typeof exports === 'object' && typeof module === 'object') {
		module.exports = httpquery;
	}
	
} else if (typeof $ !== "undefined") {
	
	let httpquery = {
		get: function(url) {
			return new Promise((resolve, reject) => {
				$.ajax({
				  url: `${url}`,
				  data: null,
				  success: function( result ) {
					resolve(result);
				  },
				  error: function( result ) {
					reject(result);
				  }
				});
			});
		}, 
		post: function(url, data) {
			return new Promise((resolve, reject) => {
				$.ajax({
				  type: "POST",
				  dataType: "json",
				  contentType:"application/json",
				  url: `${url}`,
				  data: data,
				  success: function( result ) {
					resolve(result);
				  },
				  error: function( result ) {
					reject(result);
				  }
				});
			});
		}
	};

	if (typeof modules === 'object') {
		modules.register("@pdulvp/httpquery", httpquery);
	}
	
} else if (window.XMLHttpRequest) {
	let httpquery = {
		get: function(url) {
			return new Promise((resolve, reject) => {
				let xmlhttp=new XMLHttpRequest();
				xmlhttp.onload = function (e) {
					if (xmlhttp.readyState === 4) {
					  if (xmlhttp.status === 200) {
						resolve(xmlhttp.responseText);
					  } else {
						reject(xmlhttp.statusText);
					  }
					}
				  };
				  xmlhttp.onerror = function (e) {
					reject(xmlhttp.statusText);
				  };
				xmlhttp.open("GET", url, true);
				xmlhttp.send(null); 
			});
		}
	};

	if (typeof modules === 'object') {
		modules.register("@pdulvp/httpquery", httpquery);
	}
 
} else {
	console.error("module httpquery can't be initialized");
}
