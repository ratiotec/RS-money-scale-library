using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using rt.Devices.RsScale;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var r = new RsScale())
            {
                r.Open();
                if (r.IsOpen)
                {
                    var w = r.GetWeight();
                    var values = r.GetAccountingValues();
                }
            }

        }
    }
}
