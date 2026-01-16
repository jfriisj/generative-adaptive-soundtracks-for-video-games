using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [SerializeField] public int damage;
    [SerializeField] public float Speed;
    [SerializeField] public bool player;

    public int despawnTime;
    public Vector3 playerPos;

    private Rigidbody2D rb;

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Invoke("Despawn", despawnTime);
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void FixedUpdate()
    {
        var vel = Speed * Time.deltaTime * 100f;

        //if(player)
        //{
        rb.linearVelocity = vel * transform.up;
        //}
        //else
        //{
        //    transform.position = Vector3.MoveTowards(transform.position, playerPos, vel);
        //    //if(Vector3.Distance(transform.position, playerPos) < 0.1f)
        //    //{
        //    //    Destroy(gameObject);
        //    //}
        //}
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Terrain" || collision.tag == "CameraTrigger") Destroy(gameObject);
    }


    private void Despawn()
    {
        Destroy(gameObject);
    }
}