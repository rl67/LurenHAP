using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using ABB.PAConnector.Common.DataContracts.EquipmentConfig;
using ABB.PAConnector.Common.DataContracts.EquipmentData;
using ABB.PAConnector.Common.DataContracts.FieldPlugConfig;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.Common.Enums;
using InfluxData.Net.Common.Infrastructure;
using InfluxData.Net.InfluxDb.Models;
using InfluxData.Net.InfluxDb.Models.Responses;
using Newtonsoft.Json;
using ABB.PAConnector.Common.DataContracts;
using ABB.API.LibLog.Logging;

namespace ABB.API.DataStorage
{
    /// <summary>
    /// A class for handling of storage / retrieval of configuration data in MongoDB
    /// </summary>
    public class MongoDbClientWrapper
    {
        private IMongoDatabase db = null;
        private Boolean connected = false;
        private string ipAddress = "localhost";
        private string portNumber = "27017";
        private string username = "";
        private string password = "";
        private string databaseName = "MSMQ";

        public string mdbEquipmentConfigCollectionName { get; set; }
        public string mdbEquipmentDataCollectionName { get; set; }
        public string mdbFieldPlugConfigCollectionName { get; set; }
        private static readonly ILog log = LogProvider.GetCurrentClassLogger();

        /// <summary>
        /// Constructor for the class.
        /// </summary>
        public MongoDbClientWrapper()
        {
            mdbEquipmentConfigCollectionName = "EquipmentConfig";
            mdbEquipmentDataCollectionName = "EquipmentData";
            mdbFieldPlugConfigCollectionName = "FieldPlugConfig";
            initBsonClassMappingForSerialization();
        }

        /// <summary>
        /// Connect to underlying database.
        /// </summary>
        public Boolean Connect(string mdbIpAddr, string mdbPort, string mdbUserName, string mdbPassword, string mdbName)
        {
            ipAddress = mdbIpAddr;
            portNumber = mdbPort;
            username = mdbUserName;
            password = mdbPassword;
            databaseName = mdbName;

            //string connectString = "mongodb://" + ipAddress + ":" + port;
            string connectString = "mongodb://" + mdbIpAddr + ":" + mdbPort + "/?safe=true;uuidRepresentation=Standard";
            try
            {
                var client = new MongoClient(connectString);
                db = client.GetDatabase(databaseName);
                overrideMongoDbDriverDefaults();
                connected = true;
            }
            catch (Exception ex)
            {
                log.Error("Exception in Connect(), ConnectString: " + connectString);
                log.Debug(ex.ToString());
                connected = false;
            }

            return true;
        }

        /// <summary>
        /// Set MongoDB Driver configuration parameters
        /// Note: http://3t.io/blog/best-practices-uuid-mongodb/
        /// </summary>
        private void overrideMongoDbDriverDefaults()
        {
            // Default value for GuidRepresentation is CSharpLegacy, but this is not working when GUIDs are converted
            // to binary form when GUIDs are used in BsonDocument _id field. It is important to change this parameter to Standard.
            MongoDB.Driver.MongoDefaults.GuidRepresentation = MongoDB.Bson.GuidRepresentation.Standard;

        }

        /// <summary>
        /// Disconnect from database.
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool Disconnect()
        {
            connected = false;

            return true;
        }

        /// <summary>
        /// Check if connected to database.
        /// </summary>
        /// <returns>True if connected, else false</returns>
        public bool IsConnected()
        {
            return connected;
        }

        /// <summary>
        /// Insert new or replace existing equipment configuration document in database. 
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool insertEquipmentConfigIntoMdb(MdbEquipmentConfiguration mdbEquipmentConfig)
        {
            bool success = false;

            try
            {
                if (IsConnected())
                {
                    var col = db.GetCollection<MdbEquipmentConfiguration>(mdbEquipmentConfigCollectionName);
                    if (col != null)
                    {
                        // Check if configuration already exist
                        var filterIndex = Builders<MdbEquipmentConfiguration>.Filter.Eq("_id", mdbEquipmentConfig.ID);
                        var results = col.Find(filterIndex).ToList();

                        // Replace existing configuration
                        if (results.Count > 0)
                        {
                            col.ReplaceOne(filterIndex, mdbEquipmentConfig);
                        }
                        else
                        {
                            // Insert new configuration
                            col.InsertOne(mdbEquipmentConfig);
                        }

                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in insertEquipmentConfigIntoMdb()");
                log.Debug(ex.ToString());
            }
            return success;
        }

        /*public bool insertEquipmentConfigIntoMdb(EquipmentConfiguration equipmentConfig)
        {
            bool success = false;

            try
            {
                if (IsConnected())
                {
                    var col = db.GetCollection<EquipmentConfiguration>(mdbEquipmentConfigCollectionName);
                    if (col != null)
                    {
                        // Check if configuration already exist
                        var filterIndex = Builders<EquipmentConfiguration>.Filter.Eq("_id", equipmentConfig.ID);
                        var results = col.Find(filterIndex).ToList();

                        // Replace existing configuration
                        if (results.Count > 0)
                        {
                            col.ReplaceOne(filterIndex, equipmentConfig);
                        }
                        else
                        {
                            // Insert new configuration
                            col.InsertOne(equipmentConfig);
                        }

                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in insertEquipmentConfigIntoMdb()");
                log.Debug(ex.ToString());
            }
            return success;
        }*/

        /// <summary>
        /// Read one spesific equipment configuration object from Mdb.
        /// </summary>
        /// <returns>MdbEquipmentConfiguration or null if not found</returns>
        public MdbEquipmentConfiguration GetEquipmentConfigFromMdb(Guid eqConfigId)
        {
            MdbEquipmentConfiguration config = null;

            try
            {
                var col = db.GetCollection<MdbEquipmentConfiguration>(mdbEquipmentConfigCollectionName);
                if (col != null)
                {
                    var filter = Builders<MdbEquipmentConfiguration>.Filter.Eq("_id", eqConfigId);
                    config = col.Find(filter).First();
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetEquipmentConfigFromMdb(), EqConfigId: " + eqConfigId.ToString());
                log.Debug(ex.ToString());
            }
            return config;
        }

        /// <summary>
        /// Read all equipment configuration objects from database.
        /// </summary>
        /// <returns>List of MdbEquipmentConfiguration</returns>
        public List<MdbEquipmentConfiguration> GetEquipmentConfigFromMdb()
        {
            List<MdbEquipmentConfiguration> config = null;

            try
            {
                var col = db.GetCollection<MdbEquipmentConfiguration>(mdbEquipmentConfigCollectionName);
                if (col != null)
                {
                    var filter = Builders<MdbEquipmentConfiguration>.Filter.Empty;
                    config = col.Find(filter).ToList();
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetEquipmentConfigFromMdb()");
                log.Debug(ex.ToString());
            }
            return config;
        }

        /*public List<EquipmentConfiguration> GetEquipmentConfigFromMdb_v2()
        {
            List<EquipmentConfiguration> config = null;

            try
            {
                var col = db.GetCollection<EquipmentConfiguration>(mdbEquipmentConfigCollectionName);
                if (col != null)
                {
                    var filter = Builders<EquipmentConfiguration>.Filter.Empty;
                    config = col.Find(filter).ToList();
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetEquipmentConfigFromMdb_v2()");
                log.Debug(ex.ToString());
            }
            return config;
        }*/

        /// <summary>
        /// Insert new or replace existing FieldPlug configuration document in database. 
        /// </summary>
        /// <returns>True if successful, else false</returns>
        /*public bool insertFieldPlugConfigIntoMdb(FieldPlugConfig mdbFieldPlugConfig)
        {
            bool success = false;

            try
            {
                if (IsConnected())
                {
                    var col = db.GetCollection<FieldPlugConfig>(mdbFieldPlugConfigCollectionName);

                    if (col != null)
                    {
                        // Check if configuration already exist
                        var filter = Builders<FieldPlugConfig>.Filter.Eq("_id", mdbFieldPlugConfig.Source);
                        var results = col.Find(filter).ToList();

                        // Replace existing configuration
                        if (results.Count > 0)
                        {
                            col.ReplaceOne(filter, mdbFieldPlugConfig);
                        }
                        else
                        {
                            // Insert new configuration
                            col.InsertOne(mdbFieldPlugConfig);
                        }

                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in insertFieldPlugConfigIntoMdb()");
                log.Debug(ex.ToString());
            }

            return success;
        }*/
        public bool insertFieldPlugConfigIntoMdb(MdbFieldPlugConfig mdbFieldPlugConfig)
        {
            bool success = false;

            try
            {
                if (IsConnected())
                {
                    var col = db.GetCollection<MdbFieldPlugConfig>(mdbFieldPlugConfigCollectionName);

                    if (col != null)
                    {
                        // Check if configuration already exist
                        var filter = Builders<MdbFieldPlugConfig>.Filter.Eq("_id", mdbFieldPlugConfig.FieldPlugID);
                        var results = col.Find(filter).ToList();

                        // Replace existing configuration
                        if (results.Count > 0)
                        {
                            col.ReplaceOne(filter, mdbFieldPlugConfig);
                        }
                        else
                        {
                            // Insert new configuration
                            col.InsertOne(mdbFieldPlugConfig);
                        }
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in insertFieldPlugConfigIntoMdb()");
                log.Debug(ex.ToString());
            }
            return success;
        }

        /// <summary>
        /// Read FieldPlug configuration for from database.
        /// </summary>
        /// <returns>One FieldPlugConfig object</returns>
        /*public FieldPlugConfig GetFieldPlugConfigFromMdb(string source)
        {
            FieldPlugConfig config = null;

            try
            {
                if (IsConnected())
                {
                    var col = db.GetCollection<FieldPlugConfig>(mdbFieldPlugConfigCollectionName);
                    if (col != null)
                    {
                        var filter = Builders<FieldPlugConfig>.Filter.Eq("_id", source);
                        config = col.Find(filter).First();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetFieldPlugConfigFromMdb()");
                log.Debug(ex.ToString());
            }
            return config;
        }*/
        public MdbFieldPlugConfig GetFieldPlugConfigFromMdb(Guid fieldPlugID)
        {
            MdbFieldPlugConfig mdbFpConfig = null;

            try
            {
                if (IsConnected())
                {
                    var col = db.GetCollection<MdbFieldPlugConfig>(mdbFieldPlugConfigCollectionName);
                    if (col != null)
                    {
                        var filter = Builders<MdbFieldPlugConfig>.Filter.Eq("_id", fieldPlugID);
                        //mdbFpConfig = col.Find(filter).First();
                        List<MdbFieldPlugConfig> mdbFpConfigList = col.Find(filter).ToList();
                        if (mdbFpConfigList != null && mdbFpConfigList.Count > 0)
                        {
                            mdbFpConfig = mdbFpConfigList.First();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetFieldPlugConfigFromMdb( fieldPlugId: " + fieldPlugID.ToString() + " )");
                log.Debug(ex.ToString());
            }
            return mdbFpConfig;
        }

        /// <summary>
        /// Read all FieldPlug configuration objects from database.
        /// </summary>
        /// <returns>List of MdbEquipmentConfiguration</returns>
        public List<MdbFieldPlugConfig> GetFieldPlugConfigFromMdb()
        {
            List<MdbFieldPlugConfig> mdbFpConfigList = null;

            try
            {
                if (IsConnected())
                {
                    var col = db.GetCollection<MdbFieldPlugConfig>(mdbFieldPlugConfigCollectionName);
                    if (col != null)
                    {
                        var filter = Builders<MdbFieldPlugConfig>.Filter.Empty;
                        mdbFpConfigList = col.Find(filter).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetFieldPlugConfigFromMdb()");
                log.Debug(ex.ToString());
            }
            return mdbFpConfigList;
        }

        /// <summary>
        /// Define mapping rules between .net objects and corresponding 
        /// BSON documents to be used by BSON serializer/deserializer
        public void initBsonClassMappingForSerialization()
        {
            try
            {
                BsonClassMap.RegisterClassMap<MdbFieldPlugConfig>(cm =>
                {
                    cm.MapIdMember(c => c.FieldPlugID);
                    cm.AutoMap();
                });
                BsonClassMap.RegisterClassMap<FieldPlugConfig>(cm =>
                {
                    //cm.MapIdMember(c => c.Source);
                    //cm.MapMember(c => c.Config);
                    cm.AutoMap();
                });
                BsonClassMap.RegisterClassMap<FieldPlugConfigItem>(cm =>
                {
                    cm.AutoMap();
                });

                BsonClassMap.RegisterClassMap<EquipmentConfiguration>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(c => c.ID);
                    cm.UnmapMember(c => c.Hierarchy);
                    cm.UnmapMember(c => c.Instances);
                    cm.UnmapMember(c => c.Types);
                });
                BsonClassMap.RegisterClassMap<EquipmentType>(cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(c => c.SeriesDefinitions);
                });
                BsonClassMap.RegisterClassMap<HierarchyNode>(cm =>
                {
                    cm.AutoMap();
                });
                BsonClassMap.RegisterClassMap<Equipment>(cm =>
                {
                    cm.AutoMap();
                });
                BsonClassMap.RegisterClassMap<DataSeriesDefinition>(cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(c => c.DataType);
                });

                BsonClassMap.RegisterClassMap<MdbEquipmentConfiguration>(cm =>
                {
                    cm.AutoMap();
                });
            }
            catch(Exception ex)
            {
                string text = ex.ToString();
                log.Error("Exception in MongoDbClientWrapper.initBsonClassMappingForSerialization: " + text);
            }
        }
    }

    /// <summary>
    /// A class for handling of storage / retrieval of historical data in InfluxDB
    /// </summary>
    public class InfluxDbClientWrapper
    {
        private InfluxDbClient influxDbClient = null;
        private string endpointUri = @"http://localhost:8086/";
        private string username = "";
        private string password = "";
        private string databaseName = "history";
        private string retentionName;
        private string retentionDuration;
        private string retentionShardDuration;
        private bool connected = false;
        private const string queryDateTimeFormat = "yyyy-MM-dd HH':'mm':'ss.fff";
        private static readonly ILog log = LogProvider.GetCurrentClassLogger();

        /// <summary>
        /// Constructor for the class.
        /// </summary>
        public InfluxDbClientWrapper() { }

        /// <summary>
        /// Connect to InfluxDB  
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool Connect(string idbIpAddr, string idbPort, string idbUserName, string idbPassword, string idbName = null)
        {
            bool success = false;
            
            try
            {
                endpointUri = "http://" + idbIpAddr + ":" + idbPort + "/";
                username = idbUserName;
                password = idbPassword;
                if (idbName != null)
                {
                    databaseName = idbName;
                }

                influxDbClient = new InfluxDbClient(endpointUri, username, password, InfluxDbVersion.v_1_0_0);
                if (influxDbClient != null)
                {
                    connected = true;
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in Connect(), EndpointUri: " + endpointUri);
                log.Debug(ex.ToString());
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Connect to InfluxDB  
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool Connect()
        {
            bool success = false;

            try
            {
                influxDbClient = new InfluxDbClient(endpointUri, username, password, InfluxDbVersion.v_1_0_0);
                if (influxDbClient != null)
                {
                    connected = true;
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in Connect()");
                log.Debug(ex.ToString());
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Initialize InfluxDB history database and related retension policy 
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool InitHistoryDatabase(string idbName, string idbRetentionName, string idbRetentionDuration, string idbRetentionShardDuration)
        {
            bool success = true;

            databaseName = idbName;
            retentionName = idbRetentionName;
            retentionDuration = idbRetentionDuration;
            retentionShardDuration = idbRetentionShardDuration;

            // Create history database in InfluxDb if it does not exist
            bool idbExist = ExistHistoryDatabase(databaseName);
            if (idbExist == false)
            {
                bool dbCreated = CreateHistoryDatabase(databaseName);
                if (dbCreated == false)
                {
                    log.Error(String.Format("CreateHistoryDatabase(\"{0}\") failed", databaseName));
                    success = false;
                }
            }

            // Create retention policy for history database if it does not exist, or alter an existing one
            bool policyExist = ExistHistoryDatabaseRetentionPolicy(databaseName, idbRetentionName);
            if (policyExist == false)
            {
                bool policyCreated = CreateHistoryDatabaseRetentionPolicy(databaseName, retentionName, retentionDuration, retentionShardDuration);
                if (policyCreated == false)
                {
                    log.Error(String.Format("Failed to create retention polecy idbName: {0}, idbName: {1}, idbDuration: {2}, idbShardDuration: {3}",
                        databaseName, retentionName, retentionDuration, retentionShardDuration));
                    success = false;
                }
            }
            else
            {
                bool policyAltered = AlterHistoryDatabaseRetentionPolicy(databaseName, retentionName, retentionDuration, retentionShardDuration);
                if (policyAltered == false)
                {
                    log.Error(String.Format("Failed to alter retention polecy idbName: {0}, idbName: {1}, idbDuration: {2}, idbShardDuration: {3}",
                        idbName, idbRetentionName, idbRetentionDuration, idbRetentionShardDuration));
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Disconnect from InfluxDB  
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool Disconnect()
        {
            connected = false;
            return true;
        }

        /// <summary>
        /// Check if connected to InfluxDB  
        /// </summary>
        /// <returns>True if connected, else false</returns>
        public bool IsConnected()
        {
            return connected;
        }

        /// <summary>
        /// Insert time-series data points into InfluxDB for equipment property
        /// </summary>
        /// <returns>True if successful, else false</returns>
        /*public bool insertHistoryDataPoints(Guid eqId, Guid propId, List<MdbEqPropDataPoint> dataPoints)
        {
            bool success = false;
            var idbPoints = new List<Point>();

            try
            {
                foreach (var dp in dataPoints)
                {
                    var idbPoint = new Point();
                    idbPoint.Name = eqId.ToString() + "_" + propId.ToString();
                    idbPoint.Fields = new Dictionary<string, object>()
                    {
                        { "Value", dp.Value },
                        { "Quality", dp.Quality }
                    };
                    idbPoint.Timestamp = dp.TimeStamp.ToUniversalTime();
                    idbPoints.Add(idbPoint);
                }

                var result = influxDbClient.Client.WriteAsync(idbPoints, databaseName).GetAwaiter().GetResult();
                if (result.Success)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in insertHistoryDataPoints()");
                log.Debug(ex.ToString());
            }
            return success;
        }*/

        /// <summary>
        /// Insert time-series data points into InfluxDB for equipment property
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool InsertHistoryDataPoints(EquipmentData equipmentData)
        {
            bool success = true;
            int batchSize = 100;
            var idbPoints = new List<Point>(batchSize);

            try
            {
                foreach (DataSeries dataSeries in equipmentData.Series)
                {
                    string idbPointName = dataSeries.EquipmentID.ToString() + "_" + dataSeries.SeriesDefinitionID.ToString();

                    // Values with data type not supported by InfluxDb are serialized to json
                    if (!SupportedInfluxDbDataType(dataSeries.DataTypeCode))
                    {
                        foreach (DataPoint dataPoint in dataSeries.Points)
                        {
                            var idbPoint = new Point()
                            {
                                Name = idbPointName,
                                Fields = new Dictionary<string, object>()
                                {
                                    { "Value", SerializeObjectToJson(dataPoint.Value) },
                                    { "Quality", dataPoint.Quality }
                                },
                                Timestamp = dataPoint.TimeStamp.ToUniversalTime()
                            };
                            idbPoints.Add(idbPoint);
                        }
                    }
                    else
                    {
                        foreach (DataPoint dataPoint in dataSeries.Points)
                        {
                            var idbPoint = new Point()
                            {
                                Name = idbPointName,
                                Fields = new Dictionary<string, object>()
                                {
                                    { "Value", dataPoint.Value },
                                    { "Quality", dataPoint.Quality }
                                },
                                Timestamp = dataPoint.TimeStamp.ToUniversalTime()
                            };
                            idbPoints.Add(idbPoint);
                        }
                    }
                    if (idbPoints.Count > batchSize)
                    {
                        var result = influxDbClient.Client.WriteAsync(idbPoints, databaseName).GetAwaiter().GetResult();
                        if (!result.Success)
                        {
                            success &= false;
                        }
                        idbPoints.Clear();
                    }
                }
                if (idbPoints.Count > 0)
                {
                    var result2 = influxDbClient.Client.WriteAsync(idbPoints, databaseName).GetAwaiter().GetResult();
                    if (!result2.Success)
                    {
                        success &= false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in InsertHistoryDataPoints()");
                log.Debug(ex.ToString());
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Check if data type is supported by InfluxDb 
        /// </summary>
        /// <returns>True if supported, else false</returns>
        public bool SupportedInfluxDbDataType(int dataTypeCode)
        {
            switch (dataTypeCode)
            {
                case DataContractsHelper.DataTypeBool:
                case DataContractsHelper.DataTypeChar:
                case DataContractsHelper.DataTypeSByte:
                case DataContractsHelper.DataTypeByte:
                case DataContractsHelper.DataTypeInt16:
                case DataContractsHelper.DataTypeUInt16:
                case DataContractsHelper.DataTypeInt32:
                case DataContractsHelper.DataTypeUInt32:
                case DataContractsHelper.DataTypeInt64:
                case DataContractsHelper.DataTypeUInt64:
                case DataContractsHelper.DataTypeSingle:
                case DataContractsHelper.DataTypeDouble:
                case DataContractsHelper.DataTypeDecimal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Read time-series data points from InfluxDB for one equipment property and specified time period
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public List<MdbEqPropDataPoint> GetHistoryDataPoints(Guid eqId, Guid propId, DateTime fromTime, DateTime toTime)
        {
            List<MdbEqPropDataPoint> dataPointList = null;
            string logName = eqId.ToString() + "_" + propId.ToString();
            string query = string.Format("SELECT \"Quality\", \"Value\" FROM \"{0}\" WHERE time >= '{1}' and time <= '{2}'",
                logName, fromTime.ToString(queryDateTimeFormat), toTime.ToString(queryDateTimeFormat));

            try
            {
                var result = influxDbClient.Client.QueryAsync(query, databaseName).GetAwaiter().GetResult();
                if (result != null)
                {
                    const int timeColumn = 0;
                    const int qualityColumn = 1;
                    const int valueColumn = 2;
                    dataPointList = new List<MdbEqPropDataPoint>(result.Count());
                    
                    foreach (var series in result)
                    {
                        foreach (var value in series.Values)
                        {
                            var dataPoint = new MdbEqPropDataPoint()
                            {
                                TimeStamp = (DateTime)value[timeColumn],
                                Value = value[valueColumn],
                                Quality = Convert.ToUInt32(value[qualityColumn])
                            };
                            dataPointList.Add(dataPoint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetHistoryDataPoints(), Query: " + query);
                log.Debug(ex.ToString());
            }
            return dataPointList;
        }

        /// <summary>
        /// Get list of all measurement names in the history database
        /// </summary>
        /// <returns>Return List<string></string></returns>
        public List<string> GetMeasurementNames()
        {
            List<string> measNameList = new List<string>();
            string query = string.Format("show series");
            const int measName = 0;

            try
            {
                var result = influxDbClient.Client.QueryAsync(query, databaseName).GetAwaiter().GetResult();
                if (result != null)
                {
                    var series = result.First().Values;
                    foreach (var meas in series)
                    {
                        measNameList.Add((string)meas[measName]);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetMeasurementNames()");
                log.Debug(ex.ToString());
            }
            return measNameList;
        }

        /// <summary>
        /// Get last data point from InfluxDB for one measurement 
        /// </summary>
        /// <returns>Return MdbEqPropDataPoint, else null if measurement not found</returns>
        /*public MdbEqPropDataPoint GetLastHistoryDataPoint(Guid eqId, Guid propId)
        {
            MdbEqPropDataPoint lastDataPoint = null;
            string logName = eqId.ToString() + "_" + propId.ToString();
            string query = string.Format("SELECT last(\"Quality\") as Quality, last(\"Value\") as Value FROM \"{0}\"", logName);
            const int timeColumn = 0;
            const int qualityColumn = 1;
            const int valueColumn = 2;

            try
            {
                var result = influxDbClient.Client.QueryAsync(query, databaseName).GetAwaiter().GetResult();
                if (result != null)
                {
                    var value = result.First().Values.First();
                    lastDataPoint = new MdbEqPropDataPoint()
                    {
                        TimeStamp = (DateTime)value[timeColumn],
                        Value = value[valueColumn],
                        Quality = Convert.ToUInt32(value[qualityColumn])
                    };
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetLastHistoryDataPoint()");
                log.Debug(ex.ToString());
            }
            return lastDataPoint;
        }*/

        /// <summary>
        /// Get last data point from InfluxDB for a list of measurement names 
        /// </summary>
        /// <returns>Return Dictionary<string,MdbEqPropDataPoint>, where key is InfluxDB measurement name</returns>
        public Dictionary<string, MdbEqPropDataPoint> GetLastHistoryDataPoint(List<string> measurementNames)
        {
            var lastDataPointDict = new Dictionary<string, MdbEqPropDataPoint>();
            const int timeColumn = 0;
            const int valueColumn = 1;
            const int qualityColumn = 2;
            const int batchSize = 100;

            try
            {
                // Get range of measurement list to avoid too long queary string
                int measurementCount = measurementNames.Count;
                for (int i = 0; i < measurementCount; i += batchSize)
                {
                    List<string> measurementNamesInBatch = measurementNames.GetRange(i, Math.Min(batchSize, measurementCount - i));
                    List <string> queries = new List<string>(measurementNames.Count);

                    foreach (var meas in measurementNamesInBatch)
                    {
                        string query = string.Format("SELECT last(\"Value\") as Value, \"Quality\" FROM \"{0}\"", meas);
                        queries.Add(query);
                    }
                    IEnumerable<IEnumerable<Serie>> resultList = influxDbClient.Client.MultiQueryAsync(queries, databaseName).GetAwaiter().GetResult();
  
                    if (resultList != null)
                    {
                        foreach (var serie in resultList)
                        {
                            if (serie.Count() > 0)
                            {
                                var firstSerie = serie.First(); 
                                if (firstSerie.Values.Count() > 0)
                                {
                                    var firstValue = firstSerie.Values.First();
                                    var lastDataPoint = new MdbEqPropDataPoint()
                                    {
                                        TimeStamp = (DateTime)firstValue[timeColumn],
                                        Value = firstValue[valueColumn],
                                        Quality = Convert.ToUInt32(firstValue[qualityColumn])
                                    };
                                    lastDataPointDict.Add(firstSerie.Name, lastDataPoint);
                                }
                                else
                                {
                                    log.Warn("No last value found in InfluxDb for " + firstSerie.Name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetLastHistoryDataPoint(), MeasurementNames.Count: " + measurementNames.Count.ToString());
                log.Debug(ex.ToString());
            }
            return lastDataPointDict;
        }

        /*public Dictionary<string, MdbEqPropDataPoint> getLastHistoryDataPoint_OLD_NotWorkingProperly(List<string> measurementNames)
        {
            var lastDataPointDict = new Dictionary<string, MdbEqPropDataPoint>();
            const int timeColumn = 0;
            const int valueColumn = 1;
            const int qualityColumn = 2;
            const int batchSize = 500;

            try
            {
                // Get range of measurement list to avoid too long queary string
                int measurementCount = measurementNames.Count;
                for (int i = 0; i < measurementCount; i += batchSize)
                {
                    string queryNames = String.Join("\",\"", measurementNames.GetRange(i, Math.Min(batchSize, measurementCount - i)));
                    string query = string.Format("SELECT last(\"Value\") as Value, \"Quality\" FROM \"{0}\"", queryNames);
                    IEnumerable<Serie> resultList = influxDbClient.Client.QueryAsync(query, databaseName).GetAwaiter().GetResult();
                    if (resultList != null)
                    {
                        foreach (var result in resultList)
                        {
                            var value = result.Values.First();
                            var lastDataPoint = new MdbEqPropDataPoint()
                            {
                                TimeStamp = (DateTime)value[timeColumn],
                                Value = value[valueColumn],
                                Quality = Convert.ToUInt32(value[qualityColumn])
                            };
                            lastDataPointDict.Add(result.Name, lastDataPoint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in getLastHistoryDataPoint()");
                log.Debug(ex.ToString());
            }
            return lastDataPointDict;
        }*/

        /*public List<MdbEqPropDataPoint> getHistoryTransientData(Guid eqId, Guid propId, DateTime fromTime, DateTime toTime)
        {
            List<MdbEqPropDataPoint> dataPointList = null;
            string logName = eqId.ToString() + "_" + propId.ToString();
            string query = string.Format("SELECT * FROM \"{0}\" WHERE time >= '{1}' and time <= '{2}'",
                logName, fromTime.ToString(queryDateTimeFormat), toTime.ToString(queryDateTimeFormat));

            // test start
            query = string.Format("SELECT * FROM \"{0}\" WHERE time >= '{1}' and time <= '{2}'",
                logName, fromTime.ToString(queryDateTimeFormat), toTime.ToString(queryDateTimeFormat));
            // test end

            try
            {
                var result = influxDbClient.Client.QueryAsync(query, databaseName).GetAwaiter().GetResult();
                if (result != null)
                {
                    const int timeColumn = 0;
                    const int qualityColumn = 1;
                    const int valueColumn = 2;

                    //dataPointList = new List<MdbEqPropDataPoint>(result.Count());
                    dataPointList = new List<MdbEqPropDataPoint>();
                    foreach (var series in result)
                    {
                        foreach (var value in series.Values)
                        {
                            TransientData transientData = DeserializeJsonToTransientDataObject((string)value[valueColumn]);
                            if (transientData != null)
                            {
                                var transientDpList = MakeTransiendDataHistoryDataPointList(
                                                        transientData, (DateTime)value[timeColumn],
                                                        Convert.ToUInt32(value[qualityColumn]));
                                if (transientDpList.Count > 0)
                                {
                                    dataPointList.AddRange(transientDpList);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in getHistoryTransientData()");
                log.Debug(ex.ToString());
            }
            return dataPointList;
        }*/

        /// <summary>
        /// Serialize an object to a byte[] and then encode the byte[] to a string
        /// </summary>
        /*public static string SerializeObjectToString(object obj)
        {
            using (var stream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, obj);
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }*/

        /// <summary>
        /// Encode a string to byte[] and then deserialize content of byte[] to an .net object 
        /// </summary>
        /*public static object DeserializeStringToObject(string str)
        {
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(str);
            using (var stream = new MemoryStream(byteArray))
            {
                var binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(stream);
            }
        }*/

        /// <summary>
        /// Serialize a TransientData object to json string 
        /// </summary>
        /*public static string SerializeTransientDataObjectToJson(TransientData obj)
        {
            return JsonConvert.SerializeObject(obj, 
                new JsonSerializerSettings
                {
                    StringEscapeHandling = StringEscapeHandling.Default,
                    Formatting = Formatting.None
                });
        }*/

        /// <summary>
        /// Deserialize json string to a TransientData object
        /// </summary>
        /*public static TransientData DeserializeJsonToTransientDataObject(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<TransientData>( RemoveIdbStringExcapeCharacters(str), 
                    new JsonSerializerSettings
                    {
                        StringEscapeHandling = StringEscapeHandling.Default,
                        Formatting = Formatting.None
                    });
            }
            catch(Exception ex)
            {
                log.Error("Exception in DeserializeJsonToTransientDataObject: " + ex.ToString());
                return null;
            }
        }*/

        /// <summary>
        /// Serialize an object to json string 
        /// </summary>
        public static string SerializeObjectToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj,
                new JsonSerializerSettings
                {
                    StringEscapeHandling = StringEscapeHandling.Default,
                    Formatting = Formatting.None
                });
        }

        /// <summary>
        /// Deserialize json string to an object
        /// </summary>
        public static T DeserializeJsonToObject<T>(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>( RemoveIdbStringExcapeCharacters(str),
                    new JsonSerializerSettings
                    {
                        StringEscapeHandling = StringEscapeHandling.Default,
                        Formatting = Formatting.None
                    });
            }
            catch (Exception ex)
            {
                log.Error("Exception in DeserializeJsonToObject()");
                log.Debug(ex.ToString());
                object obj = null;
                return (T)obj;
            }
        }

        /// <summary>
        /// Deserialize json string to an object
        /// </summary>
        public static object DeserializeJsonToObject(string str, Type dataType)
        {
            try
            {
                return JsonConvert.DeserializeObject( RemoveIdbStringExcapeCharacters(str), dataType,
                    new JsonSerializerSettings
                    {
                        StringEscapeHandling = StringEscapeHandling.Default,
                        Formatting = Formatting.None
                    });
            }
            catch (Exception ex)
            {
                throw new InvalidCastException("Exception in DeserializeJsonToObject()", ex);
            }
        }

        /// <summary>
        /// Remove special excape characters from InfluxDB string values 
        /// Note: These escape characters are automatically added by the InfluxDbClient.WriteAsync method 
        /// when string field values are written to InfluxDB, but they are not automatically removed 
        /// by InfluxDbClient when string values are read from InfluxDB.
        /// </summary>
        /// <returns>True if database exist, else false</returns>
        public static string RemoveIdbStringExcapeCharacters(string idbString)
        {
            return idbString
                    .Replace(@"\,", @",")
                    .Replace(@"\=", @"=")
                    .Replace(@"\ ", @" ");
        }

        /*public List<MdbEqPropDataPoint> MakeTransiendDataHistoryDataPointList(TransientData transientData, 
                                                                            DateTime startTimeStamp, 
                                                                            uint quality)
        {
            List<MdbEqPropDataPoint> dataPointList = new List<MdbEqPropDataPoint>();
            DateTime timeStamp = startTimeStamp;
            foreach (var value in transientData.Samples)
            {
                var dataPoint = new MdbEqPropDataPoint()
                {
                    TimeStamp = timeStamp,
                    Value = value,
                    Quality = quality
                };
                dataPointList.Add(dataPoint);

                // Calculate timestamp for next sample
                timeStamp = timeStamp.AddSeconds(transientData.Delta);
            }
            return dataPointList;
        }*/

        /// <summary>
        /// Check if InfluxDb history database already exists 
        /// </summary>
        /// <returns>True if database exist, else false</returns>
        public bool ExistHistoryDatabase(string idbName)
        {
            bool idbNameExist = false;
            try
            {
                var idbList = influxDbClient.Database.GetDatabasesAsync().GetAwaiter().GetResult();
                foreach( Database db in idbList)
                {
                    if(db.Name == idbName)
                    {
                        idbNameExist = true;
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                string text = ex.ToString();
                log.Error(String.Format("Exception in existHistoryDatabase(\"{0}\"): {1}", idbName, text));
            }
            return idbNameExist;
        }

        /// <summary>
        /// Create the InfluxDb history database 
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool CreateHistoryDatabase(string idbName)
        {
            bool success = false;

            try
            {
                InfluxDataApiResponse response = influxDbClient.Database.CreateDatabaseAsync(idbName).GetAwaiter().GetResult() as InfluxDataApiResponse;
                if (response.Success)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in CreateHistoryDatabase(), IdbName: " + idbName);
                log.Debug(ex.ToString());
            }
            return success;
        }

        /// <summary>
        /// Check if InfluxDb history database retention policy already exists 
        /// </summary>
        /// <returns>True if retension policy exist, else false</returns>
        public bool ExistHistoryDatabaseRetentionPolicy(string idbName, string idbRetentionName)
        {
            bool idbPolicyExist = false;

            try
            {
                var policyList = influxDbClient.Retention.GetRetentionPoliciesAsync(idbName).GetAwaiter().GetResult();
                foreach(RetentionPolicy policy in policyList)
                {
                    if (policy.Name == idbRetentionName)
                    {
                        idbPolicyExist = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in ExistHistoryDatabaseRetentionPolicy(), IdbName: " + idbName + ", RetentionName: " + idbRetentionName);
                log.Debug(ex.ToString());
            }
            return idbPolicyExist;
        }

        /// <summary>
        /// Create the InfluxDb history database retension policy
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool CreateHistoryDatabaseRetentionPolicy(string idbName, string idbRetentionName, string idbRetentionDuration, string idbRetentionShardDuration)
        {
            bool success = false;
            string query = string.Format("CREATE RETENTION POLICY \"{0}\" ON \"{1}\" DURATION {2} REPLICATION 1 SHARD DURATION {3} DEFAULT", 
                idbRetentionName, idbName, idbRetentionDuration, idbRetentionShardDuration);

            try
            {
                var result = influxDbClient.Client.QueryAsync(query, idbName).GetAwaiter().GetResult();
                if (result != null)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in CreateHistoryDatabaseRetentionPolicy(), IdbName: " + idbName + ", RetentionName: " + idbRetentionName);
                log.Debug(ex.ToString());
            }
            return success;
        }

        /// <summary>
        /// Alter the existing InfluxDb history database retension policy
        /// </summary>
        /// <returns>True if successful, else false</returns>
        public bool AlterHistoryDatabaseRetentionPolicy(string idbName, string idbRetentionName, string idbRetentionDuration, string idbRetentionShardDuration)
        {
            bool success = false;
            string query = string.Format("ALTER RETENTION POLICY \"{0}\" ON \"{1}\" DURATION {2} REPLICATION 1 SHARD DURATION {3} DEFAULT",
                idbRetentionName, idbName, idbRetentionDuration, idbRetentionShardDuration);

            try
            {
                var result = influxDbClient.Client.QueryAsync(query, idbName).GetAwaiter().GetResult();
                if (result != null)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in AlterHistoryDatabaseRetentionPolicy(), IdbName: " + idbName + ", RetentionName: " + idbRetentionName);
                log.Debug(ex.ToString());
            }
            return success;
        }

        /*
        public bool CreateHistoryDatabaseRetentionPolicy_NotWorking(string idbName, string idbPolicyName, string idbDuration)
        {
            bool success = false;

            try
            {
                var response = influxDbClient.Retention.CreateRetentionPolicyAsync(idbName, idbPolicyName, idbDuration, 1).GetAwaiter().GetResult(); ;
                if (response.Success)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in CreateHistoryDatabaseRetentionPolicy_NotWorking()");
                log.Debug(ex.ToString());
            }
            return success;
        }
        */
    }

    /// <summary>
    /// A class holding one single equipment property data point, used both for current value and historical value.
    /// </summary>
    public class MdbEqPropDataPoint
    {
        [BsonElement]
        public DateTime TimeStamp { get; set; }

        [BsonElement]
        public object Value { get; set; }

        [BsonElement]
        [BsonRepresentation(BsonType.Int32, AllowOverflow = true)]
        public uint Quality { get; set; }

        [BsonConstructor]
        public MdbEqPropDataPoint()
        {
            TimeStamp = DateTime.MinValue;
            Value = 0;
            Quality = StatusCodes.Bad;
        }

        public MdbEqPropDataPoint(DateTime timeStamp, object value, uint quality)
        {
            TimeStamp = timeStamp;
            Value = value;
            Quality = quality;
        }

        public void set(DataPoint dataPoint)
        {
            TimeStamp = dataPoint.TimeStamp;
            Value = dataPoint.Value;
            Quality = dataPoint.Quality;
        }

    }

    /// <summary>
    /// A class holding equipment configuration 
    /// </summary>
    public class MdbEquipmentConfiguration : EquipmentConfiguration
    {
        public String Name { get; set; }
        private static readonly ILog log = LogProvider.GetCurrentClassLogger();

        /// <summary>
        /// Constructor
        /// </summary>
        public MdbEquipmentConfiguration()
        {
            Name = "";
        }

        /// <summary>
        /// Set properties of this equipment configuration instance equal to instance given as input parameter.
        /// </summary>
        public void set(EquipmentConfiguration equipmentConfig)
        {
            ID = equipmentConfig.ID;

            foreach (EquipmentType equipmentType in equipmentConfig.Types)
            {
                var eqType = new EquipmentType()
                {
                    ID = equipmentType.ID,
                    BaseID = equipmentType.BaseID,
                    Name = equipmentType.Name
                };
                eqType.SeriesDefinitionsSerializable.AddRange(equipmentType.SeriesDefinitionsSerializable);
                TypesSerializable.Add(eqType);
            }

            foreach (HierarchyNode hierarchyNode in equipmentConfig.Hierarchy)
            {
                var hNode = new HierarchyNode()
                {
                    ID = hierarchyNode.ID,
                    Name = hierarchyNode.Name,
                    ParentID = hierarchyNode.ParentID,
                    LevelID = hierarchyNode.LevelID
                };
                HierarchySerializable.Add(hNode);
            }

            foreach (Equipment equipment in equipmentConfig.Instances)
            {
                var eq = new Equipment()
                {
                    ID = equipment.ID,
                    Name = equipment.Name,
                    EquipmentTypeID = equipment.EquipmentTypeID,
                    HierarchyNodeID = equipment.HierarchyNodeID,
                };
                InstancesSerializable.Add(eq);
            }
        }

        /// <summary>
        /// Find the equipment type definition having the specified GUID.
        /// </summary>
        /// <returns>Return equipment type object if found, else null</returns>
        public EquipmentType getEquipmentInstanceTypeData(Guid eqTypeId)
        {
            EquipmentType typeData = null;
            try
            {
                typeData = TypesSerializable.Find(element => element.ID == eqTypeId);
            }
            catch (Exception ex)
            {
                log.Error("Exception in getEquipmentInstanceTypeData(), EqTypeId: " + eqTypeId.ToString());
                log.Debug(ex.ToString());
            }
            return typeData;
        }

        /// <summary>
        /// Find the hierarchy type definition having the specified GUID.
        /// </summary>
        /// <returns>Return hierarchy type object if found, else null</returns>
        public HierarchyNode getEquipmentInstanceHierarchyData(Guid hierarchyNodeId)
        {
            HierarchyNode hierarchyNode = null;
            Equipment equipment = null;
            try
            {
                // Try to find element in list of hierarchy definitions
                hierarchyNode = HierarchySerializable.Find(element => element.ID == hierarchyNodeId);
                if (hierarchyNode == null)
                {
                    // Try to find element in list of equipment instances
                    equipment = InstancesSerializable.Find(element => element.ID == hierarchyNodeId);
                    if (equipment != null)
                    {
                        hierarchyNode = new HierarchyNode();
                        hierarchyNode.ID = equipment.ID;
                        hierarchyNode.Name = equipment.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in getEquipmentInstanceHierarchyData(), HierarchyNodeId: " + hierarchyNodeId.ToString());
                log.Debug(ex.ToString());
            }
            return hierarchyNode;
        }

        /// <summary>
        /// Get list of Eguipment instances in this configuration object
        /// </summary>
        /// <returns>Return List<Equipment>, else null</returns>
        public List<Equipment> getEquipmentInstanceList()
        {
            List<Equipment> eqInstDataList = null;

            try
            {
                eqInstDataList = new List<Equipment>();
                eqInstDataList.AddRange(InstancesSerializable);
            }
            catch(Exception ex)
            {
                string text = ex.ToString();
                log.Error("Exception in getEquipmentInstanceList(): " + text);
            }
            return eqInstDataList;
        }

        /// <summary>
        /// Get list of DataSeriesDefinition for a spesific equipment type
        /// </summary>
        /// <returns>Return List<DataSeriesDefinition>, else null</returns>
        public List<DataSeriesDefinition> getEquipmentPropertyList(Guid eqTypeId)
        {
            List<DataSeriesDefinition> eqPropertyList = null;

            try
            {
                EquipmentType eqType = TypesSerializable.Find(element => element.ID == eqTypeId);
                if (eqType != null)
                {
                    eqPropertyList = new List<DataSeriesDefinition>();
                    eqPropertyList.AddRange(eqType.SeriesDefinitionsSerializable);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in getEquipmentPropertyList(), EqTypeId: " + eqTypeId.ToString());
                log.Debug(ex.ToString());
            }
            return eqPropertyList;
        }
    }

    public class MdbFieldPlugConfig
    {
        public Guid FieldPlugID;
        public string FieldPlugName;
        public string FieldPlugQueueName;
        public FieldPlugConfig FieltPlugConfigObject;

        public MdbFieldPlugConfig()
        {
            FieldPlugID = Guid.Empty;
            FieldPlugName = "";
            FieldPlugQueueName = "";
            FieltPlugConfigObject = null;
        }
        public MdbFieldPlugConfig(Guid fpId, string fpName, string fpQueueName, FieldPlugConfig fpConfig)
        {
            FieldPlugID = fpId;
            FieldPlugName = fpName;
            FieldPlugQueueName = fpQueueName;
            FieltPlugConfigObject = fpConfig;
        }
    }
}
