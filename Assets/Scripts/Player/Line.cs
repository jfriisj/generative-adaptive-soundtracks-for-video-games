using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour
{
    [SerializeField] private List<GameObject> tileTriggers;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Block")
            foreach (var t in tileTriggers)
            {
                var box = t.GetComponent<BoxCollider2D>();
                box.isTrigger = false;
            }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Block")
            foreach (var t in tileTriggers)
            {
                var box = t.GetComponent<BoxCollider2D>();
                box.isTrigger = true;
            }
    }
}