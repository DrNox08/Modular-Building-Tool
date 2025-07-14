# Building Tool (Unity)

A Unity editor tool for rapidly prototyping buildings using simple modular primitives.

## Description

This project provides an editor extension that lets you assemble structures in-scene using basic Unity primitives (cubes, cylinders, planes, etc.). Each module can be previewed, snapped into place, paused/resumed during placement, and saved as a prefab. The tool also automatically organizes and cleans up the scene hierarchy.

## Features

- **Container Object Creation**  
  Spawns a new GameObject in the scene that holds all placed modules, organizing them by module type into a clear, editable hierarchy.
- **Module Preview**  
  Displays a visual preview of the module before spawning, with the option to toggle gizmos on or off.
- **Pause/Resume Build**  
  Pause and resume the building process without losing your current state.
- **Dynamic Snapping**  
  Automatically align the active module to neighboring modules for precise placement.
- **Alternate-Snap Targeting**  
  Cycle snap targets to attach to different underlying modules as needed.
- **Hierarchy Cleanup**  
  Automatically remove empty or unwanted GameObjects to keep the hierarchy tidy.
- **Collider Removal**  
  Disable or delete colliders on placed modules to avoid physics conflicts.
- **Save as Prefab**  
  Export the container and its child modules as a prefab in your project.

> Detailed instructions and settings are available in the tool’s **Help** tab.

## Requirements

- Unity 2020.3 LTS or newer  
- Scripting Define Symbols enabled for editor scripts  
- (Optional) **Editor Coroutines** package for coroutine support in custom tools

## Installation

1. **Clone the repository**  
   ```bash
   git clone https://github.com/your-username/building-tool-unity.git

2. Or you can download the package directly from the release