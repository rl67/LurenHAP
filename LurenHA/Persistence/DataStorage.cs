using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.Common.Enums;
using InfluxData.Net.Common.Infrastructure;
using InfluxData.Net.InfluxDb.Models;
using InfluxData.Net.InfluxDb.Models.Responses;
using LASignals;

namespace LAPersistence
{
    public class DataStorage
    {
        private InfluxDbClient influxDbClient = null;
        private readonly string endpointUri = @"http://172.16.80.15:8086/";
        private readonly string username = "";
        private readonly string password = "";
        private readonly string m_database = "dotNet";
        private bool connected = false;

        /// <summary>
        /// Constructor for the class.
        /// </summary>
        public DataStorage() { }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public bool Connect()
        {
            bool success = false;

            try
            {
                influxDbClient = new InfluxDbClient(endpointUri, username, password, InfluxDbVersion.Latest);
                if (influxDbClient != null)
                {
                    connected = true;
                    success = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception in Connect()");
                logger.Debug(ex.Message);
                success = false;                
            }
            return success;
        }

        public async Task<bool> WriteDiToDb(IEnumerable<SignalDi> diSignals)
        {
            if (string.IsNullOrWhiteSpace(m_database)) throw new ArgumentNullException("database");
            if (diSignals == null || diSignals.Count() == 0) throw new ArgumentNullException("DI points can not be null, or empty.");

            var dataSet = new List<Point>(diSignals.Count());

            foreach (var diSignal in diSignals)
            {
                var pointToWrite = new Point()
                {
                    Name = "DI",
                    Tags = new Dictionary<string, object>()
                    {
                        { "SensorID", 5 },
/*                        { "SensorType", "diSignal.SensorType" },*/
                        { "SensorType", "relay" },
                        { "Meas", diSignal.Description }
                    },
                    Fields = new Dictionary<string, object>()
                    {
                        { "value", Convert.ToSingle(diSignal.Input.Value) },
                        { "status", (float)diSignal.Input.Status }
                    }
                };
                dataSet.Add(pointToWrite);
            }
            var response = influxDbClient.Client.WriteAsync(dataSet, m_database).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> WriteAiToDb(IEnumerable<SignalAi> aiSignals)
        {
            if (string.IsNullOrWhiteSpace(m_database)) throw new ArgumentNullException("database");
            if (aiSignals == null || aiSignals.Count() == 0) throw new ArgumentNullException("AI oints can not be null, or empty.");

            var dataSet = new List<Point>(aiSignals.Count());

            foreach (var aiSignal in aiSignals)
            {
                var pointToWrite = new Point()
                {
                    Name = "AI",
                    Tags = new Dictionary<string, object>()
                    {
                        { "SensorID", 7},
                        { "SensorType", aiSignal.SensorType },
                        { "Meas", aiSignal.Description }
                    },
                    Fields = new Dictionary<string, object>()
                    {
                        { "value", (float)aiSignal.Input.Value },
/*                        { "status", (float)aiSignal.Input.Status }*/
                        { "status", (float)192 }
                    }
                };
                dataSet.Add(pointToWrite);
            }
            var response = await influxDbClient.Client.WriteAsync(dataSet, m_database).ConfigureAwait(false);
            return true;
            /*
            if (string.IsNullOrWhiteSpace(database)) throw new ArgumentNullException("database");
            if (aiSignals == null || aiSignals.Count() == 0) throw new ArgumentNullException("Points can not be null or empty.");

            var list = new List<Point>(aiSignals.Count());

            foreach (var aiSignal in aiSignals)
            {
                var pointToWrite = new Point()
                {
                    Name = measurement,
                    Tags = new Dictionary<string, object>()
                    {
                        { "sensorId", 7 },
                        { "sensorType", aiSignal.SensorType }
                    },
                    Fields = new Dictionary<string, object>()
                    {
                        { aiSignal.Description, (float)aiSignal.Input.Value }
                    }
                };
                list.Add(pointToWrite);
            }
            var response = await influxDbClient.Client.WriteAsync(list, database).ConfigureAwait(false);
            return true;
            */
        }

        public async Task<bool> WriteDataToDbGolvTank(string idesc, double ival)
        {
            bool success = false;

            var pointToWrite = new Point()
            {
                Name = "golv_tank",             // measurement name
                Tags = new Dictionary<string, object>()
                {
                    { "sensorId", 7 },
                    { "sensorType", "18C20" }
                },
                Fields = new Dictionary<string, object>()
                {
                    { idesc, (float)ival }
                }
            };

            try
            {
                var response = await influxDbClient.Client.WriteAsync(pointToWrite, "rlPlay");
            }
            catch(Exception ex)
            {
                logger.Error("Exception when writing to database!");
                logger.Debug(ex.Message);
            }
            return true;
        }

        public async Task<bool> WriteDataToDbSewagePump(string idesc, bool iPumpRunning)
        {
            bool success = false;

            var pointToWrite = new Point()
            {
                Name = "kloakkPumpe",             // measurement name
                Tags = new Dictionary<string, object>()
                {
                    { "source", "Barionet1" }
                },
                Fields = new Dictionary<string, object>()
                {
                    { idesc, Convert.ToSingle(iPumpRunning) }
                }
            };

            try
            {
                var response = await influxDbClient.Client.WriteAsync(pointToWrite, "rlPlay");
            }
            catch (Exception ex)
            {
                logger.Error("Exception when writing to database!");
                logger.Debug(ex.Message);
            }
            return true;
        }
    }
}
