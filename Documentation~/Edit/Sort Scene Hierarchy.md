# Sort by Name (Scene Hierarchy)

## What it does

There are situations where you might want to re-order the objects in your scene hierarchy alphabetically.

Unity DOES provide a custom sort mode (alphabetical) in the Hierarchy window, however this will break any UI elements in the scene (since Unity's UI uses the scene hierarchy for Z-depth sorting of the UI and doing a reorder will break your UI). 

This utility is designed to allow you to Sort specific sub-elements of a scene hierarchy alphabetically as desired.

Do NOT use this on UI Canvas or children (and in fact it tries to avoid doing so)

## How do you use this

Right click on any Game Object in your scene and choose 'Sort by Name'

This will sort any children of that game object in the scene alphabetically.



