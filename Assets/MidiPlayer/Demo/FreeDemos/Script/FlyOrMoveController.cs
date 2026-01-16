using UnityEngine;

namespace DemoMPTK
{
    public class FlyOrMoveController : MonoBehaviour
    {
        public Transform[] movePath;
        public Transform[] lookPath;
        public Transform lookTarget;
        public float percentage;

        [Range(0, 300)] public float speed = 20;

        public bool FreeMove;

        public float rotationSpeed = 10;
        public float dampingTime = 0.2f;
        public float movingSensitivity = 1f;

        public Vector3 m_TargetAngles;
        private Vector3 m_FollowAngles;
        private Vector3 m_FollowMoves;
        private Vector3 m_FollowVelocityMov;
        private Vector3 m_FollowVelocityRot;
        private Vector3 m_TargetMoves;

        private void Start()
        {
            m_FollowAngles = m_TargetAngles = transform.localEulerAngles;
            m_FollowMoves = m_TargetMoves = transform.position;
        }

        private void FixedUpdate()
        {
            if (!FreeMove)
            {
                percentage += Time.unscaledDeltaTime * speed / 800;
                if (percentage >= 1f) percentage = 0f;
            }
            else
            {
                if (Input.GetButton("Fire2"))
                {
                    // read input from mouse or mobile controls
                    var inputH = Input.GetAxis("Mouse X"); //Input.mousePosition.x;
                    var inputV = Input.GetAxis("Mouse Y"); //Input.mousePosition.y;

                    //Debug.Log(inputH + " " + inputV);
                    m_TargetAngles.x -= inputV * rotationSpeed;
                    m_TargetAngles.y += inputH * rotationSpeed;
                    // smoothly interpolate current values to target angles
                    m_FollowAngles = Vector3.SmoothDamp(m_FollowAngles, m_TargetAngles, ref m_FollowVelocityRot,
                        dampingTime);
                    // update the actual gameobject's rotation and position
                    transform.localEulerAngles = new Vector3(m_FollowAngles.x, m_FollowAngles.y, 0);
                }

                var inputdir = Vector3.zero;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputdir.z = movingSensitivity;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputdir.z = -movingSensitivity;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputdir.x = -movingSensitivity;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputdir.x = movingSensitivity;
                // Moves in direction of the camera
                m_TargetMoves += transform.localRotation * inputdir;
                // smoothly interpolate current position to target position
                m_FollowMoves = Vector3.SmoothDamp(m_FollowMoves, m_TargetMoves, ref m_FollowVelocityMov, dampingTime);
                transform.position = m_FollowMoves;
            }
        }


        private void OnGUI()
        {
            if (!FreeMove)
            {
                percentage = GUI.VerticalSlider(new Rect(Screen.width - 20, 20, 15, Screen.height - 40), percentage, 1,
                    0);
                //Debug.Log("OnGUI percentage :" + percentage);

                iTween.PutOnPath(gameObject, movePath, percentage);
                if (lookTarget != null)
                {
                    iTween.PutOnPath(lookTarget, lookPath, percentage);
                    transform.LookAt(lookTarget);
                }
            }
        }

        private void OnDrawGizmos()
        {
            iTween.DrawPath(movePath, Color.magenta);
            iTween.DrawPath(lookPath, Color.cyan);
            Gizmos.color = Color.black;
            if (lookTarget != null)
                Gizmos.DrawLine(transform.position, lookTarget.position);
        }

        public void SetFreeMove(bool free)
        {
            m_FollowAngles = m_TargetAngles = transform.localEulerAngles;
            m_FollowMoves = m_TargetMoves = transform.position;
            FreeMove = free;
        }

        //void SlideTo(float position)
        //{
        //    iTween.Stop(gameObject);
        //    iTween.ValueTo(gameObject, iTween.Hash("from", percentage, "to", position, "time", 2, "easetype", iTween.EaseType.easeInOutCubic, "onupdate", "SlidePercentage"));
        //}

        //void SlidePercentage(float p)
        //{
        //    percentage = p;
        //}
    }
}