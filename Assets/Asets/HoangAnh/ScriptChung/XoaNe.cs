using Fusion;
using UnityEngine;

public class XoaNe : NetworkBehaviour
{
    // Cờ đánh dấu để không bị đếm thời gian lại nhiều lần nếu chạm liên tục
    private bool daChamBoss = false;
    // Bộ đếm thời gian mạng chuẩn Fusion
    [Networked] private TickTimer xoaTimer { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        // Phải có quyền Host/Server mới được ra lệnh xóa
        if (Object != null && !Object.HasStateAuthority) return;

        // Nếu đã chạm rồi thì bỏ qua, không kích hoạt lại
        if (daChamBoss) return;

        // Kiểm tra xem thằng chạm vào có phải là Boss không
        if (other.CompareTag("Boss"))
        {
            daChamBoss = true;

            // Hẹn giờ đúng 1 giây sau sẽ xóa
            xoaTimer = TickTimer.CreateFromSeconds(Runner, 0f);

            Debug.Log("🎯 Boss đã chạm điểm tập kết! Chuẩn bị xóa điểm này sau 1 giây...");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object != null && !Object.HasStateAuthority) return;

        // Nếu đã chạm Boss và đồng hồ 1 giây đã chạy xong
        if (daChamBoss && xoaTimer.Expired(Runner))
        {
            // Hủy object qua mạng Fusion
            if (Object != null && Object.IsValid)
            {
                Runner.Despawn(Object);
            }
            else
            {
                // Phòng hờ lỡ mày quên gắn NetworkObject cho cục ViTriBoss
                Destroy(gameObject);
            }
        }
    }
}