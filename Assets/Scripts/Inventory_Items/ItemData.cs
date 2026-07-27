using UnityEngine;

public enum ItemType
{
    Weapon,
    Consumable,
    Equipment,
    Misc
}

/// <summary>
/// ScriptableObject định nghĩa dữ liệu Vật Phẩm trong Game (Rìu Lửa, Máu, Trang bị...)
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("=== THÔNG TIN VẬT PHẨM ===")]
    public string id = "riu_lua";
    public string itemName = "Rìu Lửa";
    public Sprite icon;
    public Sprite infoCardSprite;
    public ItemType itemType = ItemType.Weapon;
    
    [TextArea(2, 5)]
    public string description = "Rìu Lửa thần thoại thiêu rọi bóng tối, gây 35 sát thương khi vung chém!";

    [Header("=== KỸ NĂNG ĐẶC BIỆT (SKILL) ===")]
    public string skillName = "Bão Lửa Thiêu Rụi";
    [TextArea(2, 4)]
    public string skillDescription = "Nhấn Q hoặc Chuột Phải để kích hoạt Bão Lửa quét 65 sát thương!";
    public float skillCooldown = 3.0f;

    [Header("=== THUỘC TÍNH VŨ KHÍ / CHIẾN ĐẤU ===")]
    public int damage = 35;
    public float attackSpeed = 0.35f;
    public float attackRange = 1.3f;
    public int healAmount = 0;
}
