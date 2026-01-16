using UnityEngine;
using UnityEngine.Tilemaps;

public class CollectableChecker : MonoBehaviour
{
    [SerializeField] private Tilemap doorMap;
    public int collectables;

    public void getCollectable()
    {
        collectables++;

        if (collectables == 4) doorMap.gameObject.SetActive(false);
    }
}