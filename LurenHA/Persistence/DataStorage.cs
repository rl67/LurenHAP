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

namespace LAPersistence
{
    public class DataStorage
    {
        private InfluxDbClient influxDbClient = null;
        private readonly string endpointUri = @"http://172.16.80.15:8086/";
        private readonly string username = "";
        private readonly string password = "";
        private readonly string databaseName = "dotNet";
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
