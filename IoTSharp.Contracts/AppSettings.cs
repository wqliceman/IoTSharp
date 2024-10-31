﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace IoTSharp.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TelemetryStorage
    {
        SingleTable,
        Sharding,
        Taos,
        InfluxDB,
        PinusDB,
        TimescaleDB,
        IoTDB
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventBusStore
    {
        PostgreSql,
        MongoDB,
        InMemory,
        LiteDB,
        MySql,
        SqlServer
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventBusMQ
    {
        RabbitMQ,
        Kafka,
        InMemory,
        ZeroMQ,
        NATS,
        Pulsar,
        RedisStreams,
        AmazonSQS,
        AzureServiceBus
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CachingUseIn
    {
        InMemory,
        Redis,
        LiteDB,
        SQlite
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventBusFramework
    {
        CAP,
        Shashlik,
    }

    public class AppSettings
    {
        public string JwtKey { get; set; }
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
        public double JwtExpireHours { get; set; }

        [DefaultValue(EventBusFramework.CAP)]
        public EventBusFramework EventBus { get; set; } = EventBusFramework.CAP;

        /// <summary>
        /// Broker settings
        /// </summary>
        public MqttBrokerSetting MqttBroker { get; set; } = new MqttBrokerSetting();

        /// <summary>
        /// mqtt client settings
        /// </summary>
        public MqttClientSetting MqttClient { get; set; } = new MqttClientSetting() { MqttBroker = "built-in", UserName = Guid.NewGuid().ToString(), Password = Guid.NewGuid().ToString(), Port = 1883 };

        public Dictionary<string, string> ConnectionStrings { get; set; }

        public ModBusServerSetting ModBusServer { get; set; } = new ModBusServerSetting();

        public TelemetryStorage TelemetryStorage { get; set; } = TelemetryStorage.SingleTable;

        public EventBusStore EventBusStore { get; set; } = EventBusStore.InMemory;
        public EventBusMQ EventBusMQ { get; set; } = EventBusMQ.InMemory;
        public int ConsumerThreadCount { get; set; } = Environment.ProcessorCount;
        public int DbContextPoolSize { get; set; } = 128;
        public CachingUseIn CachingUseIn { get; set; } = CachingUseIn.InMemory;
        public string CachingUseRedisHosts { get; set; }

        //public DiscoveryOptions Discovery { get; set; } = null;
        //public ZMQOption ZMQOption { get; set; } = null;
        public int SucceedMessageExpiredAfter { get; set; } = 3600 * 6;

        public DataBaseType DataBase { get; set; } = DataBaseType.PostgreSql;
        public int RuleCachingExpiration { get; set; } = 60;
        public ShardingByDateMode ShardingByDateMode { get; set; } = ShardingByDateMode.PerMonth;

        private DateTime shardingBeginTime;

        public DateTime ShardingBeginTime
        {
            get
            {
                //shardingBeginTime没值时给默认值
                if (shardingBeginTime == DateTime.MinValue || shardingBeginTime.Year <= 1970)
                {
                    switch (ShardingByDateMode)
                    {
                        case ShardingByDateMode.PerMinute:
                            shardingBeginTime = DateTime.UtcNow.AddMinutes(-5);
                            break;

                        case ShardingByDateMode.PerHour:
                            shardingBeginTime = DateTime.UtcNow.AddHours(-1);
                            break;

                        case ShardingByDateMode.PerDay:
                            shardingBeginTime = DateTime.UtcNow.AddDays(-1).Date;
                            break;

                        case ShardingByDateMode.PerMonth:
                            shardingBeginTime = DateTime.UtcNow.AddMonths(-1).Date;
                            break;

                        case ShardingByDateMode.PerYear:
                            shardingBeginTime = DateTime.UtcNow.AddYears(-1).Date;
                            break;

                        default:
                            shardingBeginTime = DateTime.UtcNow.Date;
                            break;
                    }
                }
                return shardingBeginTime;
            }
            set
            {
                shardingBeginTime = value;
            }
        }

        public string RootKey { get; set; }
    }

    public enum ShardingByDateMode
    {
        //
        // 摘要:
        //     每分钟
        PerMinute,

        //
        // 摘要:
        //     每小时
        PerHour,

        //
        // 摘要:
        //     每天
        PerDay,

        //
        // 摘要:
        //     每月
        PerMonth,

        //
        // 摘要:
        //     每年
        PerYear
    }

    public class ModBusServerSetting
    {
        public int Port { get; set; } = 502;
        public int TimeOut { get; set; } = 120000;
    }

    public class MqttClientSetting
    {
        /// <summary>
        /// built-in or IP、HostName
        /// </summary>
        public string MqttBroker { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
    }
}