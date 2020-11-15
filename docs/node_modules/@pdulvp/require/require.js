/**
 * Author: pdulvp@laposte.net
 * Licence: CC-BY-NC-SA
 * 
 * This file is NOT a module compatible with NodeJs.
 * It allows to use the require(module) format in browser js files 
 * and to allow basic nodejs modules to be run into browser.
 *
 * Mechanism: 
 * - Modules have to be imported in <script> file. Order of <script> is important, based on require dependencies.
 * - Nodules have to register to modules.register(moduleName, module) to be accessible through require() method. 
 */
 if (typeof require === 'undefined' && typeof exports === 'undefined' && typeof module === 'undefined') { 
	modules = {
		register: function(moduleName, module) {
			//otherwise, we use the shared variable used on the module
			if (modules[moduleName] === undefined) {
				modules[moduleName] = module;
				console.info(`[pdulvp-require] module '${moduleName}' successfully registered`);
			} else {
				console.error(`[pdulvp-require] module '${moduleName}' already registered`);
			}
		}
	};
	
	function require(moduleName) {
		//a bit of hack allowing some local debug.
		if (moduleName.startsWith("./")) {
			moduleName = moduleName.substring(2);
		}
		
		//load from an already loaded module
		if (modules[moduleName] !== undefined) {
			//console.info(`[pdulvp-require] module '${moduleName}' successfully loaded`);
			return modules[moduleName];
		}

		console.error(`[pdulvp-require] module '${moduleName}' is missing`);
		return undefined;
	}
	
} else {
	console.error("[pdulvp-require] module can't be used on NodeJs");
}
