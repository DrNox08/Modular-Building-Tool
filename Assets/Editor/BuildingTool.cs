using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

public class BuildingTool : EditorWindow
{
    [MenuItem("Tools/BuildingTool")]
    public static void OpenBuildingTool() => GetWindow<BuildingTool>();
    string toolTitle = "Modular Bulding Tool (Basic Version)";
    int selectedTab = 0;
    string[] tabTitles = { "Build", "Instructions" };
    GameObject selectedPrefab;
    enum ToolState { NoBuild, BuildInitiated };
    ToolState buildState;
    // --------------------------------------------------------------------------------------\\
    //Fields
    string buildingName = "New Building";

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
    private void OnEnable()
    {
        LoadState();
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
                Repaint();
                SaveState();
            }
        }
        GUILayout.EndHorizontal();
    }






    private void ExecuteBuildTab()
    {
        if (selectedPrefab == null) return;
    }
    void DrawInstructionTab()
    {

    }

    #region Building Tools

    void CreateNewRoot()
    {
        root = new GameObject(buildingName);
        root.transform.position = Vector3.zero;
    }

    #endregion


}
