using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BannedFromHighsec.Models
{
    public class viewLosses
    {

        public long killID { get; set; }
        public DateTime killTime { get; set; }
        public long victimID { get; set; }
        public long locationID { get; set; }
        public string victimName { get; set; }
        public string locationName { get; set; }
        public long victimShipID { get; set; }
        public string victimShipName { get; set; }
        public long victimLostIsk { get; set; }

        public viewLosses(long kID, DateTime kTime, long vicID, string vicName, long locID, string locName, long vicShipID, string vicShipName, long vicLostIsk)
        {
            killID = kID;
            killTime = kTime;
            victimID = vicID;
            victimName = vicName;
            locationID = locID;
            locationName = locName;
            victimShipID = vicShipID;
            victimShipName = vicShipName;
            victimLostIsk = vicLostIsk;
        }

        public viewLosses(long kID, DateTime kTime, long vicID, string vicName, long locID, string locName, long vicShipID, long vicLostIsk)
        {
            killID = kID;
            killTime = kTime;
            victimID = vicID;
            victimName = vicName;
            locationID = locID;
            locationName = locName;
            victimShipID = vicShipID;
            victimShipName = "";
            victimLostIsk = vicLostIsk;
        }

    }
}