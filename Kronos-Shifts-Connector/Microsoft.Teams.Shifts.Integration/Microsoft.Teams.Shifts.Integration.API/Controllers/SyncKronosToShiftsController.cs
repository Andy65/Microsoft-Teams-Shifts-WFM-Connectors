﻿// <copyright file="SyncKronosToShiftsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;

    /// <summary>
    /// Sync Kronos To Shifts Controller.
    /// </summary>
    [Route("api/SyncKronosToShifts")]
    public class SyncKronosToShiftsController : ControllerBase
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IConfigurationProvider configurationProvider;
        private readonly OpenShiftController openShiftController;
        private readonly OpenShiftRequestController openShiftRequestController;
        private readonly SwapShiftController swapShiftController;
        private readonly TimeOffController timeOffController;
        private readonly TimeOffReasonController timeOffReasonController;
        private readonly TimeOffRequestsController timeOffRequestsController;
        private readonly ShiftController shiftController;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncKronosToShiftsController"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="configurationProvider">The Configuration Provider DI.</param>
        /// <param name="openShiftController">OpenShiftController DI.</param>
        /// <param name="openShiftRequestController">OpenShiftRequestController DI.</param>
        /// <param name="swapShiftController">SwapShiftController DI.</param>
        /// <param name="timeOffController">TimeOffController DI.</param>
        /// <param name="timeOffReasonController">TimeOffReasonController DI.</param>
        /// <param name="timeOffRequestsController">TimeOffRequestsController DI.</param>
        /// <param name="shiftController">ShiftController DI.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public SyncKronosToShiftsController(
            TelemetryClient telemetryClient,
            IConfigurationProvider configurationProvider,
            OpenShiftController openShiftController,
            OpenShiftRequestController openShiftRequestController,
            SwapShiftController swapShiftController,
            TimeOffController timeOffController,
            TimeOffReasonController timeOffReasonController,
            TimeOffRequestsController timeOffRequestsController,
            ShiftController shiftController,
            BackgroundTaskWrapper taskWrapper)
        {
            this.telemetryClient = telemetryClient;
            this.configurationProvider = configurationProvider;
            this.openShiftController = openShiftController;
            this.openShiftRequestController = openShiftRequestController;
            this.swapShiftController = swapShiftController;
            this.timeOffController = timeOffController;
            this.timeOffReasonController = timeOffReasonController;
            this.timeOffRequestsController = timeOffRequestsController;
            this.shiftController = shiftController;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// Http get method to start Kronos to Shifts sync.
        /// </summary>
        /// <param name="isRequestFromLogicApp">True if request is coming from logic app, false otherwise.</param>
        /// <returns>Returns Ok if request is successfully queued.</returns>
        [Authorize(Policy = "AppID")]
        [HttpGet]
        [Route("StartSync/{isRequestFromLogicApp}")]
        public ActionResult SyncKronosToShifts(string isRequestFromLogicApp)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "APIRoute", this.ControllerContext.ActionDescriptor.AttributeRouteInfo.Name },
            };

            this.telemetryClient.TrackTrace($"SyncKronosToShifts starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            this.taskWrapper.Enqueue(this.ProcessKronosToShiftsShiftsAsync(isRequestFromLogicApp));

            using (StringContent stringContent = new StringContent(string.Empty))
            {
                this.telemetryClient.TrackTrace($"SyncKronosToShifts ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
                return this.Ok(stringContent);
            }
        }

        /// <summary>
        /// Process all the entities from kronos to shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">true if the call is coming from logic app, false otherwise.</param>
        /// <returns>Returns task.</returns>
        private async Task ProcessKronosToShiftsShiftsAsync(string isRequestFromLogicApp)
        {
            try
            {
                this.telemetryClient.TrackTrace($"{Resource.ProcessKronosToShiftsShiftsAsync} start at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);

                // Sync open shifts from kronos to shifts.
                await this.openShiftController.ProcessOpenShiftsAsync(isRequestFromLogicApp).ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");

                // Sync open shifts requests from kronos to shifts.
                await this.openShiftRequestController.ProcessOpenShiftsRequests(isRequestFromLogicApp).ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");

                // Sync swap shifts from kronos to shifts.
                await this.swapShiftController.ProcessSwapShiftsAsync(isRequestFromLogicApp).ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.ProcessSwapShiftsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");

                // Sync timeoffreasons from kronos to shifts.
                await this.timeOffReasonController.MapPayCodeTimeOffReasonsAsync().ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.MapPayCodeTimeOffReasonsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");

                // Sync timeoff from kronos to shifts.
                await this.timeOffController.ProcessTimeOffsAsync(isRequestFromLogicApp).ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.ProcessTimeOffsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");

                // Sync timeoff from kronos to shifts.
                await this.timeOffRequestsController.ProcessTimeOffRequetsAsync(isRequestFromLogicApp).ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.SyncTimeOffRequestsFromShiftsToKronos} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");

                // Sync shifts from kronos to shifts.
                await this.shiftController.ProcessShiftsAsync(isRequestFromLogicApp).ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.ProcessShiftsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");

                this.telemetryClient.TrackTrace($"{Resource.ProcessKronosToShiftsShiftsAsync} completed at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}