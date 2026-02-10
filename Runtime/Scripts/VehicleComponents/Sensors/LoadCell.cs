using System.Collections.Generic;
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
                List<float> forces = new();
                body.GetDriveForces(forces);
                Force = 0f;
                for (int i = 1; i < forces.Count; i++)
                    Force += forces[i];
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