using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozyLifeSim.Editor
{
    public static class CozyVisualSnapshotUtility
    {
        private const string SnapshotPath = "C:/tmp/cozy-life-sim-gameview-snapshot.png";

        [MenuItem("Tools/CozySim/Capture Visual Layout Snapshot")]
        public static void CaptureVisualLayoutSnapshot()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath));
            ScreenCapture.CaptureScreenshot(SnapshotPath);
            Debug.Log($"[CozySim Snapshot] Requested Game view snapshot at {SnapshotPath}");
        }
    }
}
