using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong
{
    /// <summary>Một lựa chọn sức mạnh sau mỗi đợt giặc.</summary>
    public class Upgrade
    {
        public string Name;
        public string Description;
        public System.Action<Player> Apply;
    }

    /// <summary>Kho nâng cấp: mỗi lần thắng một đợt, chọn ngẫu nhiên 3 nâng cấp khác nhau để người chơi chọn 1.</summary>
    public static class UpgradeSystem
    {
        static readonly List<Upgrade> Pool = new()
        {
            new() { Name = "Roi sắt nặng", Description = "+25% sát thương roi sắt", Apply = p => p.Damage *= 1.25f },
            new() { Name = "Áo giáp sắt", Description = "+30 máu tối đa và hồi 30 máu", Apply = p => p.AddMaxHP(30f) },
            new() { Name = "Ngựa sắt phi nhanh", Description = "+12% tốc độ di chuyển", Apply = p => p.MoveSpeed *= 1.12f },
            new() { Name = "Đòn liên hoàn", Description = "Vung roi nhanh hơn 15%", Apply = p => p.AttackCooldown *= 0.85f },
            new() { Name = "Hơi thở lửa", Description = "Phun lửa tốn ít Lửa hơn 25%, mạnh hơn 20%",
                    Apply = p => { p.FireCost *= 0.75f; p.FireDamageMul *= 1.2f; } },
            new() { Name = "Cơm cà làng Gióng", Description = "Dân làng góp gạo: hồi đầy máu", Apply = p => p.FullHeal() },
            new() { Name = "Hút sinh lực", Description = "Mỗi đòn trúng hồi 2 máu", Apply = p => p.Lifesteal += 2f },
            new() { Name = "Tầm roi xa", Description = "+20% tầm đánh", Apply = p => p.AttackRange *= 1.2f },
            new() { Name = "Lướt như gió", Description = "Hồi chiêu lướt nhanh hơn 30%", Apply = p => p.DashCooldown *= 0.7f },
            new() { Name = "Mắt tinh", Description = "+10% tỉ lệ chí mạng", Apply = p => p.CritChance += 0.1f },
        };

        public static List<Upgrade> Roll(int count)
        {
            var copy = new List<Upgrade>(Pool);
            var result = new List<Upgrade>();
            for (int i = 0; i < count && copy.Count > 0; i++)
            {
                int idx = Random.Range(0, copy.Count);
                result.Add(copy[idx]);
                copy.RemoveAt(idx);
            }
            return result;
        }
    }
}
