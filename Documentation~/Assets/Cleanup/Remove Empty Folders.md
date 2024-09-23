# Remove Empty Folders

## What it does

Remove Empty Folders enables or disables a task that runs in the background to delete any empty folders in your project.

This can be useful if you delete a folder outside of Unity but potentially have not deleted the folder 'inside' Unity. This is a common situation if you are using Perforce for example.

## How do you use this

Simply select the menu option ***'Assets -> Cleanup -> Remove Empty Folders***' to enable this behaviour.

This will toggle the menu option from unchecked:

![](../../Images/RemoveEmptyFoldersDefault.png)

To checked:

![](../../Images/RemoveEmptyFoldersChecked.png)

Once enabled, this will monitor Asset Database changes and remove any Empty folders that it finds.

*Note: this might cause frustration if enabled since it will delete any folders that is created manually as well (so you literally can't create folders if enabled).*

