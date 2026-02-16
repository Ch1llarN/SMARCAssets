using System.Collections.Generic;
using UnityEngine;

namespace VehicleComponents.Sensors
{


    [AddComponentMenu("Smarc/Sensor/LoadCell")]
    public class LoadCell : Sensor
    {
        [Header("LoadCell")]
        public bool PositiveOnly = true;
        public int SmoothOverFrames = 5;
        public float Force;
        public float Weight;
        public Joint joint;
        public ArticulationBody body;



        public override bool UpdateSensor(double deltaTime)
        {
            float instantForce = 0;
            if (body != null)
            {
                var parent = body.transform.parent;
                if (parent)
                {
                    ArticulationReducedSpace forces = body.driveForce;
                    if (forces.dofCount > 1)
                    {
                        Debug.LogWarning($"LoadCell only supports 1 DOF joints (prismatic, revolute), but joint has {forces.dofCount} DOFs.");
                    }
                    else
                    {
                        instantForce = forces[0];
                        if (PositiveOnly && instantForce < 0) instantForce = 0;
                    }
                }
            }

            if (joint != null)
            {
                instantForce = joint.currentForce.magnitude;
                if (PositiveOnly && instantForce < 0) instantForce = 0;
            }

            if (instantForce != 0)
            {
                Force = instantForce;
                Weight = Force / Physics.gravity.magnitude;
                return true;
            }

            return false;

        }

    }

}