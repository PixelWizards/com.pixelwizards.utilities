# Multi-Scene Timeline Track

This document attempts to describe the new Multi-Scene Timeline track, it's purpose and how to use it. 

At it's core, the Multi-Scene Timeline track has 3 elements:

1. Multi-Scene Loader Config
2. Multi-Scene Helper
3. Multi-Scene Timeline Track

All 3 are required and interconnected. 

The multi-scene Loader defines a set of 'Scene Configs' that can be loaded as needed.  Each config may contain as many individual scenes as may be needed.  More information about the [scene configs]() and how the system works, check out it's corresponding page in the documentation. 

The corresponding timeline track allows you to control scene loading dynamically. 

**Usage:**

1. Create a Multi-Scene Swap Helper - this is a component that you add to a game object in the scene that contains the Timeline. 
2. Configure the Multi-Scene Swap Helper - the main thing you need to do is point it at the Multi-Scene config that you wish to use.
3. Create a Multi-Scene Timeline Track. Drag the Multi-Scene Swap Helper into the track binding to tell Timeline which Multi-Scene Swap Helper that this timeline is controlling.
4. Create any number of Multi-Scene Clips on the track. For each clip, you can customize which configs that you wish to load' and 'configs to unload'.

That's it - as you scrub or play your timeline, the corresponding scenes that are referenced in the Multi-Scene Swap Helper will be loaded and unloaded as defined in the Timeline Clips.


