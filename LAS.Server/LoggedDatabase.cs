using System;
using NLog;
using PetaPoco;
using System.Linq;
using System.Data.Common;

namespace LAS.Server
{
	public class LoggedDatabase : Database
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public LoggedDatabase()
		{
		}

		public LoggedDatabase(IDatabaseBuildConfiguration configuration)
			: base (configuration)
		{

		}

		public override void OnExecutingCommand(System.Data.IDbCommand cmd)
		{
			base.OnExecutingCommand(cmd);
			logger.Info("OnExecutingCommand:" + cmd.CommandText);
			//logger.Info(cmd.Parameters.GetType ());
			// var parameters = (Mono.Data.Sqlite.SqliteParameterCollection)cmd.Parameters;
			var parameters = (DbParameterCollection)cmd.Parameters;

			foreach (var c in parameters) {
				//logger.Info(c.GetType ());
				// var c1 = (Mono.Data.Sqlite.SqliteParameter)c;
				var c1 = (DbParameter)c;
				logger.Info(c1.ParameterName + " = " + c1.Value);
			}
			// logger.Info(string.Join (",", parameters.Count)));
		}

		public override System.Data.IDbConnection OnConnectionOpened(System.Data.IDbConnection conn)
		{
			var c = base.OnConnectionOpened(conn);
			logger.Info("Opening connection");
			return c;
		}

		public override void OnConnectionClosing(System.Data.IDbConnection conn)
		{
			base.OnConnectionClosing(conn);
			logger.Info("Closing Database");
		}
	}
}
