using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public class BuildingTool : EditorWindow
{
    [MenuItem("Tools/BuildingTool")]
    public static void OpenBuildingTool() => GetWindow<BuildingTool>();
    string toolTitle = "Modular Bulding Tool (Basic Version)";
    int selectedTab = 0;
    string[] tabTitles = { "Build", "Instructions" };
    GameObject selectedPrefab;
    GameObject previewInstance;
    enum ToolState { NoBuild, BuildInitiated };
    ToolState buildState;
    Dictionary<string, bool> foldoutStates = new();


    // Paths
    string floorPath = "Assets/Prefabs/Floor";
    string wallPath = "Assets/Prefabs/Wall";
    string roofPath = "Assets/Prefabs/Roof";
    string propsPath = "Assets/Prefabs/Props";
    // --------------------------------------------------------------------------------------\\

    //Fields
    string buildingName = "New Building";
    GameObject[] floorPrefabs;
    GameObject[] wallPrefabs;
    GameObject[] roofPrefabs;
    GameObject[] propsPrefabs;

    //State Management
    void SaveState() => EditorPrefs.SetInt("BuildState", (int)buildState);
    void LoadState()
    {
        if (EditorPrefs.HasKey("BuildState"))
        {
            buildState = (ToolState)EditorPrefs.GetInt("BuildState");
        }
        else buildState = ToolState.NoBuild;
    }

    // Hierarchy
    GameObject root;
    GameObject floorParent;
    GameObject wallParent;
    GameObject roofParent;
    GameObject propsParent;
    private void OnEnable()
    {
        LoadState();
        LoadAllPrefabs();
        InitFoldOutStates();
        SceneView.duringSceneGui += DuringSceneGUI;

    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }
    private void OnGUI()
    {
        // Title
        GUILayout.Space(5);
        var centerBold = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField(toolTitle, centerBold, GUILayout.ExpandWidth(true));
        GUILayout.Space(15);
        //Tabs
        selectedTab = GUILayout.Toolbar(selectedTab, tabTitles, EditorStyles.toolbarButton);

        switch (selectedTab)
        {
            case 0: DrawBuildTab(buildState); break;
            case 1: DrawInstructionTab(); break;
        }

    }
    private void DuringSceneGUI(SceneView view)
    {
        ExecuteBuildTab();
    }
        


    void DrawBuildTab(ToolState state)
    {
        // Init Build Tab
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (state == ToolState.NoBuild)
        {
            GUILayout.Label("Building Name", EditorStyles.label);
            buildingName = EditorGUILayout.TextField(buildingName);
            if (GUILayout.Button("Start Building", EditorStyles.miniButtonRight))
            {
                CreateNewRoot();
                buildState = ToolState.BuildInitiated;
                SaveState();
            }
            GUILayout.EndHorizontal();
            return;
        }
        GUILayout.EndHorizontal();

        // ACTUAL BUILDING TOOL
        GUILayout.BeginHorizontal();
        GUILayout.Label(buildingName, EditorStyles.boldLabel);
        if (GUILayout.Button("Discard Building", EditorStyles.miniButtonRight))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "WARNING",
                "This will delete this building for good, are you sure?",
                "Yes",
                "No");

            if (confirm)
            {
                buildState = ToolState.NoBuild;
                selectedPrefab = null;
                DestroyImmediate(root);
                DestroyPreview();
                Repaint();
                SaveState();
            }
        }
        GUILayout.EndHorizontal();

        DrawFoldout("Floor", floorPrefabs);
        DrawFoldout("Walls", wallPrefabs);
        DrawFoldout("Roof", roofPrefabs);
        DrawFoldout("Props", propsPrefabs);

    }






    void ExecuteBuildTab()
    {
        if (buildState != ToolState.BuildInitiated) return;
        if (selectedPrefab == null) return;


        if (previewInstance == null || previewInstance.name != selectedPrefab.name + "_preview")
        {
            DestroyPreview();
            CreatePreviewInstance();
        }

        UpdatePreviewPosition();
        HandleInput_Placement();
        SceneView.RepaintAll();
    }
    void DrawInstructionTab()
    {

    }

    #region Building Tools
    void LoadAllPrefabs()
    {
        floorPrefabs = LoadPrefabsFromFolder(floorPath);
        wallPrefabs = LoadPrefabsFromFolder(wallPath);
        roofPrefabs = LoadPrefabsFromFolder(roofPath);
        propsPrefabs = LoadPrefabsFromFolder(propsPath);
    }

    void InitFoldOutStates()
    {
        foldoutStates["Floor"] = false;
        foldoutStates["Walls"] = false;
        foldoutStates["Roof"] = false;
        foldoutStates["Props"] = false;
    }

    void DrawFoldout(string label, GameObject[] prefabs)
    {
        if (!foldoutStates.ContainsKey(label))
            foldoutStates[label] = false;

        foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], label, true);
        if (foldoutStates[label])
        {
            DrawPrefabGrid(prefabs);
        }
    }

    void DrawPrefabGrid(GameObject[] prefabs)
    {
        int buttonsPerRow = 4;
        int i = 0;

        while (i < prefabs.Length)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < buttonsPerRow && i < prefabs.Length; j++, i++)
            {
                GameObject prefab = prefabs[i];
                if (GUILayout.Button(prefab.name, GUILayout.Width(80), GUILayout.Height(80)))
                {
                    selectedPrefab = prefab;
                    EditorPrefs.SetString("SelectedPrefabPath", AssetDatabase.GetAssetPath(prefab));
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    void CreateNewRoot()
    {
        root = new GameObject(buildingName);
        root.transform.position = Vector3.zero;
    }
    private GameObject[] LoadPrefabsFromFolder(string path)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
        GameObject[] prefabs = new GameObject[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        return prefabs;
    }

    void SetPreviewMaterial(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(); // Using arrays in case i add props

        foreach (Renderer renderer in renderers)
        {
            Material ghostMat = new Material(Shader.Find("Unlit/Color"));
            ghostMat.color = new Color(color.r, color.g, color.b, 0.4f); // Set Alpha

            Material[] ghostMats = new Material[renderer.sharedMaterials.Length];

            for (int i = 0; i < ghostMats.Length; i++) ghostMats[i] = ghostMat;
        }
    }

    void CreatePreviewInstance() // Call in tick
    {
        if (selectedPrefab == null) return;
        if (previewInstance != null) DestroyPreview();

        previewInstance = Instantiate(selectedPrefab);
        previewInstance.name = selectedPrefab.name + "_preview";

        SetPreviewMaterial(previewInstance, Color.red);
    }

    void DestroyPreview()
    {
        if (previewInstance != null) DestroyImmediate(previewInstance);
        previewInstance = null;
    }

    void UpdatePreviewPosition() // Call in tick
    {
        if (previewInstance == null) return;

        //Plane on Y = 0;
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        float enter;

        if (groundPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            previewInstance.transform.position = hitPoint;

            // Visual Feedback
            Color previewColor = IsValidPosition() ? Color.green : Color.red;
        }
    }
     



    private bool IsValidPosition()
    {
        if (previewInstance == null) return false;

        Bounds bounds = CalculateBounds(previewInstance);

        Vector3 halfExtents = bounds.extents * 0.98f; // Tollerance

        Collider[] overlaps = Physics.OverlapBox(bounds.center, halfExtents, previewInstance.transform.rotation);

        foreach (Collider col in overlaps)
        {
            if (!col.transform.IsChildOf(previewInstance.transform)) return false;
        }
        return true;
    }

    Bounds CalculateBounds(GameObject obj)
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

    void HandleInput_Placement()
    {
        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
        {
            Debug.Log("accetto input");
            e.Use();
        }
    }


    #endregion


}


