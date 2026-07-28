using UnityEngine;

namespace DungeonCastle.CameraControl
{
    /// <summary>
    /// Camera tách biệt khỏi Player. Tự tìm Player, tự theo dõi, tự sống sót qua các Scene.
    /// KHÔNG cần gắn làm con của Player.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("=== Cấu hình Target ===")]
        [Tooltip("Transform của nhân vật cần đi theo. Nếu trống sẽ tự tìm đối tượng có Tag là Player")]
        [SerializeField] private Transform target;

        [Header("=== Cài đặt Di chuyển ===")]
        [Tooltip("Bật chế độ di chuyển mượt mà (Lerp). Nếu tắt, camera sẽ bám khít 100% không trễ.")]
        [SerializeField] private bool smoothFollow = true;
        [Tooltip("Tốc độ bám theo (chỉ có tác dụng khi bật Smooth Follow)")]
        [SerializeField] private float smoothSpeed = 15f;
        [Tooltip("Khoảng cách lệch giữa camera và nhân vật (Z thường là -10 cho game 2D)")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("=== Giới hạn Bản đồ (Tùy chọn) ===")]
        [SerializeField] private bool useLimits = false;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float minY = -10f;
        [SerializeField] private float maxY = 10f;

        private static CameraFollow _instance;

        private void Awake()
        {
            // Singleton: đảm bảo chỉ có 1 Camera duy nhất tồn tại
            if (_instance != null && _instance != this)
            {
                // Nếu đã có Camera khác đang hoạt động, xóa Camera mới
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Tách Camera ra khỏi Player nếu nó đang là con của Player
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Khi chuyển Scene, tìm lại Player
            FindPlayerTarget();

            // Xóa Camera trùng lặp trong scene mới (nếu có)
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Camera myCam = GetComponent<Camera>();
            foreach (Camera cam in allCameras)
            {
                if (cam != null && cam != myCam && cam.gameObject != gameObject)
                {
                    // Xóa AudioListener trùng lặp trước
                    AudioListener extraListener = cam.GetComponent<AudioListener>();
                    if (extraListener != null) Destroy(extraListener);
                    // Xóa camera trùng lặp
                    Destroy(cam.gameObject);
                    Debug.Log($"<color=cyan>[CameraFollow]</color> Đã xóa Camera trùng lặp: <b>{cam.gameObject.name}</b>");
                }
            }

            // Snap camera ngay tại vị trí Player (không delay)
            if (target != null)
            {
                Vector3 targetPos = target.position;
                PlayerMovement pm = target.GetComponent<PlayerMovement>();
                if (pm != null) targetPos += (Vector3)pm.spriteOffset;
                transform.position = targetPos + offset;
            }
        }

        private void Start()
        {
            FindPlayerTarget();
            
            // Đảm bảo Camera có đủ component cần thiết
            Camera cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 4f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.18f, 0.22f, 0.15f, 1f);
            }
            
            if (GetComponent<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        private void LateUpdate()
        {
            // Nếu target chưa được gán HOẶC target không phải Player thật, tìm lại
            if (target == null || (target.GetComponent<PlayerMovement>() == null && target.GetComponent<PlayerStats>() == null))
            {
                FindPlayerTarget();
                if (target == null) return;
            }

            // Lấy vị trí gốc của Target Player
            Vector3 targetPos = target.position;

            // Nếu đối tượng có script PlayerMovement, cộng thêm độ lệch tâm hình ảnh
            PlayerMovement pm = target.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                targetPos += (Vector3)pm.spriteOffset;
            }

            // Tính toán vị trí mong muốn
            Vector3 desiredPosition = targetPos + offset;

            // Giới hạn bản đồ nếu kích hoạt
            if (useLimits)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            // Di chuyển camera
            if (smoothFollow && Application.isPlaying)
            {
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
                transform.position = smoothedPosition;
            }
            else
            {
                transform.position = desiredPosition;
            }
        }

        /// <summary>
        /// Tìm chính xác đối tượng Player (Không bao giờ nhầm sang Quái)
        /// </summary>
        public void FindPlayerTarget()
        {
            // 1. Ưu tiên tìm theo Singleton PlayerStats.Instance
            if (PlayerStats.Instance != null && PlayerStats.Instance.gameObject.activeInHierarchy)
            {
                target = PlayerStats.Instance.transform;
                return;
            }

            // 2. Tìm theo PlayerMovement component
#if UNITY_2021_3_OR_NEWER
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
#else
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
#endif
            if (pm != null && pm.gameObject.activeInHierarchy)
            {
                target = pm.transform;
                return;
            }

            // 3. Dự phòng: Tìm theo tag "Player"
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }
    }
}
