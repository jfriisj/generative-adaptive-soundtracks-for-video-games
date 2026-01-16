using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] private GameObject gameManager;
    public int HP;

    public GameObject particle;
    private SpriteRenderer sprite;

    private void Start()
    {
        gameManager = GameObject.Find("Game Manager").gameObject;
        sprite = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "PlayerBullet")
        {
            var b = collision.gameObject.GetComponent<BulletScript>();

            TakeDamage(b.damage);
            Destroy(collision.gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        HP -= damage;
        // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
        // RuntimeManager.PlayOneShot("event:/sfx/enemy/goblin/hurt");
        if (HP <= 0)
        {
            if (gameObject.name.Contains("Range"))
            {
                // RuntimeManager.PlayOneShot("event:/sfx/enemy/goblin/death");
            }
            else if (gameObject.name.Contains("Rusher"))
            {
                // RuntimeManager.PlayOneShot("event:/sfx/enemy/hound/death");  
            }

            Die();
        }


        StartCoroutine(Fade());
    }

    private void Die()
    {
        var pe = Instantiate(particle, gameObject.transform.position, gameObject.transform.rotation);
        Destroy(gameObject);
        Destroy(pe, 1);
    }

    private IEnumerator Fade()
    {
        sprite.color = new Color(255 / 255, 0 / 255, 0 / 255);
        float i = 0;
        while (i <= 1)
        {
            sprite.color = new Color(1, i, i);
            i += 0.2f;
            yield return new WaitForSeconds(0.1f);
        }
    }
}