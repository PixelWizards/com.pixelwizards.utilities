# Text to Text Mesh Pro (Converter)

## What it does

Many moons ago, Unity added a new way to render text in Unity games (TextMeshPro).

Unfortunately you can still encounter 3rd party packages / utils (for example, from the Asset Store) that use the legacy 'Text' component instead of it's replacement (TextMeshProUGUI). 

This utility will let you convert from the legacy Text components to the new TextMeshProUGUI components wherever they are found in a scene.

## How do you use this

Select any game objects in a scene and then go to the menu ***Edit -> Text to Text Mesh Pro*** 

This will convert any Text components in the selection (and children) into the newer Text Mesh Pro UGUI components, copying any properties (font size / color etc) to the new component.

## Known Issues

There are a variety of UI components that include a Text component, but also have a newer TextMeshPro variant. For example, Dropdown or Input Fields. 

This utility does NOT convert these from the legacy version into the TextMesh Pro version, and WILL convert any child game objects with the legacy Text component into the newer format (which in turn will break the legacy UI element as a result)

**TLDR:** If you have any legacy Dropdown or Input Field elements in your UI, you should convert these manually into the newer Text Mesh Pro equivalents.



