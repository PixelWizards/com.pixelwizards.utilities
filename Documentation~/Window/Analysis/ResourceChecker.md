# Resource Checker

## What it does

The resource checker is a custom editor window that shows you a detailed breakdown of all of the textures, materials and meshes in any currently loaded scenes.

It can be used in **edit mode** or at **runtime** to review any scenes are loaded.

## How do you use this

Open ***Window -> Analysis -> Resource Checker***

The Resource Checker window will open

![](../../Images/ResourceChecker.png)

Navigate to any of the tabs (Textures / Materials / Meshes) to view what is loaded in the current scene(s).

You can click on any of the elements in the list to navigate to the object in the Project window.

Each of the entries in the list also indicate dependencies, so you can see if a material is used by more than one objects etc.

This is a very useful window for optimization and profiling your scene(s).

