com.pixelwizards.utilities
=========================

[![openupm](https://img.shields.io/npm/v/com.pixelwizards.utilities?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.pixelwizards.utilities/)

Some useful Utility tools for Unity.

Note: this repo uses Git LFS 

Usage
--------------

### Install via OpenUPM

The package is available on the [openupm registry](https://openupm.com). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.pixelwizards.utilities
```

### Install via git url

Add this to your project manifest.json

```
"com.pixelwizards.utilities": "https://github.com/PixelWizards/com.pixelwizards.utilities.git",
```

OpenUPM Support
----------------

This package is also available via the OpenUPM scoped registry: 
https://openupm.com/packages/com.pixelwizards.utilities/

Prerequistes
---------------
* This has been tested for `>= 2018.3`

Content
----------------

### Tools

* Timeline/Multi-Scene Swap track - added a new Timeline track to drive multi-scene config loading. Also see Multi-Scene Swap Helper
* Assets/Create/Scene Management/Multi-Scene Loader - multi scene loading system (see Samples for Runtime API usage as well!)
* Assets/Texture Combiner - lets you pack texture channels (combine multiple maps for HDRP textures etc)
* Assets/Find all References - find all references of a given object
* Window/Analysis/ResourceChecker - shows resources loaded in a scene, very useful for optimizing builds
* Edit/Distribute/Along X / Y / Z - distributes selected game objects in the scene
* Edit/Physics Settler - allows you to activate physics in edit mode to 'drop' / settle objects dynamically
* various other tools - 'Create GameObject at Root' etc
* Edit/Find in Project
* Edit/Group - create groups from gameobjects
* Edit/Reset Parent Transform - if you have groups you want to reset the parent transform position 
* Edit/Global Defines wizard - manage your .rsp files
* Edit/Enable / Disable Gizmos
* Edit/Replace Materials in object
* Edit/Replace Selection - bulk replace objects in a scene

### Samples

* None currently

Required dependencies
---------------
* None 
