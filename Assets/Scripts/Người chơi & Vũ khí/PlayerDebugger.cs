using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerDebugger
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Debug.Log("<color=orange><b>[PlayerDebugger] Started monitoring!</b></color>");
        SceneManager.sceneLoaded += OnSceneLoaded;
        ReportState("AfterSceneLoad");
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReportState("OnSceneLoaded");
    }

    public static void ReportState(string context)
    {
        Debug.Log($"<color=orange><b>--- Player State Report ({context}) ---</b></color>");

        // 1. Find all objects with PlayerStats
        PlayerStats[] allStats = Object.FindObjectsByType<PlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"Total PlayerStats found in hierarchy: {allStats.Length}");

        for (int i = 0; i < allStats.Length; i++)
        {
            PlayerStats stats = allStats[i];
            GameObject go = stats.gameObject;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            PlayerMovement pm = go.GetComponent<PlayerMovement>();
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            Collider2D col = go.GetComponent<Collider2D>();

            Debug.Log($"PlayerStats [{i}]:\n" +
                      $"- Name: {go.name}\n" +
                      $"- Active in hierarchy: {go.activeInHierarchy}\n" +
                      $"- Position: {go.transform.position}\n" +
                      $"- Scale: {go.transform.localScale}\n" +
                      $"- Layer: {LayerMask.LayerToName(go.layer)}\n" +
                      $"- Tag: {go.tag}\n" +
                      $"- SpriteRenderer: {(sr != null ? $"Active={sr.enabled}, Sprite={sr.sprite?.name}, Color={sr.color}, SortingOrder={sr.sortingOrder}, SortingLayer={sr.sortingLayerName}" : "MISSING")}\n" +
                      $"- PlayerMovement: {(pm != null ? $"Active={pm.enabled}" : "MISSING")}\n" +
                      $"- Rigidbody2D: {(rb != null ? $"Simulated={rb.simulated}, Velocity={rb.linearVelocity}" : "MISSING")}\n" +
                      $"- Collider2D: {(col != null ? $"Enabled={col.enabled}" : "MISSING")}");
        }

        // 2. Camera Report
        Camera mainCam = Camera.main;
        Debug.Log($"Camera.main: {(mainCam != null ? mainCam.gameObject.name : "NULL")}");

        Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"Total Cameras in hierarchy: {allCams.Length}");
        for (int i = 0; i < allCams.Length; i++)
        {
            Camera cam = allCams[i];
            DungeonCastle.CameraControl.CameraFollow cf = cam.GetComponent<DungeonCastle.CameraControl.CameraFollow>();
            Debug.Log($"Camera [{i}]:\n" +
                      $"- Name: {cam.gameObject.name}\n" +
                      $"- Active in hierarchy: {cam.gameObject.activeInHierarchy}\n" +
                      $"- Position: {cam.transform.position}\n" +
                      $"- Orthographic: {cam.orthographic}, Size: {cam.orthographicSize}\n" +
                      $"- CameraFollow: {(cf != null ? $"Active={cf.enabled}, Target={(cf.transform != null ? "HAS_CF" : "NO_CF")}" : "MISSING")}");
        }

        // 3. Spawners Report
        Debug.Log($"PlayerStats.Instance: {(PlayerStats.Instance != null ? PlayerStats.Instance.gameObject.name : "NULL")}");
    }
}
