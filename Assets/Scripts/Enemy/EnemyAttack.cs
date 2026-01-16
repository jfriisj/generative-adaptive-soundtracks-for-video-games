using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private int Damage;
    [SerializeField] private float atkInterval;
    private Coroutine atkPlayer;
    private GameObject player;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            player = collision.gameObject;
            atkPlayer = StartCoroutine(AttackPlayer());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            player = null;
            StopCoroutine(atkPlayer);
        }
    }

    private IEnumerator AttackPlayer()
    {
        while (true)
        {
            // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
            if (gameObject.name.Contains("Range"))
            {
                // RuntimeManager.PlayOneShot("event:/sfx/enemy/goblin/attack");
            }
            else if (gameObject.name.Contains("Rusher"))
            {
                // RuntimeManager.PlayOneShot("event:/sfx/enemy/hound/attack");
            }

            var stats = player.GetComponent<PlayerStats>();
            stats.TakeDamage(Damage);
            yield return new WaitForSeconds(atkInterval);
        }
    }
}