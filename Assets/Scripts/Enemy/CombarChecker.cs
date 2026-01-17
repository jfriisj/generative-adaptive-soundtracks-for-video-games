using UnityEngine;

public class CombarChecker : MonoBehaviour
{
    [SerializeField] private GameObject audioInstance;
    public bool inCombat;
    public int amount;
    private AudioManager audioManager;

    private void Start()
    {
        audioManager = audioInstance.GetComponent<AudioManager>();
    }

    public void enemyAttacking(bool inRange)
    {
        if (inRange)
            amount++;
        else
            amount--;

        if (amount == 0)
        {
            audioManager.playerState = 0f;
            inCombat = false;
            if (!audioManager.isStatic)
                audioManager.ApplyBiomeAndCombatState();
        }
        else
        {
            audioManager.playerState = 1f;
            if (!audioManager.isStatic)
                if (!inCombat)
                    audioManager.ApplyBiomeAndCombatState();
            inCombat = true;
        }
    }
}