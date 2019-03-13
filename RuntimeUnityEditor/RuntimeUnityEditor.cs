using System.Reflection;
using System.Xml;
using System.ComponentModel;
using RuntimeUnityEditor.ObjectTree;
using RuntimeUnityEditor.REPL;
using UnityEngine;
using UnityModManagerNet;
using Harmony12;

namespace RuntimeUnityEditor
{
    static class Main
    {
        public static Inspector.Inspector Inspector { get; private set; }
        public static ObjectTreeViewer TreeViewer { get; private set; }
        public static ReplWindow Repl { get; private set; }

        public static bool init = false;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            //Patches
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnUpdate = OnUpdate;

            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (!init && Input.GetKeyDown(KeyCode.F12))
            {
                init = true;
                var main = new GameObject();

                main.AddComponent<RuntimeUnityEditor>();

                RuntimeUnityEditor.Instance.Show = true;
            }
        }
    }

    public class RuntimeUnityEditor : MonoBehaviour
    {
        public Inspector.Inspector Inspector { get; private set; }
        public ObjectTreeViewer TreeViewer { get; private set; }
        public ReplWindow Repl { get; private set; }

        internal static RuntimeUnityEditor Instance { get; private set; }

        protected void Awake()
        {
            new XmlDocument().CreateComment("Test if System.XML is available (REPL fails with no message without it)");

            Instance = this;

            Inspector = new Inspector.Inspector(targetTransform => TreeViewer.SelectAndShowObject(targetTransform));
            TreeViewer = new ObjectTreeViewer(items =>
            {
                Inspector.InspectorClear();
                foreach (var stackEntry in items)
                    Inspector.InspectorPush(stackEntry);
            });

            Repl = new ReplWindow();
        }

        void OnGUI()
        {
            if (Show)
            {
                Inspector.DisplayInspector();
                TreeViewer.DisplayViewer();
                Repl.DisplayWindow();
            }
        }

        [Browsable(false)]
        public bool Show
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

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                Show = !Show;
            }

            Inspector.InspectorUpdate();
        }

        private void SetWindowSizes()
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
}
