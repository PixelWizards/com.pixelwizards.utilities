#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MegaCrush.AmbientAI.EditorTools
{
    public static class AnimatorFullDump
    {
        [MenuItem("Assets/Animation/Dump Animator (Full)")]
        public static void DumpSelectedAnimator()
        {
            var controller = Selection.activeObject as AnimatorController;
            if (!controller)
            {
                Debug.LogWarning("Select an AnimatorController asset.");
                return;
            }

            var sb = new StringBuilder(4096);

            sb.AppendLine($"Animator Controller: {controller.name}");
            sb.AppendLine("========================================");

            for (int l = 0; l < controller.layers.Length; l++)
            {
                var layer = controller.layers[l];
                sb.AppendLine();
                sb.AppendLine($"Layer [{l}]: {layer.name}");
                sb.AppendLine("----------------------------------------");

                DumpStateMachine(layer.stateMachine, sb, layer.name, "  ");
            }

            Debug.Log(sb.ToString());
        }

        private static void DumpStateMachine(
            AnimatorStateMachine sm,
            StringBuilder sb,
            string path,
            string indent)
        {
            // States
            foreach (var child in sm.states)
            {
                var state = child.state;
                sb.AppendLine($"{indent}- State: {path}/{state.name}");

                if (state.motion == null)
                {
                    sb.AppendLine($"{indent}    (no motion)");
                }
                else
                {
                    DumpMotion(state.motion, sb, indent + "    ");
                }
            }

            // Sub-state machines
            foreach (var child in sm.stateMachines)
            {
                var sub = child.stateMachine;
                sb.AppendLine($"{indent}> SubStateMachine: {path}/{sub.name}");
                DumpStateMachine(sub, sb, $"{path}/{sub.name}", indent + "  ");
            }
        }

        private static void DumpMotion(Motion motion, StringBuilder sb, string indent)
        {
            if (motion is AnimationClip clip)
            {
                sb.AppendLine($"{indent}- Clip: {clip.name}");
                return;
            }

            if (motion is BlendTree tree)
            {
                sb.AppendLine($"{indent}- BlendTree: {tree.name}");
                sb.AppendLine($"{indent}  BlendParam: {tree.blendParameter}");

                foreach (var child in tree.children)
                {
                    if (child.motion == null) continue;
                    DumpMotion(child.motion, sb, indent + "  ");
                }
                return;
            }

            sb.AppendLine($"{indent}- Motion: {motion.name} (unknown type)");
        }
    }
}
#endif
