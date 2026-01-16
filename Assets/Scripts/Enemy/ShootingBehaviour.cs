using System.Collections;
using UnityEngine;

public class ShootingBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject gameManager;
    [SerializeField] private int dmg;
    [SerializeField] private GameObject Bullet;
    [SerializeField] private float bulletSpeed;
    private bool isShooting;

    private bool lookAtPlayer;
    private GameObject player;
    private Vector3 playerPosition;

    private Coroutine routine;

    // Start is called before the first frame update
    private void Start()
    {
        gameManager = GameObject.Find("Game Manager").gameObject;
    }

    // Update is called once per frame
    private void Update()
    {
        if (player != null)
            playerPosition = player.transform.position;

        if (lookAtPlayer)
        {
            //Vector3 playerWorldPosition = Camera.main.ScreenToWorldPoint(playerPosition + Vector3.forward * 10f);
            var angle = AngleBetweenPoints(transform.GetChild(0).position, playerPosition);
            transform.GetChild(0).rotation = Quaternion.Euler(new Vector3(0f, 0f, angle + 90));
        }
    }

    //Enter Combat
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && !isShooting)
        {
            player = collision.gameObject;
            // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
            // RuntimeManager.PlayOneShot("event:/sfx/enemy/goblin/discover");
            isShooting = true;
            lookAtPlayer = true;
            gameManager.GetComponent<CombarChecker>().enemyAttacking(true);
            Invoke("StartRoutine", 1f);
        }
    }

    //Exit Combat
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            player = null;
            isShooting = false;
            lookAtPlayer = false;
            gameManager.GetComponent<CombarChecker>().enemyAttacking(false);
            StopCoroutine(routine);
        }
    }

    private void StartRoutine()
    {
        routine = StartCoroutine(Fire());
    }

    private IEnumerator Fire()
    {
        while (true)
        {
            var bullet = Instantiate(Bullet, transform.GetChild(0).position, transform.GetChild(0).rotation);
            bullet.tag = "EnemyBullet";
            var script = bullet.GetComponent<BulletScript>();
            script.Speed = bulletSpeed;
            script.player = false;
            script.playerPos = playerPosition;
            script.damage = dmg;
            script.despawnTime = 5;
            // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
            // RuntimeManager.PlayOneShot("event:/sfx/enemy/goblin/attack");
            yield return new WaitForSeconds(2);
        }
    }

    private float AngleBetweenPoints(Vector2 a, Vector2 b)
    {
        return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
    }
}