using UnityEngine;

namespace Rope
{
    public class TwoSegmentWinchPulley : MonoBehaviour
    {
        [Header("Loads")]
        public Transform GlobalLoadTfOne;
        public Transform GlobalLoadTfTwo;

        [Header("Winches")]
        public TwoSegmentWinch WinchOne;
        public TwoSegmentWinch WinchTwo;

        [Header("Settings")]
        public float RopeLength = 5f;
        public float PulleySpeed = 0.5f;

        [Header("Current State")]
        public float loadOne;
        public float loadTwo;
        public bool tenseOne, tenseTwo;
        public bool onePulls, twoPulls;
        public float speed;

        public void ApplySettings()
        {
            WinchOne.RopeLength = RopeLength;
            WinchTwo.RopeLength = RopeLength;
            
            // first rotate the winches to match the direction towards the targets
            WinchOne.transform.LookAt(GlobalLoadTfOne);
            WinchOne.transform.Rotate(-90f, 0f, 0f); // because the winches are "negative Y forward"
            WinchTwo.transform.LookAt(GlobalLoadTfTwo);
            WinchTwo.transform.Rotate(-90f, 0f, 0f);

            // Then set the starting rope length of winch one
            // to match the distance to the target, limited by the total rope length
            // and give the remaining rope length to winch two
            var toTargetOne = Vector3.Distance(transform.position, GlobalLoadTfOne.position);
            WinchOne.StartingRopeLength = Mathf.Clamp(toTargetOne, WinchOne.MinRopeLength, RopeLength);
            WinchTwo.StartingRopeLength = Mathf.Clamp(RopeLength - WinchOne.StartingRopeLength, WinchTwo.MinRopeLength, RopeLength);

            WinchOne.ApplySettings();
            WinchTwo.ApplySettings();
        }

        void Awake()
        {
            WinchOne.gameObject.SetActive(false);
            WinchTwo.gameObject.SetActive(false);
            ApplySettings();
            WinchOne.gameObject.SetActive(true);
            WinchTwo.gameObject.SetActive(true);
        }

        void FixedUpdate()
        {
            // we want to move change the length of the ropes (by adjusting winch speeds) 
            // such that the loaded side lengthens and the other side shortens, but the total length of the rope remains constant
            // the speed of length change should be proportional to the load difference
            loadOne = WinchOne.GetTopLoad();
            loadTwo = WinchTwo.GetTopLoad();
            // these two are mutually exclusive but not exhaustive, both can be false.
            // ex: one is extended, and load one is larger. So nothing should move, because the rope is already extended on the side with more load.
            tenseOne = WinchOne.IsTense();
            tenseTwo = WinchTwo.IsTense();
            onePulls = !tenseOne && loadOne > loadTwo;
            twoPulls = !tenseTwo && loadTwo > loadOne;
            
            if (onePulls)
            {
                speed = PulleySpeed;
            }
            else if (twoPulls)
            {
                speed = -PulleySpeed;
            }
            else
            {
                speed = 0f;
            }

            WinchOne.PullSpeed = -speed;
            WinchTwo.PullSpeed = speed;
            
        }
    }
}