namespace Aeroverra.StreamDeck.NestControl
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
