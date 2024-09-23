# Find (and Remove) Missing Scripts

## What it does

If you have ever worked on a large(ish) project, you have likely encountered a situation where you have a number of objects in your scene with missing / broken scripts.

## How do you use this

Select any number of game objects in a scene (for example the parent game object in a hierarchy of objects), OR a group of prefabs in the Project window, and then open ***Assets -> Cleanup -> Find and Remove Missing Scripts***

This will open the following window:

![](../../Images/FindMissingScripts.png)

Simply click 'Find Missing Scripts' and it will recurse through all of the objects and remove any missing scripts that exist.

## Roadmap / Todo:

Add a 'confirm' option so it doesn't just auto remove all of the scripts. 

Note: this will 'just' remove the scripts from any prefabs in the scene, but you may still need to apply the changes to the prefab(s).



