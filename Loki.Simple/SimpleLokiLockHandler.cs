using Coddie.Crud;
using Coddie.Data;
using Loki.Simple.Entity;
using Mst.DexterCfg.Factory;
using SimpleFileLogging.Enums;
using SimpleFileLogging.Interfaces;
using SimpleFileLogging.Logging;
using System;
using System.Data;

namespace Loki.Simple
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A simple loki lock handler. This class will be used for generic database support for Loki library. </summary>
    ///
    /// <remarks>   Mustafa SACLI, 21.02.2022. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class SimpleLokiLockHandler : LokiLockHandler
    {
        private IDbConnection GetDbConnection()
        {
            var dbConnection = DxCfgConnectionFactory.Instance.GetConnection(DxCfgConnectionFactory.Instance["loki-conn-name"]);
            var connStrName = DxCfgConnectionFactory.Instance["loki-conn-str-name"];
            dbConnection.ConnectionString = DxCfgConnectionFactory.Instance[connStrName];
            return dbConnection;
        }

        protected ISimpleLogger DayLogger
        {
            get
            {
                ISimpleLogger logger = SimpleLoggerStorage.GetSimpleLogger(SimpleLogDateFormats.Day);
                return logger;
            }
        }

        public SimpleLokiLockHandler()
        {
        }

        public override bool Lock(string serviceKey, int expiryFromSeconds)
        {
            bool isLocked = false;
            try
            {
                using (IDbConnection connection = GetDbConnection())
                {
                    try
                    {
                        connection.OpenIfNot();
                        var entity = connection.SingleOrDefault<LokiLockingEntity>(q => q.ServiceKey == serviceKey);
                        var isAnyLocked = entity?.CreationDate;

                        if (isAnyLocked != null)
                        {
                            DateTime creationDate = Convert.ToDateTime(isAnyLocked);

                            if (creationDate.AddSeconds(expiryFromSeconds) < DateTime.Now)
                            {
                                // this is optional, not certain.
                                entity.CreationDate = DateTime.Now;
                                var updateResult = connection.PartialUpdate<LokiLockingEntity>(new { CreationDate = DateTime.Now }, new
                                {
                                    ServiceKey = serviceKey,
                                    entity.CreationDate
                                });

                                if (updateResult > 0)
                                {
                                    isLocked = true;
                                }
                            }
                        }
                        else
                        {
                            entity = new LokiLockingEntity { ServiceKey = serviceKey, CreationDate = DateTime.Now };
                            var insertResult = connection.Insert(entity);

                            if (insertResult > 0)
                            {
                                isLocked = true;
                            }
                        }
                    }
                    finally
                    {
                        connection.CloseIfNot();
                    }
                }
            }
            catch (Exception ex)
            {
                isLocked = false;
                DayLogger.Error(ex);
            }

            return isLocked;
        }

        public override void Release(string serviceKey)
        {
            try
            {
                using (IDbConnection connection = GetDbConnection())
                {
                    try
                    {
                        connection.OpenIfNot();
                        connection.DeleteAll<LokiLockingEntity>(new { ServiceKey = serviceKey });
                    }
                    finally
                    {
                        connection.CloseIfNot();
                    }
                }
            }
            catch (Exception ex)
            {
                DayLogger.Error(ex);
            }
        }
    }
}