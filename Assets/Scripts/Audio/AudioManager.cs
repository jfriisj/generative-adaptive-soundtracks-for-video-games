using AdaptiveMusic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Scene scene;

    [SerializeField] private AdaptiveMusicSystem adaptiveMusicSystem;
    [SerializeField] private MusicConfigSO forestZone;      // woods biome
    [SerializeField] private MusicConfigSO oceanZone;       // beach biome  
    [SerializeField] private MusicConfigSO cherryZone;      // orchard biome
    [SerializeField] private MusicConfigSO mountainZone;    // cliffs biome
    [SerializeField] private MusicConfigSO safeZone;        // safe/start area
    [SerializeField] private MusicConfigSO victoryZone;     // final canvas

    public bool isStatic;

    // Biome influence values (0-1) - set by BiomeChanger triggers
    public float playerState;   // Danger level (0 = safe, 1 = combat)
    public float woods;         // Forest biome
    public float beach;         // Ocean biome  
    public float orchard;       // Cherry biome
    public float cliffs;        // Mountain biome

    private MusicConfigSO currentZone;

    private void Awake()
    {
        ManageSingleton();
        scene = SceneManager.GetActiveScene();
    }

    private void Start()
    {
        if (scene.name == "Static Scene") isStatic = true;

        // Start adaptive music system if it exists
        if (adaptiveMusicSystem != null && forestZone != null) StartAdaptiveMusic();
    }

    private void ManageSingleton()
    {
        if (instance != null)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void StartAdaptiveMusic()
    {
        if (adaptiveMusicSystem == null) return;

        // Start with the appropriate zone based on biome values
        UpdateZone();
    }

    public void FMODResetParams()
    {
        woods = 0f;
        beach = 0f;
        orchard = 0f;
        cliffs = 0f;
        UpdateZone();
    }

    public void FMODUpdateParam()
    {
        UpdateZone();

        // Update combat state for the adaptive system.
        // We prefer AdaptiveMusicSystem's automatic danger mode (GameStateTracker-driven)
        // instead of forcing manual danger values.
        if (adaptiveMusicSystem != null)
            adaptiveMusicSystem.UseAutomaticDangerLevel();

        if (GameStateTracker.Instance != null)
            GameStateTracker.Instance.SetCombatActive(playerState > 0.5f);
    }

    private void UpdateZone()
    {
        if (adaptiveMusicSystem == null) return;

        // Determine which zone to play based on biome parameters
        // Map FMOD-style params to actual biome configs
        MusicConfigSO targetZone = safeZone ?? forestZone; // Default to safe zone

        if (cliffs > 0.5f && mountainZone != null)
            targetZone = mountainZone;
        else if (orchard > 0.5f && cherryZone != null)
            targetZone = cherryZone;
        else if (beach > 0.5f && oceanZone != null)
            targetZone = oceanZone;
        else if (woods > 0.5f && forestZone != null)
            targetZone = forestZone;

        // Only change zone if it's different
        if (targetZone != currentZone)
        {
            currentZone = targetZone;
            adaptiveMusicSystem.ChangeZone(targetZone);
            Debug.Log($"[AudioManager] Zone changed to: {targetZone?.zoneName ?? "null"}");
        }
    }

    /// <summary>
    /// Trigger victory music (for Final Canvas/game completion)
    /// </summary>
    public void PlayVictoryMusic()
    {
        if (adaptiveMusicSystem == null || victoryZone == null) return;
        
        FMODResetParams();
        currentZone = victoryZone;
        adaptiveMusicSystem.ChangeZone(victoryZone);
        Debug.Log("[AudioManager] Playing victory music!");
    }

    /// <summary>
    /// Set combat/danger state (0 = peaceful, 1 = intense combat)
    /// </summary>
    public void SetPlayerState(float dangerLevel)
    {
        playerState = Mathf.Clamp01(dangerLevel);

        if (adaptiveMusicSystem != null)
            adaptiveMusicSystem.UseAutomaticDangerLevel();

        if (GameStateTracker.Instance != null)
            GameStateTracker.Instance.SetCombatActive(playerState > 0.5f);
    }

    public void StopBGM()
    {
        // Adaptive music system handles stopping internally
        // Could add explicit stop if needed
    }

    public float LerpParameter(float fParam)
    {
        if (fParam < 1)
            for (var i = 0; i <= 60; i++)
                fParam = i / 60;
        else
            for (var i = 60; i >= 0; i--)
                fParam = i / 60;

        return fParam;
    }
}