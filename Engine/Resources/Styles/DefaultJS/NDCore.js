﻿/*
   This file is part of Natural Docs, which is Copyright © 2003-2010 Greg Valure.
   Natural Docs is licensed under version 3 of the GNU Affero General Public
   License (AGPL).  Refer to License.txt for the complete details.

   This file may be distributed with documentation files generated by Natural Docs.
   Such documentation is not covered by Natural Docs' copyright and licensing,
   and may have its own copyright and distribution terms as decided by its author.

	(include in output) 
*/

/* Class: NDCore
	 _____________________________________________________________________________

    Various helper functions to be used throughout the other scripts.
*/
var NDCore = { };



	// Group: Class Functions
	// ____________________________________________________________________________


	/* Function: AddClass
		Adds a class to the passed HTML element.
	*/
	NDCore.AddClass = function (element, newClassName)
		{
		var index = element.className.indexOf(newClassName);

		if (index != -1)
			{
			if ( (index == 0 || element.className.charAt(index - 1) == ' ') &&
				 (index + newClassName.length == element.className.length ||
				  element.className.charAt(index + newClassName.length) == ' ') )
				{  return;  }
			}

		if (element.className.length == 0)
			{  element.className = newClassName;  }
		else
			{  element.className += " " + newClassName;  }
		};


	/* Function: RemoveClass
		Removes a class from the passed HTML element.
	*/
	NDCore.RemoveClass = function (element, targetClassName)
		{
		var index = element.className.indexOf(targetClassName);

		while (index != -1)
			{
			if ( (index == 0 || element.className.charAt(index - 1) == ' ') &&
				 (index + targetClassName.length == element.className.length ||
				  element.className.charAt(index + targetClassName.length) == ' ') )
				{
				var newClassName = "";

				// We'll leave surrounding spaces alone.
				if (index > 0)
					{  newClassName += element.className.substr(0, index);  }
				if (index + targetClassName.length != element.className.length)
					{  newClassName += element.className.substr(index + targetClassName.length);  }

				element.className = newClassName;
				return;
				};

			index = element.className.indexOf(targetClassName, index + 1);
			}
		};


	/* Function: AddBrowserClassesToBody
		If the current browser is IE6 or IE7, add thoses classes to HTML.body.  We're not doing a more generalized thing
		like Natural Docs 1.x did because it's not generally good practice and none of the other browsers should be broken 
		enough to need it anymore.
	*/
	NDCore.AddBrowserClassesToBody = function ()
		{
		if (navigator.userAgent.indexOf("MSIE 6") != -1)
			{  this.AddClass(document.body, "IE6");  }
		else if (navigator.userAgent.indexOf("MSIE 7") != -1)
			{  this.AddClass(document.body, "IE7");  }
		};



	// Group: Positioning Functions
	// ________________________________________________________________________


	/* Function: WindowClientWidth
		 A browser-agnostic way to get the window's client width.
	*/
	NDCore.WindowClientWidth = function ()
		{
		var width = window.innerWidth;

		// Internet Explorer
		if (width === undefined)
			{  width = document.documentElement.clientWidth;  }

		return width;
		};


	/* Function: WindowClientHeight
		 A browser-agnostic way to get the window's client height.
	*/
	NDCore.WindowClientHeight = function ()
		{
		var height = window.innerHeight;

		// Internet Explorer
		if (height === undefined)
			{  height = document.documentElement.clientHeight;  }

		return height;
		};


	/* Function: SetToAbsolutePosition
		Sets the element to the absolute position and size passed as measured in pixels.  This assumes the element is 
		positioned using fixed or absolute.  It accounts for all sizing weirdness so that the ending offsetWidth and offsetHeight
		will match what you passed regardless of any borders or padding.  If any of the coordinates are undefined it will be
		left alone.
	*/
	NDCore.SetToAbsolutePosition = function (element, x, y, width, height)
		{
		var pxRegex = /^([0-9]+)px$/i;

		if (x != undefined && element.offsetLeft != x)
			{  element.style.left = x + "px";  }
		if (y != undefined && element.offsetTop != y)
			{  element.style.top = y + "px";  }
			
		// We have to use the non-standard (though universally supported) offsetWidth instead of the W3C-approved scrollWidth.
		// In all browsers offsetWidth returns the full width of the element in pixels including the border.  In Firefox and Opera 
		// scrollWidth will do the same, but in IE and WebKit it's instead equivalent to clientWidth which doesn't include the border.
		if (width != undefined && element.offsetWidth != width)
			{
			// If the width isn't already specified in pixels, set it to pixels.  We can't figure out the difference between the style
			// and offset widths otherwise.  This might cause an extra resize, but only the first time.
			if (!pxRegex.test(element.style.width))
				{  
				element.style.width = width + "px";  

				if (element.offsetWidth != width)
					{
					var adjustment = width - element.offsetWidth;
					element.style.width = (width + adjustment) + "px";
					}
				}
			else
				{  
				var styleWidth = RegExp.$1;
				var adjustment = styleWidth - element.offsetWidth;
				element.style.width = (width + adjustment) + "px";
				}
			}

		// Copypasta for height
		if (height != undefined && element.offsetHeight != height)
			{
			if (!pxRegex.test(element.style.height))
				{  
				element.style.height = height + "px";  

				if (element.offsetHeight != height)
					{
					var adjustment = height - element.offsetHeight;
					element.style.height = (height + adjustment) + "px";
					}
				}
			else
				{  
				var styleHeight = RegExp.$1;
				var adjustment = styleHeight - element.offsetHeight;
				element.style.height = (height + adjustment) + "px";
				}
			}
		};