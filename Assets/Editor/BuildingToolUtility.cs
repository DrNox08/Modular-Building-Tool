using System;
using System.Runtime.Remoting.Messaging;
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

        public static Material GetGhostMaterial()=> ghostMaterial;
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
            if(GUILayout.Button("Optimize Colliders", EditorStyles.miniButtonMid, GUILayout.Width(500)))
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
