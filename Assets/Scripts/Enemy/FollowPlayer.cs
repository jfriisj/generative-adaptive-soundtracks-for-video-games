using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private GameObject gameManager;
    [SerializeField] private GameObject enemyObj;
    private NavMeshAgent agent;
    private Vector3 basePos;
    private GameObject player;
    private bool Reset;
    private bool targetPlayer;

    // Start is called before the first frame update
    private void Start()
    {
        gameManager = GameObject.Find("Game Manager").gameObject;
        agent = enemyObj.GetComponent<NavMeshAgent>();
        basePos = transform.parent.position;
    }

    // Update is called once per frame
    private void Update()
    {
        enemyObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
        if (targetPlayer)
        {
            agent.destination = player.transform.position;
        }
        else
        {
            if (Reset)
            {
                agent.destination = basePos;
                Reset = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            player = collision.gameObject;
            if (!targetPlayer)
            {
                // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
                // RuntimeManager.PlayOneShot("event:/sfx/enemy/hound/discover");
            }

            targetPlayer = true;
            gameManager.GetComponent<CombarChecker>().enemyAttacking(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            player = null;
            targetPlayer = false;
            gameManager.GetComponent<CombarChecker>().enemyAttacking(false);
            Reset = true;
        }
    }
}