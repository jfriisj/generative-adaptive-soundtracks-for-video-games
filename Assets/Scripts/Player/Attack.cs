using System.Collections;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject meleeCircle;
    public string weapon;
    [SerializeField] private LayerMask enemyMask;
    public bool cooldown;


    public int weaponDamage;
    public float weaponSpeed;
    public float bulletSpeed;

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetButtonDown("Fire1") && !cooldown)
        {
            switch (weapon)
            {
                case "Range":
                    RangeAttack();
                    // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
                    // RuntimeManager.PlayOneShot("event:/sfx/player/shoot");
                    break;
                case "Melee":
                    MeleeAttack();
                    // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
                    // RuntimeManager.PlayOneShot("event:/sfx/player/stab");
                    break;
            }

            StartCoroutine(weaponCooldown());
        }
    }

    private void RangeAttack()
    {
        var bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
        bullet.tag = "PlayerBullet";
        var b = bullet.GetComponent<BulletScript>();
        b.damage = weaponDamage;
        b.player = true;
        b.Speed = 20f;
        b.despawnTime = 3;
    }

    private void MeleeAttack()
    {
        var enemyHit = Physics2D.OverlapCircleAll(meleeCircle.transform.position, 5, enemyMask);

        foreach (var enemy in enemyHit)
            if (enemy.CompareTag("Enemy"))
                enemy.GetComponent<EnemyStats>().TakeDamage(weaponDamage);
    }

    private IEnumerator weaponCooldown()
    {
        cooldown = true;
        yield return new WaitForSeconds(weaponSpeed);
        cooldown = false;
    }
}