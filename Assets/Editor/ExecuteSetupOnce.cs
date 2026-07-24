using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class ExecuteSetupOnce
{
    static ExecuteSetupOnce()
    {
        EditorApplication.delayCall += Execute;
    }

    private static void Execute()
    {
        string flagPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp/setup_once_done.txt");
        if (File.Exists(flagPath)) return;

        // Perform setup
        SetupPlayerStatsUI.SetupPlayerStats();

        // Write flag to prevent running again
        try
        {
            File.WriteAllText(flagPath, "Done");
            Debug.Log("[ExecuteSetupOnce] Setup completed successfully and saved flag.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ExecuteSetupOnce] Failed to write flag file: " + ex.Message);
        }
    }
}
