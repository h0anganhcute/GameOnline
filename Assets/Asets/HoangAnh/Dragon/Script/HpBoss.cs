using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class CoChe2 : NetworkBehaviour
{
    [SerializeField] NetworkBehaviour Run;

    [Header("Chuyển Phase (Dưới 50 máu)")]
    [Networked] private NetworkBool isPhase2 { get; set; }
    private Transform viTriBossTam;
    private NavMeshAgent agent;

    [Header("Điểm tập kết Boss")]
    [SerializeField] NetworkObject ViTriNe;

    [Header("Cơ chế Bắn Đạn (Prefab)")]
    [SerializeField] private NetworkPrefabRef vienDanPrefab; // Kéo Prefab cục lửa vào đây
    [Tooltip("Chỉnh tọa độ X Y Z để dời vị trí bắn ra khỏi miệng con rồng")]
    [SerializeField] private Vector3 offsetViTriBan = new Vector3(0, 2f, 3f);
    [Tooltip("Thời gian nghỉ giữa mỗi lần khạc đạn (giây)")]
    [SerializeField] private float thoiGianHoiChieuBan = 2f;
    [Networked] private TickTimer timerBan { get; set; } // Bộ đếm giờ của Fusion

    private DamageableFusion bossHP;
    Animator ani;
    [Networked] private NetworkBool daCatCanh { get; set; }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        bossHP = GetComponentInChildren<DamageableFusion>();
        ani = GetComponent<Animator>();

        if (Object.HasStateAuthority)
        {
            isPhase2 = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (bossHP == null) return;
        if (bossHP.CurrentHP <= 0) return;

        // ============================================
        // ĐỌC TRỘM MÁU: NẾU <= 50 THÌ CHUYỂN PHASE
        // ============================================
        if (bossHP.CurrentHP <= 50 && !isPhase2)
        {
            BatDauChuyenPhase();
        }

        // ============================================
        // LOGIC DI CHUYỂN PHASE 2 VÀ XOAY MẶT
        // ============================================
        if (isPhase2)
        {
            if (ani != null)
            {
                AnimatorStateInfo stateInfo = ani.GetCurrentAnimatorStateInfo(0);

                if ((stateInfo.IsName("phunLua") && stateInfo.normalizedTime < 1.0f) || ani.IsInTransition(0))
                {
                    if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                    return;
                }
            }

            if (viTriBossTam != null && agent != null)
            {
                if (!agent.isOnNavMesh) return;

                if (Vector3.Distance(agent.transform.position, viTriBossTam.position) <= 1.5f)
                {
                    agent.isStopped = true;
                    XoayMatVePlayer();
                }
                else
                {
                    agent.isStopped = false;
                    agent.updateRotation = true;
                    agent.SetDestination(viTriBossTam.position);
                }
            }
            else if (viTriBossTam == null)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                XoayMatVePlayer();
            }
        }
    }

    private void BatDauChuyenPhase()
    {
        isPhase2 = true;

        if (Run != null) Run.enabled = false;

        if (ViTriNe != null)
        {
            ViTriNe.gameObject.SetActive(true);
            viTriBossTam = ViTriNe.transform;
            Debug.Log("⚠️ Boss tụt máu! Đã BẬT điểm tập kết và đang chạy về đó.");
        }
        else
        {
            Debug.LogError("❌ Ê mày quên kéo cục ViTriBoss vào ô Vi Tri Ne ngoài Inspector kìa!");
        }
    }

    private void XoayMatVePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && agent != null)
        {
            if (!daCatCanh)
            {
                ani.SetTrigger("Bay");
                daCatCanh = true;

                // Khởi tạo đếm ngược mốc thời gian bắn phát đầu tiên sau khi cất cánh
                timerBan = TickTimer.CreateFromSeconds(Runner, 1.5f);
                Debug.Log("🐉 Boss bắt đầu cất cánh!");
            }

            if (daCatCanh && agent.baseOffset < 5f)
            {
                agent.baseOffset = Mathf.Lerp(agent.baseOffset, 5f, 2f * Runner.DeltaTime);
            }

            Transform thangGoc = agent.transform;
            Vector3 huongNhin = player.transform.position - thangGoc.position;
            huongNhin.y = 0;

            if (huongNhin != Vector3.zero)
            {
                agent.updateRotation = false;
                Quaternion gocQuay = Quaternion.LookRotation(huongNhin);
                thangGoc.rotation = Quaternion.Slerp(thangGoc.rotation, gocQuay, 10f * Runner.DeltaTime);
            }

            // ==========================================
            // LOGIC BẮN ĐẠN KHI ĐÃ ĐỐI MẶT PLAYER
            // ==========================================
            if (timerBan.Expired(Runner))
            {
                ThucHienBan(player);
            }
        }
    }

    private void ThucHienBan(GameObject player)
    {
        if (vienDanPrefab == NetworkPrefabRef.Empty) return;

        // Tính vị trí đẻ viên đạn xoay theo hướng cái đầu của Boss
        Transform thangGoc = agent.transform;
        Vector3 diemBan = thangGoc.position + (thangGoc.rotation * offsetViTriBan);

        // Tính hướng bay: Từ điểm bắn chỉa thẳng vô Player
        Vector3 huongBay = (player.transform.position - diemBan).normalized;
        Quaternion gocBan = Quaternion.LookRotation(huongBay);

        // Ép Boss chạy Animation khạc lửa
        if (ani != null) ani.SetTrigger("phunLua");

        // Đẻ viên đạn đồng bộ qua mạng
        Runner.Spawn(vienDanPrefab, diemBan, gocBan, Object.InputAuthority);

        // Reset lại đồng hồ đếm giờ cho phát bắn tiếp theo
        timerBan = TickTimer.CreateFromSeconds(Runner, thoiGianHoiChieuBan);
        Debug.Log("🔥 Boss khạc lửa!");
    }

    // ==========================================
    // VẼ VÒNG TRÒN ĐỎ NGOÀI EDITOR (KHÔNG HIỆN TRONG GAME)
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        // Dùng parent để lúc chưa play game nó vẫn lấy đúng cục gốc
        Transform thangGoc = transform.parent != null ? transform.parent : transform;

        // Tính vị trí điểm màu đỏ dựa vào Offset mày nhập
        Vector3 diemBan = thangGoc.position + (thangGoc.rotation * offsetViTriBan);

        Gizmos.color = Color.red;
        // Vẽ cái vòng tròn lưới bự 0.5f cho dễ nhìn
        Gizmos.DrawWireSphere(diemBan, 0.5f);
    }
}