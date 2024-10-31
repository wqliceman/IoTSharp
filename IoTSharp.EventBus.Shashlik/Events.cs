using IoTSharp.Contracts;
using IoTSharp.Data;
using Shashlik.EventBus;

namespace IoTSharp.EventBus.Shashlik
{
    public class AttributeDataEvent : ShashlikEvent<PlayloadData>
    {
    }

    public class TelemetryDataEvent : ShashlikEvent<PlayloadData>
    {
    }

    public class CreateDeviceEvent : IEvent
    {
        public Guid DeviceId { get; set; }
    }

    public class DeleteDeviceEvent : IEvent
    {
        public Guid DeviceId { get; set; }
    }

    public class AlarmEvent : ShashlikEvent<CreateAlarmDto>
    {
    }

    public class DeviceActivityEvent : ShashlikEvent<DeviceActivityStatus>
    {
    }

    public class DeviceConnectEvent : ShashlikEvent<DeviceConnectStatus>
    {
    }
}