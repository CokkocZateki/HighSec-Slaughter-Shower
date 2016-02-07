using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BannedFromHighsec.Models
{
    public class Losses
    {

        public eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill zbKill { get; set; }
        public string shipName {get; set;}
        public string systemName { get; set; }
        
        public Losses()
        {
            zbKill = new eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill();
            shipName = null;
            systemName = null;
        }

        public Losses(eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill zkbKill, string sysName, string shipN)
        {
            zbKill = new eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill();
            shipName = shipN;
            systemName = sysName;
        }



    }
}