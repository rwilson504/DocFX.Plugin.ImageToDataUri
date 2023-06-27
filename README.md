# DocFX.Plugin.ImageToDataUri
A DocFx post processor that converts the src attribute of referenced images to their data-uri.

From this:  


To this:  


## Installation/Usage
1. Create a new folder called image-datauri in your DocFx template folder
1. Create a new folder called ``plugins`` within the new ``image-datauri`` folder.
1. Copy the compiled binaries into the ``plugins`` folder
1. Open the ``docfx.json`` file and add the new template folder.
	```
	"teamplate": [
	"default",
	"templates/image-datauri"
	],
	```
1. Add this post-processor plugin to the array
	```
	"postProcessors" : ["ImageToDataUriPostProcessor"]
	```
1. Run ``docfx build`` in the command line

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.