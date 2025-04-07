using System;
using UnityEditor;
using UnityEngine;

namespace BuildingToolUtils
{
    public static class BuildingToolUtility
    {
        static Material ghostMaterial;

        // Material Preview Functions
        #region Material 
        public static void InitGhostMaterial()
        {
            if (ghostMaterial == null)
            {
                ghostMaterial = new Material(Shader.Find("BuildingTool/AlwaysVisible"));
                ghostMaterial.color = new Color(1, 0, 0, 0.4f); // Red
            }
        }

        public static void SetGhostColor(Color color)
        {
            if (ghostMaterial != null)
            {
                ghostMaterial.color = new(color.r, color.g, color.b, 0.4f); // Set Alpha Here
            }
        }

        public static Material GetGhostMaterial() => ghostMaterial;
        #endregion

        #region Editor Draw Helper Functions

        public static bool DrawButton_Discard()
        {
            if (GUILayout.Button("Discard Building", EditorStyles.miniButtonRight))
            {
                return EditorUtility.DisplayDialog(
                    "WARNING",
                    "This will delete the current building for good, are you sure?",
                    "Yes",
                    "No");
            }
            return false;
        }

        public static bool DrawButton_OptimizeColliders(GameObject _floorParent, GameObject _wallParent, GameObject _roofParent)
        {
            if (GUILayout.Button("Optimize Colliders", EditorStyles.miniButtonMid, GUILayout.Width(500)))
            {
                return EditorUtility.DisplayDialog(
                    "WARNING",
                    "This action cannot be undone and is recommended only after the building phase is complete. Do you want to proceed?",
                    "Yes",
                    "no");
            }
            return false;
        }

        //TODO: BUTTON TO REMOVE ALL COLLIDERS
        //TODO: BUTTON TO SAVE THE PREFAB


        public static void DrawCentered(Action drawFunc)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            drawFunc.Invoke();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        public static void DrawSeparationLine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
        #endregion

        #region Logic Helper Functions
        public static void RefocusCameraOnTarget(GameObject preview)
        {
            SceneView.lastActiveSceneView.LookAt(preview.transform.position);
        }

        public static void ForceSceneFocus()
        {
            if (SceneView.sceneViews.Count > 0)
            {
                SceneView sceneView = (SceneView)SceneView.sceneViews[0];
                sceneView.Focus();
            }
        }


        public static Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        public static Bounds Uniform(this Bounds bounds)
        {
            if (Mathf.Approximately(bounds.extents.x, bounds.extents.z)) return bounds;

            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            Vector3 newSize = new Vector3(maxExtent * 2f, bounds.size.y, maxExtent * 2f); // size, non extents!
           
            return new Bounds(bounds.center, newSize);
        }


        public static Vector3 BottomCenter(this Bounds bounds)
        {
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        public static Vector3 TopCenter(this Bounds bounds)
        {
            return new Vector3(bounds.center.x, bounds.max.y, bounds.max.z);
        }




        #endregion

        #region Gizmos

        public static void DrawSolidSphere(Vector3 position, float radius)
        {
            Mesh sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * radius * 2);
            Graphics.DrawMeshNow(sphere, matrix);
        }

        #endregion
    }
    enum ToolState { NoBuild, BuildInitiated };

    enum ModuleType
    {
        FLOOR,
        WALL,
        ROOF,
        PROPS
    }

    struct Module
    {
        public GameObject prefab;
        public ModuleType moduleType;

        public Module(GameObject _prefab, ModuleType _moduleType)
        {
            prefab = _prefab;
            moduleType = _moduleType;
        }

        public bool IsNull() => prefab == null;

        public void Clear() => prefab = null;

    }


}
