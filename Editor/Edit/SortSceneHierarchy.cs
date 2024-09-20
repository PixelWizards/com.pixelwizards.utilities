using UnityEngine;
using UnityEditor;

namespace PixelWizards.Utilities
{
    /// <summary>
    /// Simple script to order a single level of your scene hierarchy alphabetically. Right Click on a GameObject in your scene and 'Sort by Name'
    /// 
    /// Note: you MUST not do this with UI elements because sorting == draw order and this will fuck it up.
    /// 
    /// Source: https://answers.unity.com/questions/717398/hierarchy-is-not-in-order.html?_ga=2.161714364.1693018819.1602479154-717108197.1594613165
    /// </summary>
    public class SortChildObjects : EditorWindow
    {

        [MenuItem("GameObject/Sort By Name", false, -1)]
        public static void SortGameObjectsByName(MenuCommand menuCommand)
        {
            if (menuCommand.context == null || menuCommand.context.GetType() != typeof(GameObject))
            {
                EditorUtility.DisplayDialog("Error", "You must select an item to sort in the frame", "Okay");
                return;
            }

            var parentObject = (GameObject)menuCommand.context;

            if (parentObject.GetComponentInChildren<RectTransform>())
            {
                EditorUtility.DisplayDialog("Error", "You are trying to sort a GUI element. This will screw up EVERYTHING, do not do", "Okay");
                return;
            }

            // Build a list of all the Transforms in this player's hierarchy
            var objectTransforms = new Transform[parentObject.transform.childCount];
            for (var i = 0; i < objectTransforms.Length; i++)
            {
                objectTransforms[i] = parentObject.transform.GetChild(i);
            }

            var sortTime = System.Environment.TickCount;

            var sorted = false;
            // Perform a bubble sort on the objects
            while (sorted == false)
            {
                sorted = true;
                for (var i = 0; i < objectTransforms.Length - 1; i++)
                {
                    // Compare the two strings to see which is sooner
                    var comparison = objectTransforms[i].name.CompareTo(objectTransforms[i + 1].name);

                    if (comparison <= 0)
                    {
                        continue; // 1 means that the current value is larger than the last value
                    }
                    
                    objectTransforms[i].transform.SetSiblingIndex(objectTransforms[i + 1].GetSiblingIndex());
                    sorted = false;
                }

                // resort the list to get the new layout
                for (var i = 0; i < objectTransforms.Length; i++)
                {
                    objectTransforms[i] = parentObject.transform.GetChild(i);
                }
            }

            Debug.Log("Sort took " + (System.Environment.TickCount - sortTime) + " milliseconds");

        }
    }
}