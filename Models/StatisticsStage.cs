using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Models
{
    class StatisticsStage
    {
        public int id { get; set; }
        public long chatid { get; set; }
        public string user_name { get; set; }
        public string stage { get; set; }
        public DateTime startdt { get; set; }
        public DateTime enddt { get; set; }

    }
}
