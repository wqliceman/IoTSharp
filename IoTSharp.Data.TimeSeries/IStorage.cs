﻿using IoTSharp.Contracts;
using IoTSharp.Data;

namespace IoTSharp.Storage
{
    public interface IStorage
    {
        Task<bool> CheckTelemetryStorage();

        Task<(bool result, List<TelemetryData> telemetries)> StoreTelemetryAsync(PlayloadData msg);

        Task<List<TelemetryDataDto>> GetTelemetryLatest(Guid deviceId);

        Task<List<TelemetryDataDto>> GetTelemetryLatest(Guid deviceId, string keys);

        Task<List<TelemetryDataDto>> LoadTelemetryAsync(Guid deviceId, string keys, DateTime begin, DateTime end, TimeSpan every, Aggregate aggregate);
    }
}