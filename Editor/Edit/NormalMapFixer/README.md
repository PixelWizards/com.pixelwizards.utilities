# UnityNormalMapInverter
Convert normal maps from DirectX (-y) to OpenGL (+y)

"Sky in your Eye? Flip your Y" - doctorpangloss

This is a simple tool for converting normal maps from DirectX style (-y) to OpenGL (+y). why are there 2 standards that contain the same information but have slightly different configuration? beats me. But ive seen too many people use the wrong type in the wrong engine, so I made this in-editor tool to fix it, which is much easier to use than opening gimp, decomposing the colors into the RGB components, inverting the G channel, recomposing, and overwriting. 

A render engine will use either DirectX or OpenGL style normals. Some common examples include:

 OpenGL:
  - Unity
  - Blender
  - Houdini
  - Maya
  - Zbrush
  - IClone

 DirectX:
  - UE4/5
  - Godot
  - CryEngine
  - Source Engine
  - Substance Designer/painter

How do I know if my normal maps are fucked?
Cause they look like this:
![FuckedOrNot](https://user-images.githubusercontent.com/59656122/162627338-a93b8efc-a28a-4a94-907a-1ec95cbeb385.png)



HOW TO USE:
1) Open the NormalFixer Tool (Tools/NormalMapCorrecter)

![Screenshot_1](https://user-images.githubusercontent.com/59656122/162627605-31853625-b927-40e6-8de8-0a49481c41dd.png)

2) Drag the normal map into the only slot on the tool. Then press Invert Normal Map. (it might take a while to convert the map, depending on the size)

![Screenshot_2](https://user-images.githubusercontent.com/59656122/162627615-c6bf833f-543f-44cb-b52a-ffe1c36e546b.png)

3) Set the desired file destination + name. Click “save”

![Screenshot_10](https://user-images.githubusercontent.com/59656122/162627620-d5ee8fa5-20a9-4df7-8a99-3a132cc5fab7.png)
