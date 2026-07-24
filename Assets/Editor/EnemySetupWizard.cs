using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class EnemySetupWizard : EditorWindow
{
    [MenuItem("Tools/1-Click Setup Orc Enemy")]
    public static void SetupOrcEnemy()
    {
        string enemyFolder = "Assets/tainguyen/quai";
        string controllerPath = enemyFolder + "/OrcAnimator.controller";
        string prefabPath = enemyFolder + "/OrcPrefab.prefab";

        Debug.Log("Bắt đầu tự động thiết lập quái vật Orc từ file Aseprite...");

        // 1. TẢI TOÀN BỘ SUB-ASSETS TỪ FILE ASEPRITE
        string orcAsePath = enemyFolder + "/Orc.aseprite";
        Object[] orcAssets = AssetDatabase.LoadAllAssetsAtPath(orcAsePath);

        if (orcAssets == null || orcAssets.Length == 0)
        {
            // Thử tìm tất cả tệp aseprite/png trong thư mục quái
            string[] guids = AssetDatabase.FindAssets("", new string[] { enemyFolder });
            List<Object> foundAssets = new List<Object>();
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(p);
                if (loaded != null) foundAssets.AddRange(loaded);
            }
            orcAssets = foundAssets.ToArray();
        }

        List<Object> allAssets = new List<Object>();
        if (orcAssets != null) allAssets.AddRange(orcAssets);

        AnimationClip idleClip = null;
        AnimationClip walkClip = null;
        AnimationClip attackClip = null;
        AnimationClip deathClip = null;

        foreach (Object asset in allAssets)
        {
            if (asset is AnimationClip clip)
            {
                string nameLower = clip.name.ToLower();
                if (nameLower.Contains("idle")) idleClip = clip;
                else if (nameLower.Contains("walk") || nameLower.Contains("run")) walkClip = clip;
                else if (nameLower.Contains("attack")) attackClip = clip;
                else if (nameLower.Contains("death") || nameLower.Contains("die")) deathClip = clip;
            }
        }

        // Clip fallback nếu Aseprite đặt tên ngầm định
        List<AnimationClip> allClips = allAssets.OfType<AnimationClip>().ToList();
        if (allClips.Count > 0)
        {
            if (idleClip == null) idleClip = allClips[0];
            if (walkClip == null) walkClip = allClips.Count > 1 ? allClips[1] : allClips[0];
            if (attackClip == null) attackClip = allClips.Count > 2 ? allClips[2] : allClips[0];
        }

        // 2. CẤU HÌNH ANIMATOR CONTROLLER
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        rootStateMachine.states = new ChildAnimatorState[0];
        rootStateMachine.anyStateTransitions = new AnimatorStateTransition[0];

        AnimatorState idleState = rootStateMachine.AddState("Idle");
        if (idleClip != null) idleState.motion = idleClip;

        AnimatorState walkState = rootStateMachine.AddState("Walk");
        if (walkClip != null) walkState.motion = walkClip;

        AnimatorState attackState = rootStateMachine.AddState("Attack");
        if (attackClip != null) attackState.motion = attackClip;

        // Transition Idle <-> Walk
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        // AnyState -> Attack
        AnimatorStateTransition toAttack = rootStateMachine.AddAnyStateTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        toAttack.hasExitTime = false;
        toAttack.duration = 0f;

        // Attack -> Idle
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1.0f;
        attackToIdle.duration = 0f;

        rootStateMachine.defaultState = idleState;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // 3. TẠO HOẶC CẬP NHẬT GAMEOBJECT ORC TRONG SCENE
        GameObject existingOrc = GameObject.Find("DarkFantasyOrc");
        if (existingOrc != null)
        {
            DestroyImmediate(existingOrc);
        }

        GameObject orcObj = new GameObject("DarkFantasyOrc");
        orcObj.tag = "Untagged"; // Hoặc "Enemy" nếu bạn có tag này
        
        int enemyLayerIdx = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIdx != -1)
        {
            orcObj.layer = enemyLayerIdx;
        }

        SpriteRenderer sr = orcObj.AddComponent<SpriteRenderer>();
        Sprite firstSprite = allAssets.OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault(s => s.name.Contains("Frame_0"));
        if (firstSprite == null) firstSprite = allAssets.OfType<Sprite>().FirstOrDefault();
        if (firstSprite != null) sr.sprite = firstSprite;

        Animator anim = orcObj.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        Rigidbody2D rb = orcObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Căn chỉnh Collider theo Sprite bounds
        Vector3 localSpriteCenter = orcObj.transform.InverseTransformPoint(sr.bounds.center);
        Vector3 spriteSize = sr.bounds.size;

        CapsuleCollider2D col = orcObj.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(spriteSize.x * 0.7f, spriteSize.y * 0.9f);
        col.offset = new Vector2(localSpriteCenter.x, localSpriteCenter.y);

        // Gắn script EnemyAI
        EnemyAI enemyAI = orcObj.AddComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.spriteOffset = new Vector2(localSpriteCenter.x, localSpriteCenter.y);
            enemyAI.moveSpeed = 2.5f;
            enemyAI.detectionRadius = 5.0f;
            enemyAI.attackRange = 0.35f;
            enemyAI.attackCooldown = 1.0f;
            enemyAI.attackDuration = 0.45f;
            enemyAI.attackDelay = 0.12f;
            enemyAI.lungeForce = 1.0f;
            enemyAI.useCircleHitbox = true;
            enemyAI.attackRadius = 0.32f;
            enemyAI.attackBoxSize = new Vector2(0.4f, 0.3f);
            enemyAI.attackOffset = 0.20f;
            enemyAI.attackDamage = 10;
            enemyAI.showHitbox = true;
            enemyAI.playerLayer = ~0;
        }

        // Gắn script EnemyHealth quản lý máu & thanh máu
        EnemyHealth enemyHealth = orcObj.AddComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.maxHealth = 50;
            enemyHealth.barHeightOffset = 0.06f;
            enemyHealth.barWidth = 0.28f;
            enemyHealth.barHeight = 0.04f;
            enemyHealth.alwaysShowHealthBar = true;
        }

        // Đặt vị trí gần vị trí nhân vật Player nếu có
        GameObject playerObj = GameObject.Find("DarkFantasyPlayer");
        if (playerObj != null)
        {
            orcObj.transform.position = playerObj.transform.position + new Vector3(3f, 0f, 0f);
        }
        else
        {
            orcObj.transform.position = new Vector3(3f, 0f, 0f);
        }

        // Lưu Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(orcObj, prefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = orcObj;

        Debug.Log("<color=green>Hoàn tất cài đặt Quái vật Orc! Prefab đã lưu tại: </color>" + prefabPath);
    }
}
