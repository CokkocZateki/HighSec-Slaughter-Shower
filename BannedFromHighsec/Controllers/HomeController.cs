﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SQLite;
using System.Configuration;
using BannedFromHighsec.Models;

namespace BannedFromHighsec.Controllers
{
    public class HomeController : Controller
    {

        SQLiteConnection m_dbConnection;
        SQLiteConnection m_dbConnection_Update;

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

        public void UpdateLosses486213795()
        {
            //Update Dynamic DB with latest losses - Run every x time
            //Load in DB for sec status
            string app_data_path = HttpContext.Server.MapPath("~/App_Data/");
            m_dbConnection_Update = new SQLiteConnection("Data Source=" + app_data_path + "sqlite-latest.sqlite;Version=3;");

            //var highsecLosses = new Losses();
            var tempSystemName = new List<string>();

            eZet.EveLib.ZKillboardModule.ZKillboard ZkillBoard = new eZet.EveLib.ZKillboardModule.ZKillboard();
            eZet.EveLib.ZKillboardModule.ZKillboardOptions ZkillOptions = new eZet.EveLib.ZKillboardModule.ZKillboardOptions();

            //PastTime to get kills from
            var pastSeconds = ConfigurationManager.AppSettings["PastSeconds"];
            ZkillOptions.PastSeconds = Convert.ToInt32(pastSeconds); //last 20 min

            //Add IDs to fetch data from
            //var corpIDs = ConfigurationManager.AppSettings["CorpID"].ToList();
            var corpKeys = ConfigurationManager.AppSettings.AllKeys.Where(k => k.StartsWith("CorpID")).Select(p => ConfigurationManager.AppSettings[p]).ToList();
            var allianceKeys = ConfigurationManager.AppSettings.AllKeys.Where(k => k.StartsWith("AllianceID")).Select(p => ConfigurationManager.AppSettings[p]).ToList();

            var allianceIDs = ConfigurationManager.AppSettings["AllianceID"];
            List<long> corps = new List<long>();
            List<long> alliances = new List<long>();
            foreach (var id in corpKeys)
            {
                var tempID = Convert.ToInt64(id);
                corps.Add(tempID);
            }

            foreach (var id in allianceKeys)
            {
                var tempID = Convert.ToInt64(id);
                alliances.Add(tempID);

            }

            ZkillOptions.CorporationId = corps;
            ZkillOptions.AllianceId = alliances;
            
            //ZkillOptions.CorporationId.Add(98011392); //Insrt
            //ZkillOptions.CorporationId.Add(98224068); //Baers
            //ZkillOptions.AllianceId.Add(99006112); //Friendly Probes
            //ZkillOptions.WSpace = false;

            //New list for response from Zkillboard
            var listLosses = new List<eZet.EveLib.ZKillboardModule.Models.ZkbResponse.ZkbKill>();
            var losses = ZkillBoard.GetLosses(ZkillOptions);
            listLosses.AddRange(losses);

            //If we happen to have more than 200 request we walk though the rest pages here. ###### Never going to happen. Don't think about it ######
            int i = 2;
            while (losses.Count > 0)
            {
                ZkillOptions.Page = i;
                losses = ZkillBoard.GetLosses(ZkillOptions);
                listLosses.AddRange(losses);
                i++;
            }
            
                 
            //Temp list to hold data before db insertion
            var tempLosses = new List<viewLosses>();


            //Open database and make readings
            m_dbConnection_Update.Open();
            foreach (var loss in listLosses)
            {
                //Lookup System status of location
                string sql = "select security, solarSystemName from mapSolarSystems where solarSystemID=" + loss.SolarSystemId;
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection_Update);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    double secStatus = reader.GetDouble(0);
                    if (secStatus >= 0.5)
                    {
                        tempLosses.Add(new viewLosses(loss.KillId, loss.KillTime, loss.Victim.CharacterId, loss.Victim.CharacterName, loss.SolarSystemId, reader.GetString(1), loss.Victim.ShipTypeId, (long)loss.Stats.TotalValue));
                    }
                }
            }

            foreach (var loss in tempLosses)
            {
                //Lookup ShipName
                string sql = "select typeName from invTypes where typeId=" + loss.victimShipID;
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection_Update);
                SQLiteDataReader reader = command.ExecuteReader();

                //Get Result
                while (reader.Read())
                {
                    loss.victimShipName = reader.GetString(0);
                }
            }

            //Close old DB connection and open dynamic DB and paste info in

            m_dbConnection_Update.Close();

            //Open own database
            m_dbConnection_Update = new SQLiteConnection("Data Source=" + app_data_path + "BannedFromHighSec.sqlite;Version=3;");
            m_dbConnection_Update.Open();

            //Check if killid is already present before adding somehow...!!!!!!

            //Write each entry into the database
            foreach (var lossEntry in tempLosses)
            {
                var correctTimeformat = string.Format("{0:yyyy-MM-dd HH:mm:ss}", lossEntry.killTime);
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
                    "'" + lossEntry.victimShipName.Replace("'", "''") + "'" + "," +
                    lossEntry.victimLostIsk +
                    ")";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection_Update);
                command.ExecuteNonQuery();
            }

            m_dbConnection_Update.Close();
        }

        public ActionResult Index()
        {
            //Create CB if needed Never needed as I publish it with DB
            //CreateDB();

            //Update DB
            //UpdateLosses486213795();

            //Select Last 20 results and save
            string app_data_path = HttpContext.Server.MapPath("~/App_Data/");
            m_dbConnection = new SQLiteConnection("Data Source=" + app_data_path + "BannedFromHighSec.sqlite;Version=3;");
            m_dbConnection.Open();

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