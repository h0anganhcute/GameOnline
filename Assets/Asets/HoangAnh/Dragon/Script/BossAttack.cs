using UnityEngine;
using Fusion;

public class BossAttack : NetworkBehaviour
{
    [Header("Cấu hình tấn công")]
    [SerializeField] private float thoiGianGiuaMoiPhatVả = 3f;
    [SerializeField] private float thoiGianAnimationAttack = 1.5f;

    [Header("Script cần tắt khi đánh")]
    [SerializeField] NetworkBehaviour Run;
    private Animator ani;

    // Các bộ đếm và Cờ (Flag)
    [Networked] private TickTimer timerAttack { get; set; }
    [Networked] private TickTimer timerDangDanh { get; set; }

    // ĐẶT CỜ Ở ĐÂY
    [Networked] private NetworkBool dangMuaSkill { get; set; }

    public override void Spawned()
    {
        ani = GetComponent<Animator>();

        if (HasStateAuthority)
        {
            timerAttack = TickTimer.CreateFromSeconds(Runner, thoiGianGiuaMoiPhatVả);
            dangMuaSkill = false; // Mới đẻ ra thì cờ tắt
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // LUỒNG 1: NẾU CỜ ĐANG BẬT (Đang đứng múa skill)
        if (dangMuaSkill)
        {
            // Chỉ chờ đếm giờ múa xong
            if (timerDangDanh.Expired(Runner))
            {
                // Đã múa xong -> Tắt cờ, bật lại Script đi dạo
                dangMuaSkill = false;
                if (Run != null) Run.enabled = true;

                // Múa xong mới bắt đầu đếm giờ cho cú vả tiếp theo (tùy mày muốn đếm lúc bắt đầu hay lúc kết thúc)
                timerAttack = TickTimer.CreateFromSeconds(Runner, thoiGianGiuaMoiPhatVả);
            }
        }
        // LUỒNG 2: NẾU CỜ ĐANG TẮT (Đi dạo bình thường)
        else
        {
            // Chờ đếm giờ tới phát đánh tiếp theo
            if (timerAttack.Expired(Runner))
            {
                if (ani != null) ani.SetTrigger("Attack");

                // BẬT CỜ LÊN và thiết lập thời gian múa skill
                dangMuaSkill = true;
                timerDangDanh = TickTimer.CreateFromSeconds(Runner, thoiGianAnimationAttack);

                // Tắt script di chuyển (Chỉ gọi ĐÚNG 1 LẦN nhờ có cờ bọc lại)
                if (Run != null) Run.enabled = false;

                Debug.Log("⚔️ Boss bắt đầu vả! Đã đặt cờ khóa chân.");
            }
        }
    }
}