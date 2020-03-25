using UnityEditor;
using UnityEngine;

namespace MWU.Layout
{
	public static class LayoutLoader
	{
		static string layoutPath = "Packages/com.mwu.filmlib/Editor/Module.Layout/LayoutConfigs/FilmLayout.wlt";

		[MenuItem("Tools/Load Film Layout", false, -1)]
		public static void LoadFilmLayout()
		{
			if (System.IO.File.Exists(layoutPath))
			{
				EditorUtility.LoadWindowLayout(layoutPath);
				Debug.Log("Layout Successfully Loaded");
			}
			else
			{
				Debug.LogWarning("Layout not loaded. Layout file missing at: " + layoutPath);
			}
		}


		//Used for creating new film layouts. Hidden by default to prevent overriding built in
		//template layout. Use at own risk.

		/*
		[MenuItem("Tools/Save Film Layout")]
		public static void SaveFilmLayout()
		{
			// Saving the current layout to an asset
			LayoutUtility.SaveLayoutToAsset(layoutPath);
		}
		*/
	}
}