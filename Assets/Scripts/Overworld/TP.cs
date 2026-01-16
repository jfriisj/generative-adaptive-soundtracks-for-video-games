using UnityEngine;

public class TP : MonoBehaviour
{
    [SerializeField] private GameObject TPPointB;
    [SerializeField] private GameObject TPPointC;
    [SerializeField] private Camera Cam;
    [SerializeField] private GameObject audioManagerInstance;
    private AudioManager audioManager;

    private void Start()
    {
        audioManager = audioManagerInstance.GetComponent<AudioManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            collision.transform.position = new Vector3(TPPointB.transform.position.x, TPPointB.transform.position.y,
                collision.transform.position.z);
            Cam.transform.position = new Vector3(TPPointC.transform.position.x, TPPointC.transform.position.y,
                Cam.transform.position.z);
            
            // Note: Removed dungeon music toggle - teleportation doesn't change biome
            // Music will be handled by BiomeChanger triggers instead
            
            // TODO: Replace with Unity AudioSource.PlayOneShot or adaptive music system
            // RuntimeManager.PlayOneShot("event:/sfx/player/tp");
        }
    }
}