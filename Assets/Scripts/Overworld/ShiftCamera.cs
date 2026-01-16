using UnityEngine;

public class ShiftCamera : MonoBehaviour
{
    [SerializeField] private string direction;
    public Camera cam;

    public GameObject player;

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
            var pm = player.GetComponent<PlayerMove>();

            switch (direction)
            {
                case "N":
                    if (pm.yAxis > 0)
                    {
                        cam.transform.position += new Vector3(0, 30);
                        player.transform.position += new Vector3(0, 5);
                    }

                    break;
                case "S":
                    if (pm.yAxis < 0)
                    {
                        cam.transform.position += new Vector3(0, -30);
                        player.transform.position += new Vector3(0, -5);
                    }

                    break;
                case "E":
                    if (pm.xAxis > 0)
                    {
                        cam.transform.position += new Vector3(55, 0);
                        player.transform.position += new Vector3(5, 0);
                    }

                    break;
                case "W":
                    if (pm.xAxis < 0)
                    {
                        cam.transform.position += new Vector3(-55, 0);
                        player.transform.position += new Vector3(-5, 0);
                    }

                    break;
            }
        }
    }
}