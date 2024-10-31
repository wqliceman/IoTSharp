namespace IoTSharp.Data
{
    public class ProduceData : DataStorage
    {
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Produce Owner { get; set; }
    }
}