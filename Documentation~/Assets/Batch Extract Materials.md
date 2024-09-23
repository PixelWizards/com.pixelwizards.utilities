# Batch Extract Materials

## What it does

When working with large groups of objects, it is very common to want to extract all of the materials for the objects. Unfortunately Unity does not support batch editing of a selection of objects and, by default, requires that you manually select and manage the materials one by one.

![](../Images/materialmultiedit.png)

This is very time consuming and painful when working with dozens of objects, which is why the Batch Extract Materials wizard was created.

## How do you use this

Navigate to **Assets -> Batch Extract Materials**

This will open this window

![](../Images/BatchExtractWindow.png)

Simply drag and drop source meshes (not prefabs) onto the 'Models to Process' area (you can drag multiple objects) to add them to the list. This will populate the list, like so:

![](../Images/MultipleObjectsBatchMaterial.png)

## Material Remap Options

There are a number of options that can be used while remapping the materials.

### Material Names must match

This is used in the validation process. In order to re-use materials between different meshes, this ensures that the material names match. For example "Material A", will not match "Material B" from another object, but if both objects have "Material A", then they match and both objects will use the same common material.

### Material properties must match

This is used in the validation process. In order to re-use materials between different meshes, this ensures that the material properties match. For example if ObjectA has a material with base texture "TextureA", it will not match another object with a base texture "TextureB", but if both objects have have a material with base texture "TextureA", then they match and both objects will use the same common material. 

### Don't remap already extracted materials

If you have extracted materials from an object previously, this will set the remapping to ignore these materials.

### Don't map Model A's materials to Model B

This checkbox basically ensures that all objects will have unique materials, even if the above rules match. 

## Verify the Remap

Once you click 'next' on the dialog above, the system will iterate through all of the objects in the list and provide a list of all of teh materials and show you what the system has determined the best option for each material will be.

For example:

![](../Images/VerifyExtractMaterials.png)

The top buttons let you alternately specify whether you want to 'extract' all of the materials (make unique), 'remap' them all (try to create a common material set that is reused) OR ignore all, which effectively does nothing.

As you can see from the image above, you can also modify individual materials in the list if you so choose (so if the system wants to 'remap' a material you could choose to 'extract' it to a unique material for example).

Once you are happy with the intended results, simply click 'Extract!' and the system will run the operation, or hit 'Back' if you want to change settings on a prior screen.
