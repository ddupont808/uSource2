using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// In order to help gauge the size of mesh objects,
/// this script displays the dimensions in world space of a selected object 
/// with a MeshRenderer attached whenever it's selected.
/// </summary>
[CustomEditor(typeof(MeshRenderer))]
class MeshMeasure : Editor
{
    protected virtual void OnSceneGUI()
    {
        MeshRenderer meshRenderer = (MeshRenderer)target;

        if (meshRenderer == null)
        {
            return;
        }

        Handles.DrawWireCube(meshRenderer.bounds.center, meshRenderer.bounds.size);
    }
}