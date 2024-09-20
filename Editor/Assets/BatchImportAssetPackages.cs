using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BatchImportAssetPackages : ScriptableWizard
{
    public string packagePath = "";
    public bool includeSubdirectories = false;

    [MenuItem("Assets/Batch Import")]
    static void CreateWizard()
    {
        var wizard = ScriptableWizard.DisplayWizard("Batch Import Packages", typeof(BatchImportAssetPackages));
        wizard.createButtonName = "Import";
        wizard.helpString = "Allows you to batch import .unitypackages from the specified folder, optionally including sub-folders";
    }

    void OnWizardCreate()
    {
        packagePath = packagePath.Replace("\\", "/") + "/";
        List<string> allFilePaths = new List<string>();

        if ( includeSubdirectories)
        {
            allFilePaths = Directory.GetFiles(Path.GetDirectoryName(packagePath), "*.unitypackage", SearchOption.AllDirectories).ToList();
        }
        else
        {
            allFilePaths = Directory.GetFiles(Path.GetDirectoryName(packagePath), "*.unitypackage", SearchOption.TopDirectoryOnly).ToList();
        }
        

        try
        {
            Debug.Log("Batch Importing Packages: Found " + allFilePaths.Count + " packages...");
            foreach (string curPath in allFilePaths)
            {
                string fileToImport = curPath.Replace("\\", "/");
                if (Path.GetExtension(fileToImport).ToLower() == ".unitypackage")
                {
                    Debug.Log("Importing: " + fileToImport);
                    AssetDatabase.ImportPackage(fileToImport, false);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log("Error: " + ex.Message);
        }
    }
}