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

            var debug = Environment.CurrentDirectory; ;
            //IF it doesn't exist create dynamic DB
            if (!System.IO.File.Exists("|DataDirectory|\\BannedFromHighSec.sqlite"))
                SQLiteConnection.CreateFile("BannedFromHighSec.sqlite;");
            else
                return; //Database exists, don't do anything, return.

            //Open connection to make tables
            m_dbConnection = new SQLiteConnection("Data Source=|DataDirectory|\\BannedFromHighSec.sqlite;Version=3;");
            m_dbConnection.Open();

            //Create table
            string sql = "create table Losses " +
                 "(killID int primary key, victimID int, victimName varchar(40), locationID int, locationName varchar(40)," +
                 "victimShipID int, victimShipName varchar(40), VictimLostIsk int,  "+
                 ")";
            
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            //Close and exit
            m_dbConnection.Close();

        }

        public void UpdateLosses()
        {
            //Update Dynamic DB with latest losses - Run every x time
            //Load in DB for sec status
            m_dbConnection = new SQLiteConnection("Data Source=|DataDirectory|\\sqlite-latest.sqlite;Version=3;");
            m_dbConnection.Open();

            //var highsecLosses = new Losses();
            var tempSystemName = new List<string>();
            var tempLosses = new List<eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill>();

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
                        tempLosses.Add(loss);
                        tempSystemName.Add(reader.GetString(1));
                    }
                }
            }

            //Combine to one list for DB insertion
            var highsecLosses = new List<Losses>();

            for (int i = 0; i<tempLosses.Count(); i++)
            {
                //Lookup ShipName
                string sql = "select typeName from invTypes where typeId=" + tempLosses[i].Victim.ShipTypeId;
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();


                string shipName = "";
                //Get Result
                while (reader.Read())
                {
                    shipName = reader.GetString(0);
                }

                //Add to list
                highsecLosses.Add(new Losses(tempLosses[i], tempSystemName[i], shipName));
            }

            //Close old DB connection and open dynamic DB and paste info in

            m_dbConnection.Close();

            //Time test = correct format for insertion into DB
            var test = DateTime.UtcNow;
            var testest = string.Format("{0:yyyy-MM-dd HH:mm:ss}", test);

            //SELECT * FROM Table ORDER BY datetime(datetimeColumn) DESC Limit 1  //For SQL to fetch first x results
        }

        public ActionResult Index()
        {

            //Create CB
            CreateDB();

            /*   //Load in DB for sec status
               m_dbConnection = new SQLiteConnection("Data Source=C:\\Users\\Crow\\Documents\\Visual Studio 2015\\Projects\\BannedFromHighsec\\BannedFromHighsec\\App_Data\\sqlite-latest.sqlite;Version=3;");
               m_dbConnection.Open();

               var highsecLosses = new List<eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill>();
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

               foreach (var loss in listLosses)
               {
                   //Lookup System status of location
                   string sql = "select security from mapSolarSystems where solarSystemID=" + loss.SolarSystemId;
                   SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                   SQLiteDataReader reader = command.ExecuteReader();


                   var rows = reader.StepCount;
                   while (reader.Read())
                   {
                       if (reader.GetDouble(0) >= 0.5)
                       {
                           highsecLosses.Add(loss);
                       }
                   }                
               }

               //Sort list
               highsecLosses.Sort((s1, s2) => s1.KillTime.CompareTo(s2.KillTime));

               //Save in model and return to view with list
               var model = new IndexViewModel();
               model.highsecLosses = highsecLosses;

       */  
            return View();
            //return View(model);
        }
    }
}