using UnityEngine;
using Fusion;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SoiAiI : NetworkBehaviour
{
    [Header("Cấu hình Vùng Tuần Tra")]
    [Tooltip("Kích thước hình chữ nhật (Trục X và Z)")]
    [SerializeField] private Vector2 kichThuocVung = new Vector2(10f, 10f);
    [Tooltip("Thời gian đứng nghỉ rình rập trước khi đi tiếp")]
    [SerializeField] private float thoiGianNghi = 2f;

    [Header("Tốc Độ Tuần Tra")]
    public float tocDoTuanTra = 2.5f;

    [Header("Animation")]
    [SerializeField] private NetworkMecanimAnimator netAnim;

    private NavMeshAgent agent;
    private Vector3 tamVungTuanTra;

    private TickTimer timerNghi;
    private bool dangNghi = false;

    Animator ani;

    public override void Spawned()
    {
        ani = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        tamVungTuanTra = transform.position;

        if (HasStateAuthority)
        {
            TimDiemTuanTraMoi();
        }
    }

    // Mỗi khi script này được Bật lại (OnEnable) bởi AttackSoi, nó sẽ ép lại tốc độ đi bộ
    //private void OnEnable()
    //{
    //    if (agent != null)
    //    {
    //        agent.speed = tocDoTuanTra;
    //        dangNghi = false;
    //        TimDiemTuanTraMoi();
    //    }
    //}

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || agent == null) return;

        // Ép liên tục tốc độ tuần tra phòng hờ rớt nhịp mạng
        agent.speed = tocDoTuanTra;

        if (dangNghi)
        {
            if (timerNghi.Expired(Runner))
            {
                dangNghi = false;
                ani.SetBool("Run", true);
                TimDiemTuanTraMoi();
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    dangNghi = true;
                    timerNghi = TickTimer.CreateFromSeconds(Runner, thoiGianNghi);
                    ani.SetBool("Run", false);
                }
            }
        }
    }

    void TimDiemTuanTraMoi()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        for (int i = 0; i < 5; i++)
        {
            float randomX = Random.Range(-kichThuocVung.x / 2f, kichThuocVung.x / 2f);
            float randomZ = Random.Range(-kichThuocVung.y / 2f, kichThuocVung.y / 2f);

            Vector3 diemRandom = tamVungTuanTra + new Vector3(randomX, 0, randomZ);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(diemRandom, out hit, 2f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);

                return;
            }
        }
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? tamVungTuanTra : transform.position;
        Gizmos.DrawWireCube(center, new Vector3(kichThuocVung.x, 0.5f, kichThuocVung.y));
    }
}