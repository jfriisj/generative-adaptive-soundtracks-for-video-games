using System.Collections;
using UnityEngine;

public class Poisoned : MonoBehaviour
{
    [SerializeField] private GameObject player;
    public float poisonDuration;

    public int poisonDamage;

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
            poisonDuration = 5;
            poisonDamage = 3;
            StartCoroutine(PoisonDmg());
        }
    }

    private IEnumerator PoisonDmg()
    {
        var stats = player.GetComponent<PlayerStats>();
        stats.isPoisoned = true;
        while (poisonDuration > 0)
        {
            stats.TakeDamage(poisonDamage);
            poisonDuration--;
            yield return new WaitForSeconds(1);
        }

        stats.isPoisoned = false;
        player.transform.GetChild(0).GetComponent<SpriteRenderer>().color = Color.white;
    }
}