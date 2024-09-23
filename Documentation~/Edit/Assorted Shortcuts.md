# Assorted Shortcuts

## What it does

There are a number of other assorted shortcuts provided as a part of this package.

The list of shortcuts are:

### Key Pad scene view tools

**Key pad 7** - Scene view look straight down

**Key pad 1** - Scene view look straight right

**Key pad 3** - Scene view look straight forward (-z)

**Key pad 5** - Scene view Reset

**Key pad 0** - align Scene view with Main Camera in the scene



## Edit / Find in Project

*Note: This is somewhat deprecated as Unity has added some much better search tools in recent versions.* 

This will look through all game objects in the project / scene and find any references to the selected game object.

## Group Tools

### Create Group

The default is CTRL + G - this will great an empty parent game object and add the selected objects as children.

*Note: one limitation is that all of the items to be grouped must be the same 'level' in the hierarchy. For example if you have 4 objects, but 2 are under one parent and 2 are under another parent, then the grouping will not work.  If you are trying to do this, simply drag all of the parent game objects into the same position in the hierarchy and THEN group them.*

### Center Group on Children

This attempts to center a parent game object based on the transform positions & bounding areas of it's children.

## Gizmos

### Enable all Gizmos

Turns on all Gizmos (the same as clicking the 'show gizmos button' in the scene view)

### Disable all Gizmos

Turns off all Gizmos (the same as clicking the 'hide gizmos button' in the scene view)
