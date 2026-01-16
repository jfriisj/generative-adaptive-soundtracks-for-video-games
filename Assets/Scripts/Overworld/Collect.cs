using UnityEngine;

public class Collect : MonoBehaviour
{
    [SerializeField] private GameObject gameManager;
    [SerializeField] private GameObject Canvas;
    private CollectableChecker checker;

    private bool checkPlayer;

    // Start is called before the first frame update
    private void Start()
    {
        checker = gameManager.GetComponent<CollectableChecker>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetButtonDown("Interact") && checkPlayer) getCollectable();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" || collision.tag == "PlayerMelee")
        {
            Canvas.SetActive(true);
            checkPlayer = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player" || collision.tag == "PlayerMelee")
        {
            Canvas.SetActive(false);
            checkPlayer = false;
        }
    }

    private void getCollectable()
    {
        checker.getCollectable();
        Destroy(gameObject);
    }
}