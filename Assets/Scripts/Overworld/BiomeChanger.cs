using UnityEngine;

public class BiomeChanger : MonoBehaviour
{
    public string currentBiome;
    [SerializeField] private GameObject audioManagerInstance;

    private AudioManager audioManager;

    // Start is called before the first frame update
    private void Start()
    {
        audioManager = audioManagerInstance.GetComponent<AudioManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Biome")
            switch (collision.name)
            {
                case "Forest":
                    currentBiome = "Forest";
                    audioManager.ResetBiomeInfluences();
                    audioManager.woods = 1f;
                    if (!audioManager.isStatic)
                        audioManager.ApplyBiomeAndCombatState();
                    break;
                case "Ocean":
                    currentBiome = "Ocean";
                    audioManager.ResetBiomeInfluences();
                    audioManager.beach = 1f;
                    if (!audioManager.isStatic)
                        audioManager.ApplyBiomeAndCombatState();
                    break;
                case "Cherry":
                    currentBiome = "Cherry";
                    audioManager.ResetBiomeInfluences();
                    audioManager.orchard = 1f;
                    if (!audioManager.isStatic)
                        audioManager.ApplyBiomeAndCombatState();
                    break;
                case "Mountain":
                    currentBiome = "Mountain";
                    audioManager.ResetBiomeInfluences();
                    audioManager.cliffs = 1f;
                    if (!audioManager.isStatic)
                        audioManager.ApplyBiomeAndCombatState();
                    break;
            }
    }
}