using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;

namespace BuildingToolUtils
{
    public static class BuildingToolUtility
    {
        static Material ghostMaterial;

        public static void InitGhostMaterial()
        {
            if (ghostMaterial == null)
            {
                ghostMaterial = new Material(Shader.Find("Unlit/Color"));
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
