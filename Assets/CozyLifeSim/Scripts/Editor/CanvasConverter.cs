using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace CozyLifeSim.Editor
{
    public static class CanvasConverter
    {
        [MenuItem("Tools/CozySim/Inspect UI Hierarchy")]
        public static void InspectUIHierarchy()
        {
            string path = "d:/soflware/Unity/Source/Cozy_Life_Sim/.agent/scratch/ui_hierarchy.txt";
            using (StreamWriter writer = new StreamWriter(path))
            {
                GameObject canvas = GameObject.Find("Canvas");
                if (canvas != null)
                {
                    writer.WriteLine("Canvas Details:");
                    writer.WriteLine($"  World Position: {canvas.transform.position}");
                    writer.WriteLine($"  Local Scale: {canvas.transform.localScale}");
                    
                    Transform uiRoot = canvas.transform.Find("UI_Root");
                    if (uiRoot != null)
                    {
                        writer.WriteLine("\nUI_Root Children:");
                        writer.WriteLine("-----------------");
                        DumpHierarchy(uiRoot, writer, 1);
                    }
                    else
                    {
                        writer.WriteLine("UI_Root not found under Canvas.");
                    }
                }
                else
                {
                    writer.WriteLine("Canvas not found!");
                }

                GameObject worldObj = GameObject.Find("World_Object");
                if (worldObj != null)
                {
                    writer.WriteLine("\nWorld_Object Children:");
                    writer.WriteLine("----------------------");
                    DumpHierarchy(worldObj.transform, writer, 1);
                }
                else
                {
                    writer.WriteLine("\nWorld_Object not found!");
                }
            }
            Debug.Log("UI & World hierarchy inspection complete!");
        }

        private static void DumpHierarchy(Transform t, StreamWriter writer, int depth)
        {
            string indent = new string(' ', depth * 2);
            foreach (Transform child in t)
            {
                string posStr = $"Pos: {child.localPosition}, WorldPos: {child.position}, Scale: {child.localScale}";
                writer.WriteLine($"{indent}- GameObject: {child.name} (Active: {child.gameObject.activeSelf}) | {posStr}");
                
                // Print key components
                Component[] comps = child.GetComponents<Component>();
                foreach (Component comp in comps)
                {
                    if (comp == null) continue;
                    string typeName = comp.GetType().Name;
                    if (typeName != "Transform" && typeName != "RectTransform" && typeName != "CanvasRenderer")
                    {
                        string extra = "";
                        if (comp is SpriteRenderer sr && sr.sprite != null)
                        {
                            extra = $" | Sprite: {sr.sprite.name}";
                        }
                        else if (comp is Image img && img.sprite != null)
                        {
                            extra = $" | Sprite: {img.sprite.name}";
                        }
                        writer.WriteLine($"{indent}    * Component: {typeName}{extra}");
                    }
                }

                // If child is active, dump sub-children recursively
                if (child.childCount > 0)
                {
                    DumpHierarchy(child, writer, depth + 1);
                }
            }
        }
    }
}
