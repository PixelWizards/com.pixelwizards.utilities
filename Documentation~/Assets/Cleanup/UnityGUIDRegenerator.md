# Unity GUID Regenerator

## What it does

Unity relies heavily on GUIDs to represent files and folders in your Unity project.

There are situations when you might have content / packages that you are loading into a project with conflicting GUID files (for example, multiple packages from a single Asset Store vendor).

This utility was created to help work around these issues by allowing you to regenerate the GUID files in your project.

Note: this is an ALL OR NOTHING action. You can NOT generate GUIDS for only a single folder (for example) or file.

## TLDR: You probably don't need to do this

If you don't know what the above means, then you don't need to do this.

## How do you use this

Simply go to ***Assets -> Cleanup -> Regenerate GUIDs***. This will show you the following prompt:

![](../../Images/GUIDRegeneration.png)

READ THE MESSAGE and then click 'Regenerate GUIDs' to continue.

