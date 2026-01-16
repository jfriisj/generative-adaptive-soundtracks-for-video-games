using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Slider hpBar;
    public int MaxHP;
    public int currentHP;
    public int playerAtk;

    public bool isPoisoned;
    private SpriteRenderer sprite;

    private void Start()
    {
        currentHP = MaxHP;
        hpBar.maxValue = MaxHP;
        hpBar.value = MaxHP;
        sprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "EnemyBullet")
        {
            var b = collision.gameObject.GetComponent<BulletScript>();
            TakeDamage(b.damage);

            Destroy(collision.gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        hpBar.value = currentHP;

        if (currentHP <= 0) Die();
        StartCoroutine(Fade());
    }

    private void Die()
    {
        Destroy(gameObject);
        gameOverScreen.SetActive(true);
        Time.timeScale = 0;
    }

    private IEnumerator Fade()
    {
        sprite.color = new Color(255 / 255, 0 / 255, 0 / 255);


        if (isPoisoned)
        {
            float i = 1;
            while (i >= 0)
            {
                sprite.color = new Color(i, 1, 0);
                i -= 0.2f;
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            float i = 0;
            while (i <= 1)
            {
                sprite.color = new Color(1, i, i);
                i += 0.2f;


                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}