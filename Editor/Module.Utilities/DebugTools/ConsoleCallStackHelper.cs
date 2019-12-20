// MIT License
// 
// Copyright (c) 2018 Sabresaurus
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{

    public class ConsoleCallStackHelper : EditorWindow
    {
        Vector2 scrollPosition = Vector2.zero;

        [MenuItem("/Tools/Debug/Call Stack", false, 0)]
        static void Init()
        {
            ConsoleCallStackHelper window = EditorWindow.GetWindow<ConsoleCallStackHelper>();
            window.Show();
            window.titleContent = new GUIContent("Call Stack");
        }

        private void OnGUI()
        {
            Color backgroundColor = GUI.backgroundColor;
            Type consoleWindowType = Type.GetType("UnityEditor.ConsoleWindow, UnityEditor", true);
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(consoleWindowType);
            if (windows.Length > 0)
            {
                // Fetch the active callstack from the console window
                FieldInfo fieldInfo = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
                string output = (string)fieldInfo.GetValue(windows[0]);

                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
                style.richText = true;

                style.normal.background = EditorGUIUtility.whiteTexture;

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                GUI.backgroundColor = new Color(1, 1, 1, 0.25f);

                // Split the callstack into lines
                string[] lines = output.Split('\n');
                foreach (string line in lines)
                {
                    string displayLine = line;
                    int firstIndex = line.LastIndexOf(" (at Assets/", StringComparison.InvariantCultureIgnoreCase);
                    // Wrap the line in bold tags
                    if (firstIndex != -1)
                    {
                        displayLine = displayLine.Insert(firstIndex + 5, "<b>");
                        displayLine = displayLine.Insert(displayLine.Length - 1, "</b>");
                    }
                    // Click the line if valid
                    if (GUILayout.Button(displayLine, style))
                    {
                        if (firstIndex != -1)
                        {
                            // Valid target, jump to the line in the file
                            string trimmed = line.Substring(firstIndex + 5, line.Length - firstIndex - 6);
                            string[] splitLine = trimmed.Split(':');
                            AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(splitLine[0]), int.Parse(splitLine[1]));
                        }
                    }
                    // Show a rollover cursor on valid targets
                    if (firstIndex != -1)
                    {
                        Rect rect = GUILayoutUtility.GetLastRect();
                        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                    }
                }
                GUI.backgroundColor = backgroundColor;

                EditorGUILayout.EndScrollView();
            }
        }

        void OnInspectorUpdate()
        {
            // Repaint 10 times a second in case they clicked a new log entry
            Repaint();
        }
    }
}