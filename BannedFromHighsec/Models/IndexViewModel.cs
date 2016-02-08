using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BannedFromHighsec.Models
{
    public class IndexViewModel
    {

        public List<viewLosses> highsecLosses = new List<viewLosses>();

        public IndexViewModel()
        {
            highsecLosses = null;
        }

    }
}