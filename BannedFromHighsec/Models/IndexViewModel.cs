using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BannedFromHighsec.Models
{
    public class IndexViewModel
    {

        public List<eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill> highsecLosses {get; set;}

        public IndexViewModel()
        {
            highsecLosses = null;
        }

    }
}