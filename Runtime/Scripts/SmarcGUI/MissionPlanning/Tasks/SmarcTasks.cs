using System.Collections.Generic;
using SmarcGUI.MissionPlanning.Params;

namespace SmarcGUI.MissionPlanning.Tasks
{
    public class SmarcStartGeofence : Task
    {
        public override void SetParams()
        {
            Name = "smarc-start-geofence";
            Description = "Start a geofence task";
            Params.Add("waypoints", new List<GeoPoint>());
            Params.Add("ceiling_altitude", -1.0);
            Params.Add("floor_altitude", 1.0);
        }
    }

    public class SmarcStopGeofence : Task
    {
        public override void SetParams()
        {
            Name = "smarc-stop-geofence";
            Description = "Stop a geofence task";
        }
    }
}
