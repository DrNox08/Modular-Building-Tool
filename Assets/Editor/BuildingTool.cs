using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BuildingToolUtils;
using UnityEditor.SceneManagement;


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
    int currentFloor;
    float snappingTreshold = 1.5f;
    Dictionary<string, bool> foldoutStates = new();

    // Settings


    // Paths
    string floorPath = "Assets/Prefabs/Floor";
    string wallPath = "Assets/Prefabs/Wall";
    string roofPath = "Assets/Prefabs/Roof";
    string propsPath = "Assets/Prefabs/Props";
    string junctionsPath = "Assets/Prefabs/Junctions";
    // --------------------------------------------------------------------------------------\\

    //Fields
    string buildingName = "New Building";
    Module[] floorPrefabs;
    Module[] wallPrefabs;
    Module[] roofPrefabs;
    Module[] propsPrefabs;
    Module[] junctionsPrefabs;

    //Internal ---------------------------------------------------------------------------------\\
    bool verticalSnap;
    private Vector3 originalPreviewScale;
    float currentSnappingTreshold;

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
        currentFloor = 0;
        verticalSnap = false;
        currentSnappingTreshold = snappingTreshold;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
        DestroyImmediate(previewInstance);
        buildState = ToolState.NoBuild;
        SaveState();
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

    void DrawInstructionTab()
    {
        GUILayout.Space(10);
        GUILayout.Label("Available Commands", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Quick building commands:", MessageType.Info);
        GUILayout.Space(5);

        EditorGUILayout.LabelField("Spacebar",             "Place the selected module");
        EditorGUILayout.LabelField("Ctrl + Up Arrow",      "Go up one floor");
        EditorGUILayout.LabelField("Ctrl + Down Arrow",    "Go down one floor");
        EditorGUILayout.LabelField("Ctrl + Left Arrow",    "Rotate preview 90° left");
        EditorGUILayout.LabelField("Ctrl + Right Arrow",   "Rotate preview 90° right");
        EditorGUILayout.LabelField("Ctrl + Alt",           "Toggle vertical/horizontal snap");
        EditorGUILayout.LabelField("Shift + Scroll Wheel", "Scale the selected module");
        EditorGUILayout.LabelField("Ctrl + Middle Click",  "Reset preview to original scale");
        BuildingToolUtility.ForceSceneFocus();
    }

    private void DuringSceneGUI(SceneView view)
    {
        HandleScrollWheel();
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
                HandleStartBuilding();
                buildState = ToolState.BuildInitiated;
                SaveState();
                BuildingToolUtility.ForceSceneFocus();
            }
            
            GUILayout.EndHorizontal();
            GUILayout.Space(30);
            EditorGUILayout.HelpBox("Starting to build", MessageType.Info);
            GUILayout.Space(5);
            EditorGUILayout.LabelField(
                "If you insert a building name that already exists in the scene,\n" +
                "the construction will be resumed",
                EditorStyles.wordWrappedLabel
            );
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
        BuildingToolUtility.DrawCentered(() => GUILayout.Label("SETTINGS"));
        if (verticalSnap) GUILayout.Label("Snap: Vertical", EditorStyles.centeredGreyMiniLabel);
        else GUILayout.Label("Snap: Horizontal (Standard)", EditorStyles.centeredGreyMiniLabel);

        BuildingToolUtility.DrawSeparationLine(Color.white);
        GUILayout.Label("Current Floor: " + currentFloor / 3, EditorStyles.boldLabel);
        BuildingToolUtility.DrawSeparationLine(Color.white);
        DrawFoldout("Floor", floorPrefabs);
        DrawFoldout("Wall", wallPrefabs);
        DrawFoldout("Roof", roofPrefabs);
        DrawFoldout("Junctions", junctionsPrefabs);
        DrawFoldout("Props", propsPrefabs);
        BuildingToolUtility.DrawSeparationLine(Color.white);
        BuildingToolUtility.DrawCentered(() =>
            BuildingToolUtility.DrawButton_RemoveAllColliders(root));
        BuildingToolUtility.DrawCentered(() =>
        {
            if (BuildingToolUtility.DrawButton_OptimizeHierarchy(root))
                Debug.Log("Hierarchy optimized: all meshHolder containers removed.");
        });
        BuildingToolUtility.DrawCentered(() =>
        {
            if (BuildingToolUtility.DrawButton_SaveBuildingAsPrefab(root, buildingName))
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                DestroyImmediate(root);
                DestroyImmediate(previewInstance);
                buildState = ToolState.NoBuild;
            }
        });
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

        ElaborateInputs();
        UpdatePreviewPosition();
        //SceneView.RepaintAll();
    }

    #region Building Tool

    void LoadAllPrefabs()
    {
        floorPrefabs = LoadPrefabsFromFolder(floorPath, ModuleType.FLOOR);
        wallPrefabs = LoadPrefabsFromFolder(wallPath, ModuleType.WALL);
        roofPrefabs = LoadPrefabsFromFolder(roofPath, ModuleType.ROOF);
        propsPrefabs = LoadPrefabsFromFolder(propsPath, ModuleType.PROPS);
        junctionsPrefabs = LoadPrefabsFromFolder(junctionsPath, ModuleType.CORNER);
    }

    void InitFoldOutStates()
    {
        foldoutStates["Floor"] = false;
        foldoutStates["Walls"] = false;
        foldoutStates["Roof"] = false;
        foldoutStates["Junctions"] = false;
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
                    originalPreviewScale = prefab.transform.localScale;

                    EditorPrefs.SetString("SelectedPrefabPath", AssetDatabase.GetAssetPath(prefab));
                    FocusWindowIfItsOpen<SceneView>();
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

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

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

        previewInstance.hideFlags = HideFlags.HideInHierarchy;

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
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero + Vector3.up * currentFloor);
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        float enter;

        if (groundPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            previewInstance.transform.position = hitPoint;

            //Find close modules
            Bounds previewBounds = BuildingToolUtility.CalculateBounds(previewInstance);
            Bounds uniformedBoundsXZ = previewBounds.Uniform();

            Vector3 halfExtentsUniformed = uniformedBoundsXZ.extents;
            Vector3 verticalExtents = previewBounds.extents;

            halfExtentsUniformed.x *= 3;
            halfExtentsUniformed.z *= 3;
            halfExtentsUniformed.y = previewBounds.extents.y;

            Collider[] hits;
            if (!verticalSnap)
            {
                hits = Physics.OverlapBox(uniformedBoundsXZ.center, halfExtentsUniformed,
                    previewInstance.transform.rotation);
                Handles.DrawWireCube(uniformedBoundsXZ.center, halfExtentsUniformed * 2);
            }
            else
            {
                Vector3 adjustedExtents = new Vector3(
                    verticalExtents.x * 0.5f,
                    verticalExtents.y + 2f,
                    verticalExtents.z * 0.5f
                );

                Vector3 center = previewBounds.center + (Vector3.down * 2);

                hits = Physics.OverlapBox(center, adjustedExtents);
                Handles.DrawWireCube(center, adjustedExtents * 2);
                BuildingToolUtility.DrawSolidSphere(previewBounds.center + (Vector3.down * 4), 0.1f);
            }

            float closestDist = float.MaxValue;
            GameObject closestModule = null;
            foreach (var col in hits)
            {
                Handles.color = Color.magenta;
                Handles.DrawWireCube(col.bounds.center, col.bounds.size);
                if (col.transform.IsChildOf(previewInstance.transform)) continue;

                ModuleType targetType = BuildingToolUtility.GetModuleTypeFromCollider(col);

                if (selectedObject.moduleType == ModuleType.CORNER && targetType != ModuleType.WALL) continue;
                var colBounds = BuildingToolUtility.CalculateBounds(col.gameObject);
                float dist = Vector3.Distance(colBounds.BottomCenter(), uniformedBoundsXZ.BottomCenter());
                if (dist < closestDist || verticalSnap)
                {
                    closestDist = dist;
                    closestModule = col.gameObject;
                }
            }
            //Try Snapping

            if (closestModule != null && closestDist <= currentSnappingTreshold ||
                closestModule != null && verticalSnap)
                SnapToClosestModule(closestModule);

            // Visual Feedback
            Color previewColor = IsValidPosition() ? Color.green : Color.red;
            SetPreviewMaterial(previewInstance, previewColor);
            HandleUtility.Repaint();
            SceneView.RepaintAll();
        }
    }

    private bool IsValidPosition()
    {
        if (previewInstance == null) return false;

        Bounds bounds = BuildingToolUtility.CalculateBounds(previewInstance);

        Vector3 halfExtents = bounds.extents * 0.98f; // Tolerance

        Collider[]
            overlaps = Physics.OverlapBox(bounds.center, halfExtents,
                Quaternion.identity); // quaternion.identity fixes the invalid position issue

        foreach (Collider col in overlaps)
        {
            if (!col.transform.IsChildOf(previewInstance.transform)) return false;
        }

        return true;
    }

    void AdjustPreviewScale(float delta)
    {
        if (previewInstance == null) return;

        // 1) di quanto vogliamo variare (±0.1)
        float adjustAmount = Mathf.Sign(delta) * 0.1f;

        // 2) bounding‐box del prefab (coordinate locali, senza rotazioni/applicazioni in scena)
        Bounds prefabBounds = BuildingToolUtility.CalculateBounds(selectedObject.prefab);
        float prefabSizeX = prefabBounds.size.x;
        float prefabSizeZ = prefabBounds.size.z;

        // 3) scala corrente e nuova scala "tentata"
        Vector3 currentScale = previewInstance.transform.localScale;
        Vector3 newScale = currentScale;

        // se prefab "quasi quadrato" → uniform scaling
        if (Mathf.Abs(prefabSizeX - prefabSizeZ) < 0.01f)
        {
            newScale.x = Mathf.Max(0.1f, currentScale.x + adjustAmount);
            newScale.z = Mathf.Max(0.1f, currentScale.z + adjustAmount);
        }
        // altrimenti scelgo l’asse locale X o Z più lungo nel modello
        else if (prefabSizeX > prefabSizeZ)
        {
            newScale.x = Mathf.Max(0.1f, currentScale.x + adjustAmount);
        }
        else
        {
            newScale.z = Mathf.Max(0.1f, currentScale.z + adjustAmount);
        }

        // 4) applico la "scala tentata" e controllo collisioni
        previewInstance.transform.localScale = newScale;
        if (!IsValidPosition())
        {
            // se c’è intersezione, torno alla scala precedente
            previewInstance.transform.localScale = currentScale;
            Debug.Log("[Adjust] Scaling blocked: not enough space");
            return;
        }

        // 5) se valido, aggiorno la soglia di snap e provo a repaint
        currentSnappingTreshold += adjustAmount;
        HandleUtility.Repaint();
        SceneView.RepaintAll();
    }




    void SnapToClosestModule(GameObject target)
    {
        Bounds targetBounds = BuildingToolUtility.CalculateBounds(target);
        Bounds previewBounds = BuildingToolUtility.CalculateBounds(previewInstance);

        if (verticalSnap)
        {
            SnapVertically(target);
            return;
        }

        // --- Snapping for Corner Modules
        if (selectedObject.moduleType == ModuleType.CORNER)
        {
            Vector3 direction = (previewInstance.transform.position - target.transform.position).normalized;
            Vector3 offset = Vector3.zero;

            bool snapX = Mathf.Abs(direction.x) >= Mathf.Abs(direction.z);
            if (snapX)
            {
                float dx = targetBounds.extents.x + previewBounds.extents.x;
                offset = new Vector3(Mathf.Sign(direction.x) * dx, 0, 0);
                previewInstance.transform.rotation = Quaternion.Euler(0, direction.x >= 0 ? 90 : -90, 0);
            }
            else
            {
                float dz = targetBounds.extents.z + previewBounds.extents.z;
                offset = new Vector3(0, 0, Mathf.Sign(direction.z) * dz);
                previewInstance.transform.rotation = Quaternion.Euler(0, direction.z >= 0 ? 0 : 180, 0);
            }

            Vector3 targetPos = target.transform.position + offset;
            targetPos.y = previewInstance.transform.position.y;
            previewInstance.transform.position = targetPos;
            Debug.Log($"CORNER: dir = {direction}, target = {target.name}");
            Debug.Log($"target.extents = {targetBounds.extents}, preview.extents = {previewBounds.extents}");

            return;
        }

        // --- Snapping Standard
        Vector3 directionStandard = (previewInstance.transform.position - target.transform.position).normalized;
        Vector3 snapOffset = Vector3.zero;

        if (Mathf.Abs(directionStandard.x) > Mathf.Abs(directionStandard.z))
        {
            float offset = (targetBounds.extents.x + previewBounds.extents.x);
            snapOffset = new Vector3(Mathf.Sign(directionStandard.x) * offset, 0, 0);
        }
        else
        {
            float offset = (targetBounds.extents.z + previewBounds.extents.z);
            snapOffset = new Vector3(0, 0, Mathf.Sign(directionStandard.z) * offset);
        }

        Vector3 newPos = target.transform.position + snapOffset;
        newPos.y = previewInstance.transform.position.y;
        previewInstance.transform.position = newPos;
    }


    void SnapVertically(GameObject target)
    {
        Vector3 snapPos = new(target.transform.position.x, previewInstance.transform.position.y,
            target.transform.position.z);
        previewInstance.transform.position = snapPos;
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

            case ModuleType.CORNER:
                return wallParent = wallParent != null ? wallParent : CreateChildParent("Walls");

            default:
                Debug.LogWarning("Unknown module type");
                return null;
        }
    }
    
    private void HandleStartBuilding()
    {
        GameObject existing = GameObject.Find(buildingName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog(
                "Building Resumed",
                $"Hai ripreso la costruzione di “{buildingName}”.",
                "OK"
            );
            
            root = existing;

            Transform t;

            t = root.transform.Find("Floors");
            if (t != null) floorParent = t.gameObject;

            t = root.transform.Find("Walls");
            if (t != null) wallParent = t.gameObject;

            t = root.transform.Find("Roofs");
            if (t != null) roofParent = t.gameObject;

            t = root.transform.Find("Props");
            if (t != null) propsParent = t.gameObject;

            return;
        }
        
        CreateNewRoot();
        
    }


    // Inputs
    void HandleScrollWheel()
    {
        Event e = Event.current;
        if (e != null && e.type == EventType.ScrollWheel)
        {
            bool shiftHeld = (e.modifiers & EventModifiers.Shift) != 0;

            if (shiftHeld && previewInstance != null)
            {
                float scrollDelta = e.delta.x;
                AdjustPreviewScale(scrollDelta);

                e.Use();
                HandleUtility.Repaint();
                SceneView.RepaintAll();
            }
        }
    }


    void ElaborateInputs()
    {
        Event e = Event.current;

        if (EditorKeyToggle.SpaceBar())
        {
            if (!selectedObject.IsNull() && IsValidPosition())
            {
                ModuleType currentModuleType = selectedObject.moduleType;
                GameObject placed = Instantiate(selectedObject.prefab, previewInstance.transform.position,
                    previewInstance.transform.rotation);
                placed.transform.localScale = previewInstance.transform.localScale; // Keep the modified scale
                placed.transform.parent = GetParent(currentModuleType).transform;
                placed.name = selectedObject.prefab.name + "_" + (placed.transform.parent.childCount + 1).ToString();
                placed.transform.GetChild(0).name = placed.name;
                placed.transform.name = placed.name + "_MeshHolder";
                Undo.RegisterCreatedObjectUndo(placed, "Object Spawned: " + placed.name);
            }
        }

        if (EditorKeyToggle.Ctrl(KeyCode.UpArrow))
        {
            currentFloor += 3;
            SceneView.lastActiveSceneView.Repaint();
            BuildingToolUtility.RefocusCameraOnTarget(previewInstance);
            Repaint();
        }

        if (EditorKeyToggle.Ctrl(KeyCode.DownArrow))
        {
            currentFloor = currentFloor >= 3 ? currentFloor -= 3 : 0;
            BuildingToolUtility.RefocusCameraOnTarget(previewInstance);
            Repaint();
        }

        if (EditorKeyToggle.Ctrl(KeyCode.LeftArrow))
        {
            previewInstance.transform.Rotate(Vector3.up * -90);
            SceneView.lastActiveSceneView.Repaint();
        }

        if (EditorKeyToggle.Ctrl(KeyCode.RightArrow))
        {
            previewInstance.transform.Rotate(Vector3.up * 90);
            SceneView.lastActiveSceneView.Repaint();
        }

        if (EditorKeyToggle.Ctrl(KeyCode.LeftAlt))
        {
            verticalSnap = !verticalSnap;
            Repaint();
        }

        if (EditorKeyToggle.CtrlMiddleMouseClick())
        {
            if (previewInstance != null)
            {
                previewInstance.transform.localScale = originalPreviewScale;
                currentSnappingTreshold = snappingTreshold;
                HandleUtility.Repaint();
                SceneView.RepaintAll();
            }
        }
    }

    #endregion
}