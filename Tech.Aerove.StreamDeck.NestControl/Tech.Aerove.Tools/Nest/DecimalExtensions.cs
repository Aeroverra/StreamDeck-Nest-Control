using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tech.Aerove.Tools.Nest
{
    public static class DecimalExtensions
    {
        public static decimal ToFahrenheit(this decimal celsius)
        {
            return celsius * 9 / 5 + 32;
        }
        public static decimal ToCelsius(this decimal fahrenheit)
        {
            return (fahrenheit - 32) * 5 / 9;
        }
    }
}
