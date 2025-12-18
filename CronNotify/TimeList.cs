using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronNotify
{
    /*
     * [{"time":"2025-12-18 16:17:33","text":"123"}]
     */
    internal class TimeList
    {
        public DateTime time;
        public Int16 times = 0;
        public Int16 beforeSecond = 0;
        public String text = "";
    }
}
