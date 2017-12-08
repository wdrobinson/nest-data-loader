using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetNestData.Models
{
    public class Reading
    {
        public ThermostatReading One { get; set; }
        public ThermostatReading Two { get; set; }
        public int? TargetTemperature { get; set; }
        public double? TemperatureOutside { get; set; }
        public long Timestamp { get; set; }
    }
}
