using UnityEngine;
using Fusion;

public class BossAttack : NetworkBehaviour
{
    [Header("Cấu hình tấn công")]
    [SerializeField] private float thoiGianGiuaMoiPhatVả = 3f; // Bao lâu vả 1 phát
    [Tooltip("Thời gian con Boss đứng yên múa skill (Khớp với độ dài của Animation Attack)")]
    [SerializeField] private float thoiGianAnimationAttack = 1.5f;

    [Header("Script cần tắt khi đánh")]
    [SerializeField] NetworkBehaviour Run;
    private Animator ani;

    // Bộ đếm thời gian chuẩn mạng của Fusion
    [Networked] private TickTimer timerAttack { get; set; }
    [Networked] private TickTimer timerDangDanh { get; set; } // Đếm thời gian đang đứng múa skill

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

        // ========================================================
        // 1. XỬ LÝ BẬT/TẮT SCRIPT DI CHUYỂN
        // ========================================================
        if (timerDangDanh.IsRunning)
        {
            // Đang múa skill -> TẮT script Run
            if (Run != null && Run.enabled)
            {
                Run.enabled = false;
            }
        }
        else
        {
            // Đã múa xong (hoặc đang đi bình thường) -> BẬT lại script Run
            if (Run != null && !Run.enabled)
            {
                Run.enabled = true;
            }
        }

        // ========================================================
        // 2. LOGIC TẤN CÔNG
        // ========================================================
        // Nếu bộ đếm thời gian đã chạy xong (đủ 3 giây) và không bị kẹt đánh
        if (timerAttack.Expired(Runner))
        {
            // Chạy Animation Tấn Công
            if (ani != null)
            {
                ani.SetTrigger("Attack");
            }

            // Khóa chân Boss (tắt Run) trong đúng khoảng thời gian múa skill
            timerDangDanh = TickTimer.CreateFromSeconds(Runner, thoiGianAnimationAttack);

            // Reset lại bộ đếm để 3 giây sau đánh tiếp
            timerAttack = TickTimer.CreateFromSeconds(Runner, thoiGianGiuaMoiPhatVả);

            Debug.Log("⚔️ Boss đang vả! Tạm thời tắt script Run.");
        }
    }
}