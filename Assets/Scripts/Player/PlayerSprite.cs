using UnityEngine;

public class PlayerSprite : MonoBehaviour
{
    [SerializeField] private Sprite[] playerSprites; // 0: Front, 1: Back, 2: Left, 3: Right

    private Aiming aim;

    private bool heldButton = false;

    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        aim = transform.GetChild(0).GetComponent<Aiming>();
    }

    // Update is called once per frame
    private void Update()
    {
        var angle = aim.angle;

        //Look forward
        if (angle > 45 && angle < 135) spriteRenderer.sprite = playerSprites[0];

        //Look Up
        if (angle < -45 && angle > -135) spriteRenderer.sprite = playerSprites[1];

        //Look Left
        if (angle < 45 && angle > -45) spriteRenderer.sprite = playerSprites[2];

        //Look right
        if (angle < -135 || angle > 135) spriteRenderer.sprite = playerSprites[3];
    }
}