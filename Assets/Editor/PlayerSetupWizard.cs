using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PlayerSetupWizard : EditorWindow
{
    [MenuItem("Tools/1-Click Setup Soldier Player")]
    public static void SetupPlayer()
    {
        string characterFolder = "Assets/tainguyen/player";
        string controllerPath = characterFolder + "/PlayerAnimator.controller";
        string prefabPath = characterFolder + "/PlayerPrefab.prefab";

        Debug.Log("Bắt đầu tự động thiết lập nhân vật Soldier bằng hoạt ảnh gốc từ file Aseprite...");

        // ========================================================
        // 0. SAO LƯU THÔNG SỐ TÙY CHỈNH CŨ (TRÁNH BỊ GHI ĐÈ RESET)
        // ========================================================
        float savedMoveSpeed = 5f;
        float savedSmoothing = 0.05f;
        float savedAttackCooldown = 0.35f;
        float savedAttackDuration = 0.3f;
        float savedAttackDelay = 0.1f;
        float savedLungeForce = 3f;
        float savedComboWindow = 1.0f;
        Vector2 savedAttackBoxSize = new Vector2(0.4f, 0.3f);
        float savedAttackOffset = 0.2f;
        LayerMask savedEnemyLayer = 0;
        int savedAttackDamage = 10;
        Vector2 savedColliderSize = Vector2.zero;
        Vector2 savedColliderOffset = Vector2.zero;

        // Thử tìm trong Scene hiện tại trước
        GameObject oldPlayer = GameObject.Find("DarkFantasyPlayer");
        if (oldPlayer != null)
        {
            PlayerMovement oldMove = oldPlayer.GetComponent<PlayerMovement>();
            if (oldMove != null)
            {
                savedMoveSpeed = oldMove.moveSpeed;
                savedSmoothing = oldMove.movementSmoothing;
                savedAttackCooldown = oldMove.attackCooldown;
                savedAttackDuration = oldMove.attackDuration;
                savedAttackDelay = oldMove.attackDelay;
                savedLungeForce = oldMove.lungeForce;
                savedComboWindow = oldMove.comboResetWindow;
                savedAttackBoxSize = oldMove.attackBoxSize;
                savedAttackOffset = oldMove.attackOffset;
                savedEnemyLayer = oldMove.enemyLayer;
                savedAttackDamage = oldMove.attackDamage;
            }

            CapsuleCollider2D oldCol = oldPlayer.GetComponent<CapsuleCollider2D>();
            if (oldCol != null)
            {
                savedColliderSize = oldCol.size;
                savedColliderOffset = oldCol.offset;
            }

            DestroyImmediate(oldPlayer);
        }
        else
        {
            // Nếu không có trong scene, thử đọc từ tệp Prefab cũ để lấy lại các chỉ số đã lưu
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset != null)
            {
                PlayerMovement oldMove = prefabAsset.GetComponent<PlayerMovement>();
                if (oldMove != null)
                {
                    savedMoveSpeed = oldMove.moveSpeed;
                    savedSmoothing = oldMove.movementSmoothing;
                    savedAttackCooldown = oldMove.attackCooldown;
                    savedAttackDuration = oldMove.attackDuration;
                    savedAttackDelay = oldMove.attackDelay;
                    savedLungeForce = oldMove.lungeForce;
                    savedComboWindow = oldMove.comboResetWindow;
                    savedAttackBoxSize = oldMove.attackBoxSize;
                    savedAttackOffset = oldMove.attackOffset;
                    savedEnemyLayer = oldMove.enemyLayer;
                    savedAttackDamage = oldMove.attackDamage;
                }

                CapsuleCollider2D oldCol = prefabAsset.GetComponent<CapsuleCollider2D>();
                if (oldCol != null)
                {
                    savedColliderSize = oldCol.size;
                    savedColliderOffset = oldCol.offset;
                }
            }
        }

        // Tự động gán layer Enemy làm mặc định nếu layer mask trống
        if (savedEnemyLayer.value == 0)
        {
            int enemyLayerIdx = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIdx != -1)
            {
                savedEnemyLayer = 1 << enemyLayerIdx;
            }
            else
            {
                savedEnemyLayer = 1 << 8; // Fallback mặc định
            }
        }

        // ========================================================
        // 1. NẠP HOẠT ẢNH GỐC (SUB-ASSETS) TỪ FILE ASEPRITE
        // ========================================================
        AnimationClip idleClip = null;
        AnimationClip walkClip = null;
        AnimationClip attackClip1 = null;
        AnimationClip attackClip2 = null;
        AnimationClip attackClip3 = null;
        AnimationClip deathClip = null;

        string soldierAsePath = characterFolder + "/Soldier.aseprite";
        string soldier1AsePath = characterFolder + "/Soldier 1.aseprite";

        // Tải toàn bộ sub-assets từ cả 2 file Aseprite
        Object[] soldierAssets = AssetDatabase.LoadAllAssetsAtPath(soldierAsePath);
        Object[] soldier1Assets = AssetDatabase.LoadAllAssetsAtPath(soldier1AsePath);

        List<Object> allAssets = new List<Object>();
        if (soldierAssets != null) allAssets.AddRange(soldierAssets);
        if (soldier1Assets != null) allAssets.AddRange(soldier1Assets);

        foreach (Object asset in allAssets)
        {
            if (asset is AnimationClip clip)
            {
                string nameLower = clip.name.ToLower();
                
                // Trùng khớp tên gốc được sinh ra bởi Aseprite Importer
                if (clip.name == "Soldier 1")
                {
                    idleClip = clip;
                }
                else if (clip.name == "Soldier")
                {
                    walkClip = clip;
                }
                else if (nameLower.Contains("attack01") || nameLower == "attack1")
                {
                    attackClip1 = clip;
                }
                else if (nameLower.Contains("attack02") || nameLower == "attack2")
                {
                    attackClip2 = clip;
                }
                else if (nameLower.Contains("attack03") || nameLower == "attack3")
                {
                    attackClip3 = clip;
                }
                else if (nameLower.Contains("death") || nameLower == "die")
                {
                    deathClip = clip;
                }
            }
        }

        // Kiểm tra dự phòng nếu đặt tên khác
        if (idleClip == null) idleClip = allAssets.OfType<AnimationClip>().FirstOrDefault(c => c.name.ToLower().Contains("idle"));
        if (walkClip == null) walkClip = allAssets.OfType<AnimationClip>().FirstOrDefault(c => c.name.ToLower().Contains("walk"));
        
        // Nếu vẫn thiếu Idle hoặc Walk, gán các clip Soldier/Soldier 1 còn lại làm fallback
        if (idleClip == null) idleClip = allAssets.OfType<AnimationClip>().FirstOrDefault(c => c.name == "Soldier 1");
        if (walkClip == null) walkClip = allAssets.OfType<AnimationClip>().FirstOrDefault(c => c.name == "Soldier");

        if (idleClip == null || walkClip == null || attackClip1 == null || attackClip2 == null || attackClip3 == null)
        {
            Debug.LogError("Không tìm thấy các Animation Clips gốc trong file Aseprite! Hãy chắc chắn bạn đã kéo thả các file .aseprite vào đúng vị trí và Unity đã import chúng.");
            return;
        }

        // ========================================================
        // 2. TẠO HOẶC CẤU HÌNH LẠI ANIMATOR CONTROLLER
        // ========================================================
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);
        }
        else
        {
            // Reset các State & Parameters cũ để thiết lập lại sạch sẽ
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.states = new ChildAnimatorState[0];
            rootStateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Tạo các Trạng thái (States) trong Animator
        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = walkClip;

        AnimatorState attackState1 = stateMachine.AddState("Attack1");
        attackState1.motion = attackClip1;

        AnimatorState attackState2 = stateMachine.AddState("Attack2");
        attackState2.motion = attackClip2;

        AnimatorState attackState3 = stateMachine.AddState("Attack3");
        attackState3.motion = attackClip3;

        // Thiết lập các chuyển cảnh thông thường (Idle <-> Walk)
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        // Nhấn Trigger chém từ AnyState
        AnimatorStateTransition toAttack1 = stateMachine.AddAnyStateTransition(attackState1);
        toAttack1.AddCondition(AnimatorConditionMode.If, 0f, "Attack1");
        toAttack1.hasExitTime = false;
        toAttack1.duration = 0f;

        AnimatorStateTransition toAttack2 = stateMachine.AddAnyStateTransition(attackState2);
        toAttack2.AddCondition(AnimatorConditionMode.If, 0f, "Attack2");
        toAttack2.hasExitTime = false;
        toAttack2.duration = 0f;

        AnimatorStateTransition toAttack3 = stateMachine.AddAnyStateTransition(attackState3);
        toAttack3.AddCondition(AnimatorConditionMode.If, 0f, "Attack3");
        toAttack3.hasExitTime = false;
        toAttack3.duration = 0f;

        // Quay lại từ các đòn chém về Idle
        AnimatorStateTransition attack1ToIdle = attackState1.AddTransition(idleState);
        attack1ToIdle.hasExitTime = true;
        attack1ToIdle.exitTime = 1.0f;
        attack1ToIdle.duration = 0f;

        AnimatorStateTransition attack2ToIdle = attackState2.AddTransition(idleState);
        attack2ToIdle.hasExitTime = true;
        attack2ToIdle.exitTime = 1.0f;
        attack2ToIdle.duration = 0f;

        AnimatorStateTransition attack3ToIdle = attackState3.AddTransition(idleState);
        attack3ToIdle.hasExitTime = true;
        attack3ToIdle.exitTime = 1.0f;
        attack3ToIdle.duration = 0f;

        stateMachine.defaultState = idleState;
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("Đã cấu hình Animator Controller với các clip Aseprite gốc tại: " + controllerPath);

        // ========================================================
        // 3. KHỞI TẠO GAMEOBJECT NHÂN VẬT TRONG SCENE
        // ========================================================
        GameObject playerObj = new GameObject("DarkFantasyPlayer");
        playerObj.tag = "Player"; // Đặt tag Player để các hệ thống khác (như CameraFollow) nhận diện

        // 4. Thiết lập Sprite Renderer mặc định từ Frame_0 của file Aseprite
        SpriteRenderer sr = playerObj.AddComponent<SpriteRenderer>();
        Sprite firstSprite = allAssets.OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault(s => s.name.Contains("Frame_0"));
        if (firstSprite == null)
        {
            firstSprite = allAssets.OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();
        }
        if (firstSprite != null)
        {
            sr.sprite = firstSprite;
        }

        // 5. Thiết lập Animator
        Animator anim = playerObj.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // 6. Thiết lập Rigidbody2D vật lý
        Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // ========================================================
        // 7. CĂN CHỈNH TỰ ĐỘNG COLLIDER & HITBOX THEO TRANFORM SPRITE BOUNDS
        // ========================================================
        // Tính toán vị trí tương đối thực sự của hình ảnh Sprite so với gốc GameObject
        Vector3 localSpriteCenter = playerObj.transform.InverseTransformPoint(sr.bounds.center);
        Vector3 spriteSize = sr.bounds.size;

        // Cài đặt CapsuleCollider2D ôm vừa khớp chân/thân hình ảnh nhân vật
        CapsuleCollider2D col = playerObj.AddComponent<CapsuleCollider2D>();
        if (savedColliderSize != Vector2.zero)
        {
            col.size = savedColliderSize;
            col.offset = savedColliderOffset;
        }
        else
        {
            col.size = new Vector2(spriteSize.x * 0.7f, spriteSize.y * 0.9f); // co giãn tỉ lệ phù hợp cơ thể
            col.offset = new Vector2(localSpriteCenter.x, localSpriteCenter.y);
        }

        // 8. Thêm script PlayerMovement điều khiển (Khôi phục từ giá trị đã sao lưu + tự động bù offset chém)
        PlayerMovement pm = playerObj.AddComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.moveSpeed = savedMoveSpeed;
            pm.movementSmoothing = savedSmoothing;
            pm.attackCooldown = savedAttackCooldown;
            pm.attackDuration = savedAttackDuration;
            pm.attackDelay = savedAttackDelay;
            pm.lungeForce = savedLungeForce;
            pm.comboResetWindow = savedComboWindow;
            pm.attackBoxSize = savedAttackBoxSize;
            pm.attackOffset = savedAttackOffset;
            pm.enemyLayer = savedEnemyLayer;
            pm.attackDamage = savedAttackDamage;
            
            // Gán độ lệch tâm của hình ảnh cho script chém
            pm.spriteOffset = new Vector2(localSpriteCenter.x, localSpriteCenter.y);
            Debug.Log("<color=green>Đã tự động căn chỉnh và đồng bộ Collider/Hitbox khớp theo vị trí hình ảnh nhân vật.</color>");
        }

        // 9. Đảm bảo trong Scene có Main Camera và gắn CameraFollow
        GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObj == null)
        {
            cameraObj = GameObject.Find("Main Camera");
        }
        if (cameraObj == null)
        {
            cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cameraComp = cameraObj.AddComponent<Camera>();
            cameraComp.orthographic = true;
            cameraComp.orthographicSize = 5f;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
        }

        // Gắn script CameraFollow cho Camera
        System.Type cameraFollowType = System.Type.GetType("DungeonCastle.CameraControl.CameraFollow, Assembly-CSharp");
        if (cameraFollowType != null)
        {
            Component cameraFollow = cameraObj.GetComponent(cameraFollowType);
            if (cameraFollow == null)
            {
                cameraFollow = cameraObj.AddComponent(cameraFollowType);
            }
            
            // Thiết lập mục tiêu theo dõi là nhân vật
            var targetField = cameraFollowType.GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetField != null)
            {
                targetField.SetValue(cameraFollow, playerObj.transform);
            }
        }

        // 10. Lưu đè thành Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(playerObj, prefabPath, InteractionMode.UserAction);
        
        playerObj.transform.position = Vector3.zero;
        Selection.activeGameObject = playerObj;
        
        // Dọn dẹp các tệp tin .anim trùng lặp tự tạo thừa thãi trước đây
        DeleteDuplicateAnimFiles(characterFolder);

        Debug.Log("Hoàn tất cài đặt nhân vật! Prefab đã lưu tại: " + prefabPath);
    }

    [MenuItem("Tools/Save Player Prefab Changes")]
    public static void SavePlayerPrefab()
    {
        GameObject playerObj = GameObject.Find("DarkFantasyPlayer");
        if (playerObj == null)
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (playerObj == null)
        {
            Debug.LogError("Không tìm thấy đối tượng Player trong Scene để lưu!");
            return;
        }

        string prefabPath = "Assets/tainguyen/player/PlayerPrefab.prefab";
        
        // Cập nhật đè dữ liệu đối tượng trong Scene lên file Prefab
        PrefabUtility.SaveAsPrefabAsset(playerObj, prefabPath);
        
        // Đánh dấu thay đổi và lưu Scene hiện tại
        EditorUtility.SetDirty(playerObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("<color=green>Đã lưu thành công mọi cấu hình chỉnh sửa của nhân vật vào Prefab và Scene!</color>");
    }

    private static void DeleteDuplicateAnimFiles(string characterFolder)
    {
        string[] filesToDelete = {
            characterFolder + "/Soldier_Idle.anim",
            characterFolder + "/Soldier_Walk.anim",
            characterFolder + "/Soldier_Attack.anim",
            characterFolder + "/Soldier_Attack1.anim",
            characterFolder + "/Soldier_Attack2.anim",
            characterFolder + "/Soldier_Attack3.anim",
            characterFolder + "/Soldier_Idle.anim.meta",
            characterFolder + "/Soldier_Walk.anim.meta",
            characterFolder + "/Soldier_Attack.anim.meta",
            characterFolder + "/Soldier_Attack1.anim.meta",
            characterFolder + "/Soldier_Attack2.anim.meta",
            characterFolder + "/Soldier_Attack3.anim.meta"
        };

        foreach (string file in filesToDelete)
        {
            if (File.Exists(file))
            {
                AssetDatabase.DeleteAsset(file);
            }
        }
        AssetDatabase.Refresh();
    }

    private static AnimationClip CreateClip(string path, string spriteSheetPath, bool loop, float frameRate)
    {
        return null;
    }
}
