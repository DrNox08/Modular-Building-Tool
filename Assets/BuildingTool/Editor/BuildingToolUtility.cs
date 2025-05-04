using System;
using System.Linq;
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

        public static bool DrawButton_OptimizeColliders(GameObject _floorParent, GameObject _wallParent,
            GameObject _roofParent) // NOT IMPLEMENTED
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
        
        public static bool DrawButton_RemoveAllColliders(GameObject buildingRoot)
        {
            if (GUILayout.Button("Remove All Colliders"))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Remove All Colliders",
                    "Are you sure you want to remove all colliders from this building? This action cannot be undone and is recommended only after the building phase is complete (you will not be able to snap modules).",
                    "Yes",
                    "No"
                );

                if (confirm && buildingRoot != null)
                {
                    RemoveAllColliders(buildingRoot);
                }

                return confirm;
            }
            return false;
        }
        
        public static bool DrawButton_OptimizeHierarchy(GameObject buildingRoot)
        {
            if (GUILayout.Button("Optimize Hierarchy"))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Optimize Hierarchy",
                    "Remove all ‘meshHolder’ containers and flatten the hierarchy? This action cannot be undone and is recommended only after the building phase is complete",
                    "Yes",
                    "No"
                );

                if (confirm)
                    RemoveMeshHolders(buildingRoot);

                return confirm;
            }

            return false;
        }
        
        public static bool DrawButton_SaveBuildingAsPrefab(GameObject buildingRoot, string buildingName)
        {
            if (GUILayout.Button("Save Building as Prefab"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Building Prefab",
                    buildingName,
                    "prefab",
                    "Select a folder and filename for your building prefab"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    SaveBuildingAsPrefab(buildingRoot, path);
                    return true;
                }
            }
            return false;
        }
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

        public static ModuleType GetModuleTypeFromCollider(Collider col)
        {
            Transform current = col.transform;
            while (current != null)
            {
                if (current.name.Contains("Floor")) return ModuleType.FLOOR;
                if (current.name.Contains("Wall")) return ModuleType.WALL;
                if (current.name.Contains("Roof")) return ModuleType.ROOF;
                if (current.name.Contains("Props")) return ModuleType.PROPS;
                if (current.name.Contains("Junction")) return ModuleType.CORNER;
                current = current.parent;
            }
            
            return ModuleType.PROPS;
        }

        #endregion
        
        #region Ending Building Fucntions
        
        public static void RemoveAllColliders(GameObject root)
        {
            if (root == null) return;
            
            var all = root.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c in all)
            {
                GameObject.DestroyImmediate(c);
            }
        }
        
        public static void RemoveMeshHolders(GameObject root)
        {
            if (root == null) return;

            // prende tutti i Transform il cui nome contiene "meshholder", ignorando maiuscole/minuscole
            var holders = root
                .GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(t => t.name
                    .IndexOf("meshholder", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            foreach (var holder in holders)
            {
                var children = holder.Cast<Transform>().ToList();
                foreach (var child in children)
                    child.SetParent(holder.parent, worldPositionStays: true);

                UnityEngine.Object.DestroyImmediate(holder.gameObject);
            }
        }
        
        public static void SaveBuildingAsPrefab(GameObject buildingRoot, string assetPath)
        {
            if (buildingRoot == null)
            {
                Debug.LogError("Cannot save prefab: building root is null.");
                return;
            }

            // crea o sovrascrive il prefab asset
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                buildingRoot,
                assetPath,
                InteractionMode.UserAction
            );

            if (prefab != null)
                Debug.Log($"Building saved as prefab at {assetPath}");
            else
                Debug.LogError($"Failed to save building prefab at {assetPath}");
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

    enum ToolState
    {
        NoBuild,
        BuildInitiated
    };

    public enum ModuleType
    {
        FLOOR,
        WALL,
        ROOF,
        CORNER,
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