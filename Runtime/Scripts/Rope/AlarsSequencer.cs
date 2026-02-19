using UnityEngine;
using Smarc.Rope;

namespace Smarc.Alars
{
    [RequireComponent(typeof(Collider))]
    public class AlarsSequencer : MonoBehaviour
    {
        public Collider BuoyCollider;
        public GameObject SamRopeBuoySystem;
        public TwoSegmentWinch Winch;
        public TwoSegmentWinchPulley WinchPulley;

        void OnTriggerExit(Collider other)
        {
            if (other == BuoyCollider)
            {
                SamRopeBuoySystem.SetActive(false);
                WinchPulley.ApplySettings();
                Winch.EnableLoad();
                enabled = false;
                Debug.Log("Buoy left hook, replacing sam-rope-buoy with load of the winch!");
            }
        }
        
    }
}