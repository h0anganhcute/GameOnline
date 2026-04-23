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
    [SerializeField] private NetworkPrefabRef vienDanPrefab;
    [Tooltip("Chỉnh tọa độ X Y Z để dời vị trí bắn ra khỏi miệng con rồng")]
    [SerializeField] private Vector3 offsetViTriBan = new Vector3(0, 2f, 3f);
    [Tooltip("Thời gian nghỉ giữa mỗi lần khạc đạn (giây)")]
    [SerializeField] private float thoiGianHoiChieuBan = 2f;
    [Networked] private TickTimer timerBan { get; set; }

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
        if (bossHP == null || bossHP.CurrentHP <= 0) return;

        if (bossHP.CurrentHP <= 50 && !isPhase2)
        {
            BatDauChuyenPhase();
        }

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
            else
            {
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
                timerBan = TickTimer.CreateFromSeconds(Runner, 1.5f);
            }

            if (daCatCanh && agent.baseOffset < 5f)
            {
                agent.baseOffset = Mathf.Lerp(agent.baseOffset, 5f, 2f * Runner.DeltaTime);
            }

            Vector3 huongNhin = player.transform.position - transform.position;
            huongNhin.y = 0;

            if (huongNhin != Vector3.zero)
            {
                agent.updateRotation = false;
                Quaternion gocQuay = Quaternion.LookRotation(huongNhin);
                transform.rotation = Quaternion.Slerp(transform.rotation, gocQuay, 10f * Runner.DeltaTime);
            }

            if (timerBan.Expired(Runner))
            {
                ThucHienBan(player);
            }
        }
    }

    private void ThucHienBan(GameObject player)
    {
        if (vienDanPrefab == NetworkPrefabRef.Empty) return;

        // Tính vị trí bắn dựa trên Offset
        Vector3 diemBan = transform.position + (transform.rotation * offsetViTriBan);
        Vector3 huongBay = (player.transform.position - diemBan).normalized;

        // Spawn đạn qua mạng
        NetworkObject dan = Runner.Spawn(
            vienDanPrefab,
            diemBan,
            Quaternion.LookRotation(huongBay),
            Object.InputAuthority
        );

        if (dan != null)
        {
            // 🔥 ĐOẠN QUAN TRỌNG: Thay vì AddForce, gọi Init của BulletFusion
            BulletFusion bulletScript = dan.GetComponent<BulletFusion>();
            if (bulletScript != null)
            {
                if (ani != null) ani.SetTrigger("phunLua");
                bulletScript.Init(huongBay); // Viên đạn sẽ tự bay theo hướng này
            }
        }

        timerBan = TickTimer.CreateFromSeconds(Runner, thoiGianHoiChieuBan);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 diemBan = transform.position + (transform.rotation * offsetViTriBan);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(diemBan, 0.5f);
    }
}