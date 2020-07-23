# Multi-Scene Swap Helper

This document attempts to describe the Multi-Scene Swap Helper, it's purpose and how to use it. 

At it's core, the Multi-Scene Swap Helper has 2 elements:

1. Multi-Scene Loader Config
2. Multi-Scene Helper

All 2 are required and interconnected. 

The multi-scene Loader defines a set of 'Scene Configs' that can be loaded as needed.  Each config may contain as many individual scenes as may be needed.  More information about the [scene configs]() and how the system works, check out it's corresponding page in the documentation. 

The ***Multi-Scene Swap Helper*** is a component that you can add to any GameObject in your scene.

It provides 2 key pieces of functionality:

1. It serves as the 'glue' between the Multi-Scene Timeline Track and the Multi-Scene Config, and
2. It can be used as an 'on demand' loading system to trigger loading of other scenes when the scene that the Helper is in is loaded. 

**Usage (On-Demand Loading):**

1. Create a [Multi-Scene Config](multisceneloader.md).
2. Create a ***Multi-Scene Swap Helper*** - this is a component that you add to a game object in a particular scene
3. Configure the ***Multi-Scene Swap Helper*** 
   1. link the Multi-Scene Config you created in step 1 into the 'Multi-Scene Config' field
   2. Check 'Load Config on Awake' to trigger on-demand loading
   3. In the 'Config List' add the names of any configs that you wish to load when this scene is loaded. 

That's it - whenever you load this scene now (in Edit or Play mode) - the scenes defined in the Config List will be loaded as well.


