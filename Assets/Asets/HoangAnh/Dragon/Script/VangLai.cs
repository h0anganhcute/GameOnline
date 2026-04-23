using Fusion;
using UnityEngine;

public class VangLai : NetworkBehaviour
{
    private BulletFusion bulletScript;

    public override void Spawned()
    {
        // Lấy script BulletFusion nằm cùng trên Prefab này
        bulletScript = GetComponent<BulletFusion>();
    }

    // Sử dụng OnTriggerEnter vì viên đạn thường để IsTrigger
    private void OnTriggerEnter(Collider other)
    {
        // Chỉ Host (StateAuthority) mới có quyền xử lý logic thay đổi hướng để đồng bộ
        if (!Object.HasStateAuthority) return;

        // Kiểm tra xem vật chạm vào có Tag là HitBox hay không
        if (other.CompareTag("HitBox"))
        {
            if (bulletScript != null)
            {
                // Lấy hướng hiện tại từ script đạn
                // Chúng ta cần một cách để lấy/ghi hướng bay từ BulletFusion
                // Ở đây tôi giả định bạn sẽ dùng hàm Init để ghi đè lại hướng

                // Tính toán hướng ngược lại (180 độ)
                // Lưu ý: bulletScript.huongBay cần được để public hoặc có Property truy cập
                // Nếu hướng bay đang là [Networked], ta gọi lại Init với hướng đảo ngược

                // Cách 1: Phản xạ gương (nếu muốn nảy chân thực theo bề mặt)
                // Vector3 huongPhanXha = Vector3.Reflect(transform.forward, other.transform.forward);

                // Cách 2: Văng ngược 100% hướng vừa bay tới
                Vector3 huongHienTai = transform.forward; // Hoặc lấy từ biến networked hướng bay
                Vector3 huongNguocLai = -huongHienTai;

                // Cập nhật lại hướng bay cho viên đạn
                bulletScript.Init(huongNguocLai);

                // Xoay Transform của viên đạn nhìn về hướng mới để hình ảnh không bị ngược
                transform.rotation = Quaternion.LookRotation(huongNguocLai);

                Debug.Log("🛡️ Đạn đã chạm HitBox và văng ngược lại!");
            }
        }
    }
}