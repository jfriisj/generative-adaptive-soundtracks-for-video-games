using UnityEngine;

namespace MPTKDemoCatchMusic
{
    public class Collide : MonoBehaviour
    {
        public float accelerate;
        public float speed;

        // Use this for initialization
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
            if (transform.position.y > 100f) Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (transform.position.y > 5f)
                speed += Time.fixedDeltaTime * accelerate;
            var translation = Time.fixedDeltaTime * speed;
            transform.Translate(0, translation, 0);
        }

        private void OnCollisionEnter(Collision col)
        {
            //Destroy(col.gameObject);
        }
    }
}