using Fusion;
using UnityEngine;

public class DamageableFusion : NetworkBehaviour
{
    [Header("Health")]
    [Networked] public int CurrentHP { get; set; }
    [SerializeField] public int MaxHP = 100;

    [Header("References")]
    [SerializeField] NetworkBehaviour Run;
    [SerializeField] NetworkBehaviour tanCong;

    // Lưu lại NetworkObject của cha để xóa cho đúng
    private NetworkObject parentNetworkObject;

    [Networked] private TickTimer DeathTimer { get; set; }
    Animator ani;

    public override void Spawned()
    {
        // Lấy Animator và các component điều khiển ở cha
        ani = GetComponentInParent<Animator>();

        // Tìm NetworkObject ở cha
        parentNetworkObject = GetComponentInParent<NetworkObject>();

        base.Spawned();

        if (Object.HasStateAuthority)
        {
            CurrentHP = MaxHP;
        }
    }

    public void InflictDamage(int damage, GameObject source)
    {
        if (!Object.HasStateAuthority) return;
        if (DeathTimer.IsRunning) return;

        CurrentHP -= damage;

        if (CurrentHP <= 0)
        {
            StartDeathCountdown();
        }
    }

    private void StartDeathCountdown()
    {
        // Tắt các script di chuyển/tấn công (nếu có tham chiếu)
        if (Run != null) Run.enabled = false;
        if (tanCong != null) tanCong.enabled = false;

        if (ani != null) ani.SetTrigger("Die");

        // Bắt đầu đếm ngược 3 giây trước khi biến mất hoàn toàn
        DeathTimer = TickTimer.CreateFromSeconds(Runner, 3f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (DeathTimer.Expired(Runner))
        {
            Die();
        }
    }

    private void Die()
    {
        if (Runner != null)
        {
            // Nếu tìm thấy NetworkObject cha thì xóa cha, không thì xóa chính mình
            if (parentNetworkObject != null)
            {
                Runner.Despawn(parentNetworkObject);
            }
            else
            {
                Runner.Despawn(Object);
            }
        }
        else
        {
            // Trường hợp fallback nếu không phải network object (hiếm khi xảy ra với Fusion)
            Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
        }
    }
}