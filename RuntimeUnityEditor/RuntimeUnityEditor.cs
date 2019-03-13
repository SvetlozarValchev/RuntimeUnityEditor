using System.Reflection;
using System.ComponentModel;
using RuntimeUnityEditor.ObjectTree;
using RuntimeUnityEditor.REPL;
using UnityEngine;
using UnityModManagerNet;
using Harmony12;

namespace RuntimeUnityEditor
{
    static class RuntimeUnityEditor
    {
        public static Inspector.Inspector Inspector { get; private set; }
        public static ObjectTreeViewer TreeViewer { get; private set; }
        public static ReplWindow Repl { get; private set; }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            Inspector = new Inspector.Inspector(targetTransform => TreeViewer.SelectAndShowObject(targetTransform));
            TreeViewer = new ObjectTreeViewer(items =>
            {
                Inspector.InspectorClear();
                foreach (var stackEntry in items)
                    Inspector.InspectorPush(stackEntry);
            });

            Repl = new ReplWindow();
            
            modEntry.OnUpdate = OnUpdate;

            //Patches
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }

        [Browsable(false)]
        public static bool Show
        {
            get => TreeViewer.Enabled;
            set
            {
                TreeViewer.Enabled = value;

                if (value)
                {
                    SetWindowSizes();

                    TreeViewer.UpdateCaches();
                }
            }
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                Show = !Show;
            }

            Inspector.InspectorUpdate();
        }

        static void SetWindowSizes()
        {
            const int screenOffset = 10;

            var screenRect = new Rect(
                screenOffset,
                screenOffset,
                Screen.width - screenOffset * 2,
                Screen.height - screenOffset * 2);

            var centerWidth = (int)Mathf.Min(850, screenRect.width);
            var centerX = (int)(screenRect.xMin + screenRect.width / 2 - Mathf.RoundToInt((float)centerWidth / 2));

            var inspectorHeight = (int)(screenRect.height / 4) * 3;
            Inspector.UpdateWindowSize(new Rect(
                centerX,
                screenRect.yMin,
                centerWidth,
                inspectorHeight));

            var rightWidth = 350;
            var treeViewHeight = screenRect.height;
            TreeViewer.UpdateWindowSize(new Rect(
                screenRect.xMax - rightWidth,
                screenRect.yMin,
                rightWidth,
                treeViewHeight));

            var replPadding = 8;
            Repl.UpdateWindowSize(new Rect(
                centerX,
                screenRect.yMin + inspectorHeight + replPadding,
                centerWidth,
                screenRect.height - inspectorHeight - replPadding));
        }
    }

    [HarmonyPatch(typeof(UnityModManager.UI), "OnGUI")]
    class UnityModManagerUI_OnGUI_Patch
    {
        static void Postfix(UnityModManager.UI __instance)
        {
            if (RuntimeUnityEditor.Show)
            {
                RuntimeUnityEditor.Inspector.DisplayInspector();
                RuntimeUnityEditor.TreeViewer.DisplayViewer();
                RuntimeUnityEditor.Repl.DisplayWindow();
            }
        }
    }
}
