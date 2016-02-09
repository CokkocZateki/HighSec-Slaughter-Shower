using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SQLite;
using BannedFromHighsec.Models;

namespace BannedFromHighsec.Controllers
{
    public class HomeController : Controller
    {

        SQLiteConnection m_dbConnection;

        public void CreateDB()
        {
            try
            {            
            string app_data_path = HttpContext.Server.MapPath("~/App_Data/");
            //IF it doesn't exist create dynamic DB
            if (!System.IO.File.Exists(app_data_path + "BannedFromHighSec.sqlite"))
                SQLiteConnection.CreateFile(app_data_path + "BannedFromHighSec.sqlite");
            else
                return; //Database exists, don't do anything, return.

            //Open connection to make tables
            m_dbConnection = new SQLiteConnection("Data Source=" + app_data_path + "BannedFromHighSec.sqlite;Version=3;");
            m_dbConnection.Open();

            //Create table
            string sql = "create table Losses " +
                 "(killID int primary key on conflict ignore, killTime datetime, victimID int, victimName varchar(40), locationID int, locationName varchar(40)," +
                 "victimShipID int, victimShipName varchar(40), VictimLostIsk int"+
                 ")";
            
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            //Close and exit
            m_dbConnection.Close();
            }
            catch(Exception e)
            {
                TempData["ErrorMessage"] = "Sorry an error have occured " + e.Message;
            }
        }

        public void UpdateLosses()
        {
            //Update Dynamic DB with latest losses - Run every x time
            //Load in DB for sec status
            string app_data_path = HttpContext.Server.MapPath("~/App_Data/");
            m_dbConnection = new SQLiteConnection("Data Source=" + app_data_path + "sqlite-latest.sqlite;Version=3;");

            //var highsecLosses = new Losses();
            var tempSystemName = new List<string>();

            eZet.EveLib.ZKillboardModule.ZKillboard ZkillBoard = new eZet.EveLib.ZKillboardModule.ZKillboard();
            eZet.EveLib.ZKillboardModule.ZKillboardOptions ZkillOptions = new eZet.EveLib.ZKillboardModule.ZKillboardOptions();
            ZkillOptions.AllianceId.Add(99006112);
            ZkillOptions.WSpace = false;

            //Walk pages with losses (Hardcode 5 pages for now)
            var listLosses = new List<eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill>();

            for (int i = 0; i < 5; i++)
            {
                ZkillOptions.Page = i;
                var losses = ZkillBoard.GetLosses(ZkillOptions);
                listLosses.AddRange(losses);
            }


            //Temp list to hold data before db insertion
            var tempLosses = new List<viewLosses>();
            
            
            //Open database and make readings
            m_dbConnection.Open();
            foreach (var loss in listLosses)
            {
                //Lookup System status of location
                string sql = "select security, solarSystemName from mapSolarSystems where solarSystemID=" + loss.SolarSystemId;
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (reader.GetDouble(0) >= 0.5)
                    {
                        tempLosses.Add(new viewLosses(loss.KillId, loss.KillTime, loss.Victim.CharacterId, loss.Victim.CharacterName, loss.SolarSystemId, reader.GetString(1), loss.Victim.ShipTypeId, (long)loss.Stats.TotalValue));
                    }
                }
            }

            foreach (var loss in tempLosses)
            {
                //Lookup ShipName
                string sql = "select typeName from invTypes where typeId=" + loss.victimShipID;
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();

                //Get Result
                while (reader.Read())
                {
                    loss.victimShipName = reader.GetString(0);
                }
            }

            //Close old DB connection and open dynamic DB and paste info in

            m_dbConnection.Close();

            //Open own database
            m_dbConnection = new SQLiteConnection("Data Source=" + app_data_path + "BannedFromHighSec.sqlite;Version=3;");
            m_dbConnection.Open();

            //Check if killid is already present before adding somehow...!!!!!!

            //Write each entry into the database
            foreach (var lossEntry in tempLosses)
            {
                var correctTimeformat = string.Format("{0:yyyy-MM-dd HH:mm:ss}", lossEntry.killTime);
                var test = lossEntry.victimName.Replace("'", "''");
                string sql = "insert into Losses" +
                    "(killID, killTime, victimID, victimName, locationID, locationName, victimShipID, victimShipName, victimLostIsk)" + 
                    "Values" + 
                    "(" + lossEntry.killID + "," + 
                    "'" + correctTimeformat + "'" + "," +
                    lossEntry.victimID + "," +
                    "'" + lossEntry.victimName.Replace("'", "''") + "'" + "," +
                    lossEntry.locationID + "," + 
                    "'" + lossEntry.locationName + "'" + "," +
                    lossEntry.victimShipID + "," +
                    "'" + lossEntry.victimShipName + "'" + "," +
                    lossEntry.victimLostIsk +
                    ")";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();
            }

            m_dbConnection.Close();
        }

        public ActionResult Index()
        {
            //Create CB if needed
            //CreateDB();

            //Update DB
            //UpdateLosses();

            //Select first 20 results and save
            string app_data_path = HttpContext.Server.MapPath("~/App_Data/");
            m_dbConnection = new SQLiteConnection("Data Source=" + app_data_path + "BannedFromHighSec.sqlite;Version=3;");
            m_dbConnection.Open();

            //SELECT * FROM Table ORDER BY datetime(datetimeColumn) DESC Limit 1  //For SQL to fetch first x results
            //Lookup ShipName
            string sql = "SELECT * FROM Losses ORDER BY datetime(killTime) DESC Limit 20";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader read = command.ExecuteReader();

            var highsecLosses = new List<viewLosses>();

            //Get Result
            while (read.Read())
            {
                highsecLosses.Add(new viewLosses(read.GetInt64(0), read.GetDateTime(1), read.GetInt64(2), read.GetString(3), read.GetInt64(4), read.GetString(5), read.GetInt64(6) , read.GetString(7), read.GetInt64(8)));
            }
            
            //Save in model and return to view with list
            var model = new IndexViewModel();
            model.highsecLosses = highsecLosses;

            return View(model);
        }
    }
}