using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aeroverra.StreamDeck.NestControl.Services
{
    internal class DisplayService
    {
        public async Task DisplayTemperature(IActionDispatcher dispatcher, GoogleHomeEnterpriseSdmV1Device? thermostat, bool isDial = false)
        {
            if(thermostat == null)
                return;

            var temp = thermostat.GetThermostatRenderedTemperature(TemperatureScale.FAHRENHEIT);
            await dispatcher.SetTitleAsync(temp);

            if (isDial)
            {
                await dispatcher.SetFeedbackAsync(new
                {
                    value = $"{temp}"
                });
            }
        }

        public async Task DisplaySetPoint(IActionDispatcher dispatcher, GoogleHomeEnterpriseSdmV1Device? thermostat, bool isDial = false)
        {
            if (thermostat == null)
                return;

            var thermostatMode = thermostat.GetThermostatMode();

            var setPointRender = thermostat.GetThermostatRenderedSetPoint(TemperatureScale.FAHRENHEIT);

            var modeText = $"";
            if (thermostatMode.Mode == ThermostatMode.COOL)
            {
                await dispatcher.SetStateAsync(1);
                await dispatcher.SetImageAsync(ImageColors.Blue.DataUri);
                modeText = $"{thermostatMode.Mode}\n{setPointRender}";
            }
            if (thermostatMode.Mode == ThermostatMode.HEAT)
            {
                await dispatcher.SetStateAsync(1);
                await dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                modeText = $"{thermostatMode.Mode}\n{setPointRender}";
            }
            if (thermostatMode.Mode == ThermostatMode.HEATCOOL)
            {
                await dispatcher.SetStateAsync(1);
                await dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                modeText = $"H&C\n{setPointRender}";
            }
            if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                await dispatcher.SetStateAsync(0);
                await dispatcher.SetImageAsync("");
                modeText = $"{thermostatMode.Mode}";
            }
            await dispatcher.SetTitleAsync($"{modeText}");
            if (isDial)
            {
                await dispatcher.SetFeedbackAsync(new
                {
                    value = $"{modeText}"
                });
            }
        }
        public async Task DisplayStatic(IActionDispatcher dispatcher, GoogleHomeEnterpriseSdmV1Device? thermostat, bool isDial = false)
        {
            if (thermostat == null)
                return;

            var thermostatMode = thermostat.GetThermostatMode();
            var setPointRender = thermostat.GetThermostatRenderedSetPoint(TemperatureScale.FAHRENHEIT);
            var currentTemp = thermostat.GetThermostatRenderedTemperature(TemperatureScale.FAHRENHEIT);

            string modeLabel = $"{thermostatMode.Mode}";
            string bgImage = ""; 

            if (thermostatMode.Mode == ThermostatMode.COOL)
            {
                await dispatcher.SetStateAsync(1);
                bgImage = ImageColors.Blue.DataUri;
            }
            else if (thermostatMode.Mode == ThermostatMode.HEAT)
            {
                await dispatcher.SetStateAsync(1);
                bgImage = ImageColors.Red.DataUri;
            }
            else if (thermostatMode.Mode == ThermostatMode.HEATCOOL)
            {
                await dispatcher.SetStateAsync(1);
                bgImage = ImageColors.RedBlue.DataUri;
                modeLabel = "H&C";
            }
            else
            {
                await dispatcher.SetStateAsync(0);
                // For OFF, we might want a default or empty image. 
                // Existing logic set Image to "", so we'll pass empty.
                bgImage = "";
            }

            // Construct status line
            string statusLine = $"{modeLabel} - {setPointRender}";

           if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                statusLine = "OFF";
            }
            else if (thermostatMode.Mode == ThermostatMode.HEATCOOL)
            {
                statusLine = $"{setPointRender}";
            }

            // Generate SVG with separate font sizes and background image
            string svgBase64 = GenerateSvg(bgImage, currentTemp, statusLine);

            // Set Image and Clear Title to avoid overlap
            await dispatcher.SetImageAsync(svgBase64);
            await dispatcher.SetTitleAsync("");

            if (isDial)
            {
                // Dials can use the simple text format as they support layouts
               string title = $"{currentTemp}\n\n{statusLine}";
                await dispatcher.SetFeedbackAsync(new
                {
                    value = title
                });
            }
        }

        private string GenerateSvg(string bgImage, string temp, string status)
        {
            // If we have a background image, use it. Otherwise (OFF), default to black/transparent 
            string bgContent = string.IsNullOrEmpty(bgImage) 
                ? "<rect width='72' height='72' fill='#000000' />" // Fallback black for OFF
                : $@"<image href='{bgImage}' width='72' height='72' />";

            // Simple SVG Key generation
            string svg = $@"<svg width='72' height='72' viewBox='0 0 72 72' xmlns='http://www.w3.org/2000/svg'>
  {bgContent}
  <text x='36' y='35' text-anchor='middle' font-family='Arial, sans-serif' font-weight='bold' font-size='30' fill='white'>{temp}</text>
  <text x='36' y='58' text-anchor='middle' font-family='Arial, sans-serif' font-weight='normal' font-size='14' fill='white'>{status}</text>
</svg>";
            var bytes = Encoding.UTF8.GetBytes(svg);
            return $"data:image/svg+xml;base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
