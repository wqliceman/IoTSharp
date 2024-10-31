namespace IoTSharp.Models.Rule.Params
{
    public abstract class InputParam
    {
        public string DeviceId { get; set; }
    }

    public class DemoParam : InputParam
    {
        public int Temperature { get; set; }
        public int humidity { get; set; }
    }
}