using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Pickup : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI weaponText;
    [SerializeField] private GameObject attackCircle;
    public GameObject holdingWeapon;
    public Image frame;

    private GameObject weaponHover;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        if (weaponHover != null)
            if (Input.GetButtonDown("Fire1"))
            {
                if (holdingWeapon != null)
                {
                    frame.GetComponent<Image>().sprite = null;
                    holdingWeapon.SetActive(true);
                    holdingWeapon.transform.parent = null;
                }

                assignStats(attackCircle.transform.GetComponent<Attack>());

                holdingWeapon = weaponHover;
                holdingWeapon.transform.parent = transform;

                frame.GetComponent<Image>().sprite = holdingWeapon.GetComponent<SpriteRenderer>().sprite;

                holdingWeapon.SetActive(false);
            }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Weapon")
        {
            weaponHover = collision.gameObject;
            weaponText.text = "[E] " + collision.name.Split(" - ")[0];
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Weapon")
        {
            weaponHover = null;
            weaponText.text = "";
        }
    }

    private void assignStats(Attack a)
    {
        a.weapon = weaponHover.name.Split(" - ")[1];
        a.weaponDamage = weaponHover.GetComponent<WeaponStats>().Atk;
        a.weaponSpeed = weaponHover.GetComponent<WeaponStats>().AtkSpeed;

        if (weaponHover.name.Split(" - ")[1] == "Range")
            a.bulletSpeed = weaponHover.GetComponent<WeaponStats>().rangedSpeed;
    }
}