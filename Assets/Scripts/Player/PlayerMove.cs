using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float playerSpeed = 5;
    public float speedMod = 1;
    public float xAxis;
    public float yAxis;

    private Vector2 speed;

    // Start is called before the first frame update
    private void Start()
    {
        speed = new Vector2(1 * playerSpeed, 1 * playerSpeed);
    }

    // Update is called once per frame
    private void Update()
    {
        xAxis = Input.GetAxisRaw("Horizontal");

        yAxis = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        var move = new Vector2(speed.x * xAxis, speed.y * yAxis);

        move *= Time.deltaTime;

        GetComponent<Rigidbody2D>().linearVelocity = move.normalized * playerSpeed * speedMod;

        if (xAxis != 0 || yAxis != 0)
            RotatePlayer();
    }

    private void RotatePlayer()
    {
        var angle = Mathf.Atan2(xAxis, yAxis) * Mathf.Rad2Deg;
        transform.GetChild(0).GetChild(1).transform.rotation =
            Quaternion.AngleAxis((angle + 180) * -1f, Vector3.forward);
    }
}