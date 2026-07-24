using UnityEngine;

namespace DungeonCastle.CameraControl
{
    [ExecuteAlways]
    public class CameraFollow : MonoBehaviour
    {
        [Header("=== Cấu hình Target ===")]
        [Tooltip("Transform của nhân vật cần đi theo. Nếu trống sẽ tự tìm đối tượng có Tag là Player")]
        [SerializeField] private Transform target;

        [Header("=== Cài đặt Di chuyển ===")]
        [Tooltip("Bật chế độ di chuyển mượt mà (Lerp). Nếu tắt, camera sẽ bám khít 100% không trễ.")]
        [SerializeField] private bool smoothFollow = false;
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

        private void Start()
        {
            // Tự động tìm Player nếu chưa được gán trong Inspector
            if (target == null)
            {
                FindPlayerTarget();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    FindPlayerTarget();
                }
                #endif
                if (target == null) return;
            }

            // Lấy vị trí gốc của Target
            Vector3 targetPos = target.position;

            // Nếu đối tượng có script PlayerMovement, cộng thêm độ lệch tâm hình ảnh để căn giữa camera
            PlayerMovement pm = target.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                targetPos += (Vector3)pm.spriteOffset;
            }

            // Tính toán vị trí mong muốn của camera dựa trên vị trí đã bù offset
            Vector3 desiredPosition = targetPos + offset;

            // Nếu kích hoạt giới hạn bản đồ, giới hạn vị trí X và Y của Camera
            if (useLimits)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            // Chỉ sử dụng mượt mà (Lerp) khi thực sự đang Play game
            if (smoothFollow && Application.isPlaying)
            {
                // Di chuyển camera mượt mà bằng Vector3.Lerp theo thời gian thực (smoothSpeed * Time.deltaTime)
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
                transform.position = smoothedPosition;
            }
            else
            {
                // Bám khít 100% không có độ trễ (luôn snap trong Editor Edit Mode)
                transform.position = desiredPosition;
            }
        }

        /// <summary>
        /// Tìm đối tượng Player trong màn chơi
        /// </summary>
        public void FindPlayerTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
}
