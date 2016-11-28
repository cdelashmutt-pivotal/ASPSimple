using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SqlClient;

namespace ASPSimple
{
    public class DbConfig
    {
        public static string ConnectionString {get; set;}
        public static DatabaseEngine DbEngine {get; set;}

        public static void Update()
        {
            if (ServerConfig.CloudFoundryServices.Services != null)
            {
                //Try MSSQL Server first
                Service db = ServerConfig.CloudFoundryServices.Services.Where(s => s.Label == "mssql-dev").FirstOrDefault();
                if (db != null)
                {
                    DbConfig.DbEngine = DatabaseEngine.SqlServer;
                    DbConfig.ConnectionString = db.Credentials["connectionString"].Value;
                }
                else
                {
                    //Try MySQL next
                    db = ServerConfig.CloudFoundryServices.Services.Where(s => s.Label == "p-mysql").FirstOrDefault();
                    if(db != null)
                    {
                        DbConfig.DbEngine = DatabaseEngine.MySql;
                        MySqlConnectionStringBuilder csbuilder = new MySqlConnectionStringBuilder();
                        csbuilder.Add("server", db.Credentials["hostname"].Value);
                        csbuilder.Add("port", db.Credentials["port"].Value);
                        csbuilder.Add("uid", db.Credentials["username"].Value);
                        csbuilder.Add("pwd", db.Credentials["password"].Value);
                        csbuilder.Add("database", db.Credentials["name"].Value);
                        DbConfig.ConnectionString = csbuilder.ToString();
                    }
                    else
                    {
                        DbConfig.DbEngine = DatabaseEngine.None;
                    }
                }
            }
        }

        public static void CheckDbStructure()
        {
            Console.WriteLine("Checking DB structure.");
            if (DbEngine == DatabaseEngine.SqlServer)
            {
                Console.WriteLine("Detected an mssql-dev service binding.");
                // make sure tables exist

                using (SqlConnection conn = new SqlConnection(DbConfig.ConnectionString))
                // if the table doesn't exist, create it
                using (SqlCommand command = new SqlCommand()
                {
                    CommandText = @"IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'attendee'))
                BEGIN
                    CREATE TABLE attendee (
                    id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY CLUSTERED,
                    first_name varchar(255) DEFAULT NULL,
                    last_name varchar(255) DEFAULT NULL)            
                END",
                    Connection = conn,
                    CommandType = CommandType.Text
                })
                {
                    conn.Open();
                    int rows = command.ExecuteNonQuery();
                    if (rows > -1)
                        Console.WriteLine("table didn't exist, creating it: " + rows + " rows affected.");
                }
            }
            else if (DbEngine == DatabaseEngine.MySql)
            {
                using (MySqlConnection conn = new MySqlConnection(DbConfig.ConnectionString))
                // if the table doesn't exist, create it
                using (MySqlCommand command = new MySqlCommand()
                {
                    CommandText = @"CREATE TABLE IF NOT EXISTS `attendee` (
                  `id` bigint(20) NOT NULL AUTO_INCREMENT,
                  `first_name` varchar(255) DEFAULT NULL,
                  `last_name` varchar(255) DEFAULT NULL,
                  PRIMARY KEY (`id`)
                ) AUTO_INCREMENT=8 DEFAULT CHARSET=latin1;",
                    Connection = conn,
                    CommandType = CommandType.Text
                })
                {
                    conn.Open();
                    int rows = command.ExecuteNonQuery();
                    if (rows > 0)
                        Console.WriteLine("table didn't exist, creating it: " + rows + " rows affected.");
                };
            }
            else
            {
                Console.WriteLine("No DB found.");
            }
        }
    }

    public enum DatabaseEngine
    {
        None = 0,
        SqlServer = 1,
        MySql = 2
    }
}