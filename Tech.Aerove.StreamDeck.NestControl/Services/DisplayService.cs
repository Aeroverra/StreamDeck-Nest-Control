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

        public async Task DisplayCombined(IActionDispatcher dispatcher, GoogleHomeEnterpriseSdmV1Device? thermostat, bool isDial = false)
        {
            if (thermostat == null)
                return;

            var temp = thermostat.GetThermostatRenderedTemperature(TemperatureScale.FAHRENHEIT);
            var thermostatMode = thermostat.GetThermostatMode();
            var setPointRender = thermostat.GetThermostatRenderedSetPoint(TemperatureScale.FAHRENHEIT);

            var modeText = "";
            
            if (thermostatMode.Mode == ThermostatMode.COOL)
            {
                await dispatcher.SetStateAsync(1);
                await dispatcher.SetImageAsync(ImageColors.Blue.DataUri);
                modeText = $"{thermostatMode.Mode} {setPointRender}";
            }
            else if (thermostatMode.Mode == ThermostatMode.HEAT)
            {
                await dispatcher.SetStateAsync(1);
                await dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                modeText = $"{thermostatMode.Mode} {setPointRender}";
            }
            else if (thermostatMode.Mode == ThermostatMode.HEATCOOL)
            {
                await dispatcher.SetStateAsync(1);
                await dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                modeText = $"H&C {setPointRender}";
            }
            else if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                await dispatcher.SetStateAsync(0);
                await dispatcher.SetImageAsync("");
                modeText = $"{thermostatMode.Mode}";
            }

            var combinedText = $"{temp}\n{modeText}";
            await dispatcher.SetTitleAsync(combinedText);

            if (isDial)
            {
                await dispatcher.SetFeedbackAsync(new
                {
                    value = combinedText
                });
            }
        }
    }
}
