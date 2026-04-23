using UnityEngine;
using Fusion;

public class BossAttack : NetworkBehaviour
{
    [Header("Cấu hình tấn công")]
    [SerializeField] private float thoiGianGiuaMoiPhatVả = 3f; // 3 giây

    private Animator ani;

    // Bộ đếm thời gian chuẩn mạng của Fusion
    [Networked] private TickTimer timerAttack { get; set; }

    public override void Spawned()
    {
        // Lấy Animator gắn trên con Boss
        ani = GetComponent<Animator>();

        // Khởi tạo phát chém đầu tiên sau 3 giây
        if (HasStateAuthority)
        {
            timerAttack = TickTimer.CreateFromSeconds(Runner, thoiGianGiuaMoiPhatVả);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ thằng nắm quyền (Host/Server) mới được quyết định khi nào Boss đánh
        if (!HasStateAuthority) return;

        // Nếu bộ đếm thời gian đã chạy xong (đủ 3 giây)
        if (timerAttack.Expired(Runner))
        {
            // 1. Chạy Animation Tấn Công
            if (ani != null)
            {
                ani.SetTrigger("Attack");
            }

            // 2. Reset lại bộ đếm để 3 giây sau đánh tiếp
            timerAttack = TickTimer.CreateFromSeconds(Runner, thoiGianGiuaMoiPhatVả);

            Debug.Log("⚔️ Boss tự động vả phát tiếp theo!");
        }
    }
}