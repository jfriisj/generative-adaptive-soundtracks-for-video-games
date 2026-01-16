using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Sprint : MonoBehaviour
{
    [SerializeField] private Slider sprintBar;
    [SerializeField] private Slider dashBar;

    public float dashTimer = 5f;
    public float dashSpeed = 20f;

    private bool disableSprint;

    private PlayerMove move;

    // Start is called before the first frame update
    private void Start()
    {
        move = GetComponent<PlayerMove>();
    }

    // Update is called once per frame
    private void Update()
    {
        //Sprint
        if (Input.GetButton("Sprint") && sprintBar.value != 0 && !disableSprint)
        {
            move.speedMod = 2;
            sprintBar.value -= 20 * Time.deltaTime;
            if (sprintBar.value == 0) StartCoroutine(SprintCooldown());
        }
        else
        {
            move.speedMod = 1;
            if (sprintBar.value != 100) sprintBar.value += 20 * Time.deltaTime;
        }

        //Dash
        //if(Input.GetButtonDown("Dash") && dashBar.value == 100)
        //{
        //    StartCoroutine(Dash());
        //    dashBar.value = 0;
        //}
        //else
        //{
        //    if (dashBar.value != 100)
        //    {
        //        dashBar.value += 30 * Time.deltaTime;
        //    }
        //}
    }

    private IEnumerator SprintCooldown()
    {
        disableSprint = true;
        yield return new WaitForSeconds(3);
        disableSprint = false;
    }

    //IEnumerator Dash()
    //{
    //    Debug.Log("Dashing");
    //    float currentDash = dashTimer;
    //    while(currentDash > 0)
    //    {
    //        currentDash -= 0.1f;
    //        move.speedMod = dashSpeed;
    //        yield return null;
    //    }

    //    move.speedMod = 1;
    //}
}