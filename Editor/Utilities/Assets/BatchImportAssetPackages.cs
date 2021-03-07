using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using System.IO;

public class BatchImportAssetPackages : ScriptableWizard
{
    public string packagePath = "";

    [MenuItem("Assets/Batch Import")]
    static void CreateWizard()
    {
        var wizard = ScriptableWizard.DisplayWizard("Batch Import Packages", typeof(BatchImportAssetPackages));
        wizard.createButtonName = "Import";
        wizard.helpString = "Allows you to batch import .unitypackages from local disk";
    }

    void OnWizardCreate()
    {
        packagePath = packagePath.Replace("\\", "/") + "/";

        string[] allFilePaths = Directory.GetFiles(Path.GetDirectoryName(packagePath));

        try
        {
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