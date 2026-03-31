using System;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace PixelWizards.Utilities
{
    internal static class PhysicsSettler
    {
        private const string MenuPath = "Tools/Physics/Settle Physics";
        private const string ShortcutPath = "Tools/Physics/Settle Physics";
        private const float MaxSettleTime = 5f;
        private const float SimulationStep = 1f / 60f;
        private const float OverlayWidth = 220f;

        private static bool s_IsActive;
        private static double s_StartTime;
        private static double s_LastStepTime;

        private static Rigidbody[] s_WorkList = Array.Empty<Rigidbody>();
        private static SimulationMode s_PreviousSimulationMode;

        [MenuItem(MenuPath)]
        private static void StartSettle()
        {
            if (s_IsActive)
            {
                return;
            }

            s_WorkList = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

            if (s_WorkList.Length == 0)
            {
                Debug.Log("Physics Settler: No Rigidbody components found in loaded scenes.");
                return;
            }

            s_PreviousSimulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            for (int i = 0; i < s_WorkList.Length; i++)
            {
                if (s_WorkList[i] != null)
                {
                    s_WorkList[i].WakeUp();
                }
            }

            s_IsActive = true;
            s_StartTime = EditorApplication.timeSinceStartup;
            s_LastStepTime = s_StartTime;

            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGUI;

            Menu.SetChecked(MenuPath, true);

            Debug.Log($"Physics Settler: Started settling {s_WorkList.Length} rigidbodies.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateStartSettle()
        {
            return !s_IsActive;
        }

        [MenuItem("Tools/Physics/Stop Settling", true)]
        private static bool ValidateStopSettle()
        {
            return s_IsActive;
        }

        [MenuItem("Tools/Physics/Stop Settling")]
        private static void StopSettleMenu()
        {
            StopSettling("Stopped manually.");
        }

        [Shortcut(ShortcutPath, KeyCode.Q, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        private static void StartSettleShortcut()
        {
            if (!s_IsActive)
            {
                StartSettle();
            }
        }

        private static void OnEditorUpdate()
        {
            if (!s_IsActive)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float elapsed = (float)(now - s_StartTime);

            if (elapsed >= MaxSettleTime)
            {
                StopSettling("Reached maximum settle time.");
                return;
            }

            if (AllBodiesSleeping())
            {
                StopSettling("All rigidbodies are sleeping.");
                return;
            }

            // Simulate in fixed-size chunks so editor framerate does not affect settling behavior too much.
            while ((now - s_LastStepTime) >= SimulationStep)
            {
                Physics.Simulate(SimulationStep);
                s_LastStepTime += SimulationStep;

                if (AllBodiesSleeping())
                {
                    StopSettling("All rigidbodies are sleeping.");
                    return;
                }

                elapsed = (float)(EditorApplication.timeSinceStartup - s_StartTime);
                if (elapsed >= MaxSettleTime)
                {
                    StopSettling("Reached maximum settle time.");
                    return;
                }
            }

            SceneView.RepaintAll();
        }

        private static bool AllBodiesSleeping()
        {
            for (int i = 0; i < s_WorkList.Length; i++)
            {
                Rigidbody body = s_WorkList[i];
                if (body == null)
                {
                    continue;
                }

                if (!body.IsSleeping())
                {
                    return false;
                }
            }

            return true;
        }

        private static void StopSettling(string reason)
        {
            if (!s_IsActive)
            {
                return;
            }

            Physics.simulationMode = s_PreviousSimulationMode;

            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;

            s_IsActive = false;
            s_WorkList = Array.Empty<Rigidbody>();

            Menu.SetChecked(MenuPath, false);

            Debug.Log($"Physics Settler: {reason}");
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!s_IsActive)
            {
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - s_StartTime);
            float remaining = Mathf.Max(0f, MaxSettleTime - elapsed);

            Handles.BeginGUI();

            Rect rect = new Rect(12f, 12f, OverlayWidth, 48f);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("Settling Physics");
            GUILayout.Label($"Time Remaining: {remaining:F2}s");
            GUILayout.EndArea();

            Handles.EndGUI();

            Color previousColor = Handles.color;
            Handles.color = Color.green;

            for (int i = 0; i < s_WorkList.Length; i++)
            {
                Rigidbody body = s_WorkList[i];
                if (body == null || body.IsSleeping())
                {
                    continue;
                }

                Handles.Label(body.worldCenterOfMass, "SIMULATING");
            }

            Handles.color = previousColor;
        }
    }
}