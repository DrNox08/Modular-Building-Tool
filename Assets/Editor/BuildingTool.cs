using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using BuildingToolUtils;


public class BuildingTool : EditorWindow
{
    [MenuItem("Tools/BuildingTool")]
    public static void OpenBuildingTool() => GetWindow<BuildingTool>();
    string toolTitle = "Modular Bulding Tool (Basic Version)";
    int selectedTab = 0;
    string[] tabTitles = { "Build", "Instructions" };
    //GameObject selectedPrefab;
    GameObject previewInstance;
    Module selectedObject;
    ToolState buildState;
    Collider closestModule;
    float snappingTreshold = 1.5f;
    Dictionary<string, bool> foldoutStates = new();


    // Paths
    string floorPath = "Assets/Prefabs/Floor";
    string wallPath = "Assets/Prefabs/Wall";
    string roofPath = "Assets/Prefabs/Roof";
    string propsPath = "Assets/Prefabs/Props";
    // --------------------------------------------------------------------------------------\\

    //Fields
    string buildingName = "New Building";
    Module[] floorPrefabs;
    Module[] wallPrefabs;
    Module[] roofPrefabs;
    Module[] propsPrefabs;

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
        if (BuildingToolUtility.DrawButton_Discard())
        {
            buildState = ToolState.NoBuild;
            selectedObject.Clear();
            DestroyImmediate(root);
            DestroyPreview();
            Repaint();
            SaveState();
        }
       
        GUILayout.EndHorizontal();
        BuildingToolUtility.DrawSeparationLine(Color.white);
        DrawFoldout("Floor", floorPrefabs);
        DrawFoldout("Wall", wallPrefabs);
        DrawFoldout("Roof", roofPrefabs);
        DrawFoldout("Props", propsPrefabs);
        BuildingToolUtility.DrawSeparationLine(Color.white);
        BuildingToolUtility.DrawCentered(() => BuildingToolUtility.DrawButton_OptimizeColliders(floorParent, wallParent, roofParent));

    }






    void ExecuteBuildTab()
    {
        if (buildState != ToolState.BuildInitiated) return;
        if (selectedObject.IsNull()) return;


        if (previewInstance == null || previewInstance.name != selectedObject.prefab.name + "_preview")
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
        floorPrefabs = LoadPrefabsFromFolder(floorPath, ModuleType.FLOOR);
        wallPrefabs = LoadPrefabsFromFolder(wallPath, ModuleType.WALL);
        roofPrefabs = LoadPrefabsFromFolder(roofPath, ModuleType.ROOF);
        propsPrefabs = LoadPrefabsFromFolder(propsPath, ModuleType.PROPS);
    }

    void InitFoldOutStates()
    {
        foldoutStates["Floor"] = false;
        foldoutStates["Walls"] = false;
        foldoutStates["Roof"] = false;
        foldoutStates["Props"] = false;
    }

    void DrawFoldout(string label, Module[] modules)
    {
        if (!foldoutStates.ContainsKey(label))
            foldoutStates[label] = false;

        foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], label, true);
        if (foldoutStates[label])
        {
            DrawPrefabGrid(modules);
        }
    }

    void DrawPrefabGrid(Module[] modules) // Foldouts Buttons
    {
        int buttonsPerRow = 4;
        int i = 0;

        while (i < modules.Length)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < buttonsPerRow && i < modules.Length; j++, i++)
            {
                GameObject prefab = modules[i].prefab;
                if (GUILayout.Button(prefab.name, GUILayout.Width(80), GUILayout.Height(80)))
                {
                    selectedObject = modules[i];
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
    GameObject CreateChildParent(string name)
    {
        GameObject parent = new GameObject(name);
        parent.transform.parent = root.transform;
        return parent;
    }
    private Module[] LoadPrefabsFromFolder(string path, ModuleType type)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
        Module[] modules = new Module[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            modules[i] = new Module(prefab, type);
        }

        return modules;
    }

    void SetPreviewMaterial(GameObject obj, Color color)
    {
        BuildingToolUtility.InitGhostMaterial();
        BuildingToolUtility.SetGhostColor(color);

        Material ghostMat = BuildingToolUtility.GetGhostMaterial();

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(); // Using arrays in case i add props

        foreach (Renderer renderer in renderers)
        {
            Material[] ghostMats = new Material[renderer.sharedMaterials.Length];

            for (int i = 0; i < ghostMats.Length; i++) ghostMats[i] = ghostMat;

            renderer.sharedMaterials = ghostMats;

        }

    }

    void CreatePreviewInstance() // Call in tick
    {
        if (selectedObject.IsNull()) return;
        if (previewInstance != null) DestroyPreview();

        previewInstance = Instantiate(selectedObject.prefab);
        previewInstance.name = selectedObject.prefab.name + "_preview";

        //previewInstance.hideFlags = HideFlags.HideInHierarchy;

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

            //Find close modules
            var bounds = CalculateBounds(previewInstance);
            Vector3 halfExtents = bounds.extents * 0.98f;
            Collider[] hits = Physics.OverlapBox(bounds.center,bounds.extents * 3f, previewInstance.transform.rotation);
            float closestDist = float.MaxValue;
            GameObject closestModule = null;
            foreach (var col in hits)
            {
                if (col.transform.IsChildOf(previewInstance.transform)) continue;
                float dist = Vector3.Distance(col.transform.position, previewInstance.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestModule = col.gameObject;
                }
            }
            //Try Snapping
            if(closestModule != null && closestDist <= snappingTreshold) SnapToClosestModule(closestModule);

           
            // Visual Feedback
            Color previewColor = IsValidPosition() ? Color.green : Color.red;
            SetPreviewMaterial(previewInstance, previewColor);

        }
    }

    private bool IsValidPosition()
    {
        if (previewInstance == null) return false;

        Bounds bounds = CalculateBounds(previewInstance);

        Vector3 halfExtents = bounds.extents * 0.98f; // Tolerance

        Collider[] overlaps = Physics.OverlapBox(bounds.center, halfExtents, previewInstance.transform.rotation);

        foreach (Collider col in overlaps)
        {
            if (!col.transform.IsChildOf(previewInstance.transform)) return false;
        }
        return true;
    }

    void SnapToClosestModule(GameObject target)
    {
        Bounds targetBounds = CalculateBounds(target);
        Bounds previewBounds = CalculateBounds(previewInstance);

        Vector3 direction = (previewInstance.transform.position - target.transform.position).normalized;
        Vector3 snapOffset = Vector3.zero;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            float offset = (targetBounds.extents.x + previewBounds.extents.x);
            snapOffset = new Vector3(Mathf.Sign(direction.x) * offset, 0, 0);
        }
        else
        {
            float offset = (targetBounds.extents.z + previewBounds.extents.z);
            snapOffset = new Vector3(0, 0, Mathf.Sign(direction.z) * offset);
        }

        // Applica il nuovo posizionamento
        Vector3 targetPos = target.transform.position + snapOffset;
        targetPos.y = previewInstance.transform.position.y;
        previewInstance.transform.position = targetPos;
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

    // Inputs
    void HandleInput_Placement()
    {
        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
        {
            if (!selectedObject.IsNull() && IsValidPosition())
            {
                ModuleType currentModuleType = selectedObject.moduleType;
                GameObject placed = Instantiate(selectedObject.prefab, previewInstance.transform.position, previewInstance.transform.rotation);
                placed.transform.parent = GetParent(currentModuleType).transform;
                placed.name = selectedObject.prefab.name + "_" + (placed.transform.parent.childCount + 1).ToString();
                placed.transform.GetChild(0).name = placed.name;
                Undo.RegisterCreatedObjectUndo(placed,"Object Spawned: "+placed.name);
            }
            e.Use();
        }
    }

    GameObject GetParent(ModuleType moduleType)
    {
        switch (moduleType)
        {
            case ModuleType.FLOOR:
                return floorParent = floorParent != null ? floorParent : CreateChildParent("Floors");

            case ModuleType.WALL:
                return wallParent = wallParent != null ? wallParent : CreateChildParent("Walls");

            case ModuleType.ROOF:
                return roofParent = roofParent != null ? roofParent : CreateChildParent("Roof");

            case ModuleType.PROPS:
                return propsParent = propsParent != null ? propsParent : CreateChildParent("Props");

            default:
                Debug.LogWarning("Unknown module type");
                return null;
        }
    }




    #endregion


}


