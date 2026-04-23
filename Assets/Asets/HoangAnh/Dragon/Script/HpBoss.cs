using Fusion;
using UnityEngine;

public class HpBoss : NetworkBehaviour
{
    [Header("Health")]
    [Networked] public int CurrentHP { get; set; }
    [SerializeField] public int MaxHP = 10;
    [SerializeField] NetworkBehaviour Run;
    [SerializeField] NetworkBehaviour tanCong;
    [Networked] private TickTimer DeathTimer { get; set; }
    Animator ani;
    public override void Spawned()
    {
        ani = GetComponentInParent<Animator>();
        base.Spawned();
        if (Object.HasStateAuthority)
        {
            CurrentHP = MaxHP;
        }
    }

    public void InflictDamage(int damage, GameObject source)
    {
        if (!Object.HasStateAuthority) return;

        // Nếu đã chết rồi thì khỏi trừ nữa
        if (DeathTimer.IsRunning) return;

        CurrentHP -= damage;

        if (CurrentHP <= 0)
        {
            StartDeathCountdown();
        }
    }

    private void StartDeathCountdown()
    {
        Run.enabled = false;
        tanCong.enabled = false;
        ani.SetTrigger("Die");
        // Set timer 3 giây
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
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}