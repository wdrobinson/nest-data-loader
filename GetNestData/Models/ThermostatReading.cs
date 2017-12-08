using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetNestData.Models
{
    public class ThermostatReading
    {
        public int Temperature { get; set; }
        public int Humidity { get; set; }
        public bool IsHvacOn { get; set; }
    }
}
