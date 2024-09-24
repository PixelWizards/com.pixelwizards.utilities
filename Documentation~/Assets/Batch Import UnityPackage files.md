# Batch Import .unitypackage files

## What it does

If you are like me, you buy a TON of stuff on the asset store. Even more so, when you find a vendor that you LIKE, you buy basically everything that they have. (don't judge me).

However, when you are trying to load assets into a project, it is a very slow process to go through each package one by one and import them into the project, clicking through all of the dialogs etc.

What if you could just point Unity at a folder with the .unitypackages in it and import them all at once?

## How do you use this

Navigate to **Assets -> Batch Import .unitypackage files**

The following dialog opens:

![](../Images/BatchImportPackages.png)

Simply copy & paste the path that you want to import your packages from into the 'Package Path' field, choose whether you want to include all subdirectories and then click 'Import'

For example:

```C:\Users\Mike\AppData\Roaming\Unity\Asset Store-5.x\My Favorite Vendor\```

I would highly suggest that you NOT try to import your entire Asset Store-5.x cache folder at once. This has a good chance of breaking Unity and will take a lot of disk space at best.





