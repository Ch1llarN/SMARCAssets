using UnityEngine;

namespace Rope
{
    public class TwoSegmentWinchPulley : MonoBehaviour
    {
        [Header("Winches")]
        public TwoSegmentWinch WinchOne;
        public TwoSegmentWinch WinchTwo;

        [Header("Settings")]
        public float TotalRopeLength = 5f;
        public float PulleySpeed = 0.5f;

        [Header("Current State")]
        public float loadOne;
        public float loadTwo;
        public bool tenseOne, tenseTwo;
        public bool onePulls, twoPulls;
        public float speed;

        public void ApplySettings()
        {
            // set the rope lengths such that they add up to the total rope length, and the load starts in the middle
            WinchOne.RopeLength = TotalRopeLength / 2f;
            WinchTwo.RopeLength = TotalRopeLength / 2f;
            WinchOne.StartingRopeLength = TotalRopeLength / 4f;
            WinchTwo.StartingRopeLength = TotalRopeLength / 4f;
            WinchOne.ApplySettings();
            WinchTwo.ApplySettings();
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