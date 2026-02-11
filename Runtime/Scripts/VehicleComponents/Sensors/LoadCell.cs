using System;
using UnityEngine;

namespace VehicleComponents.Sensors
{
    [AddComponentMenu("Smarc/Sensor/LoadCell")]
    public class LoadCell : Sensor
    {
        [Header("LoadCell")]
        public float Force;
        public float Weight;
        public Joint joint;
        public ArticulationBody body;


        public override bool UpdateSensor(double deltaTime)
        {
            if (body != null)
            {
                var parent = body.transform.parent;
                if (!parent)
                {
                    Force = 0;
                    Weight = 0;
                    return false;    
                } 
                ArticulationReducedSpace forces = body.driveForce;
                if (forces.dofCount > 1)
                {
                    Debug.LogWarning($"LoadCell only supports 1 DOF joints (prismatic, revolute), but joint has {forces.dofCount} DOFs.");
                    Force = 0;
                    Weight = 0;
                    return false;
                }
                Force = forces[0];
                Weight = Force / Physics.gravity.magnitude;
                
                return true;
            }

            if (joint != null)
            {
                Force = joint.currentForce.magnitude;
                Weight = Force / Physics.gravity.magnitude;
                return true;
            }

            return false;

        }

    }

}