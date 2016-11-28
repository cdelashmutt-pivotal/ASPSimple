using ASPSimple.Models;
using ASPSimple.ViewModels;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ASPSimple.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var cloudFoundryApplication = ServerConfig.CloudFoundryApplication;
            var cloudFoundryServices = ServerConfig.CloudFoundryServices;
            return View(new CloudFoundryViewModel(
                cloudFoundryApplication == null ? new CloudFoundryApplicationOptions() : cloudFoundryApplication,
                cloudFoundryServices == null ? new CloudFoundryServicesOptions() : cloudFoundryServices));
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public void Kill()
        {
            Console.WriteLine("Faster Pussycat! Kill! Kill!");
            Environment.Exit(-1);
        }

        [HttpPost]
        public ActionResult Upsert(Attendee attendee)
        {
            Console.WriteLine(string.Format("Upserting Attendee: {0}, {1}, {2}", attendee.Id, attendee.FirstName, attendee.LastName));
            if (DbConfig.DbEngine != DatabaseEngine.None)
            {
                if (DbConfig.DbEngine == DatabaseEngine.SqlServer)
                {
                    using (SqlConnection conn = new SqlConnection(DbConfig.ConnectionString))
                    using (SqlCommand command = new SqlCommand()
                    {
                        Connection = conn,
                        CommandType = CommandType.Text
                    })
                    {
                        if (attendee.Id != null)
                        {
                            command.CommandText = @"update [attendee] set [first_name] = @first_name,[last_name] = @last_name WHERE id=@id";
                            command.Parameters.Add("@id", SqlDbType.BigInt);
                            command.Parameters.Add("@first_name", SqlDbType.VarChar, 255);
                            command.Parameters.Add("@last_name", SqlDbType.VarChar, 255);
                            command.Parameters["@id"].Value = attendee.Id;
                            command.Parameters["@first_name"].Value = attendee.FirstName;
                            command.Parameters["@last_name"].Value = attendee.LastName;
                        }
                        else
                        {
                            command.CommandText = @"insert into [attendee] ([first_name],[last_name]) VALUES(@first_name, @last_name)";
                            command.Parameters.Add("@first_name", SqlDbType.VarChar, 255);
                            command.Parameters.Add("@last_name", SqlDbType.VarChar, 255);
                            command.Parameters["@first_name"].Value = attendee.FirstName;
                            command.Parameters["@last_name"].Value = attendee.LastName;
                        }
                        conn.Open();
                        Int32 rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine("Rows Affected: {0}", rowsAffected);
                    }
                }
                else
                {
                    //MySQL Not Implemented
                }
            }

            return RedirectToAction("ViewDBData");
        }

        public ActionResult ViewDBData()
        {
            List<Attendee> attendees = new List<Attendee>();

            if (DbConfig.DbEngine != DatabaseEngine.None)
            {
                if (DbConfig.DbEngine == DatabaseEngine.SqlServer)
                {
                    using (SqlConnection conn = new SqlConnection(DbConfig.ConnectionString))
                    using (SqlCommand command = new SqlCommand()
                    {
                        CommandText = @"select id, first_name, last_name from attendee",
                        Connection = conn,
                        CommandType = CommandType.Text
                    })
                    {
                        conn.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    attendees.Add(new Models.Attendee()
                                    {
                                        Id = reader.GetInt64(0),
                                        FirstName = reader.GetString(1),
                                        LastName = reader.GetString(2)
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    //MySQL Not Implemented
                }
            }
            return View(attendees);
        }
    }
}