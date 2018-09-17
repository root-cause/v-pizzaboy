using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Pizzaboy
{
    #region Script
    public class Main : Script
    {
        #region Settings
        int JobToggleKey = 176;
        int ThrowLeftKey = 174;
        int ThrowRightKey = 175;

        public static VehicleHash DeliveryVehicleModel = VehicleHash.Faggio2;
        public static bool CustomerMarkers = true;
        public static int JobSeconds = 300;
        public static int MaxCustomers = 10;
        public static int BoxCount = 5;
        public static int RewardBase = 10;
        public static int RewardMax = 30;
        public static string Language = "en";
        #endregion

        public static Random RNG = new Random();
        public static List<Blip> StoreBlips = new List<Blip>();
        public static Mission Handler = new Mission();

        #region Event: Main/Init
        public Main()
        {
            string modFolder = Path.Combine("scripts", "pizzaboy");
            string langFolder = Path.Combine(modFolder, "lang");

            // Create folders
            try
            {
                if (!Directory.Exists(modFolder)) Directory.CreateDirectory(modFolder);
                if (!Directory.Exists(langFolder)) Directory.CreateDirectory(langFolder);
            }
            catch (Exception e)
            {
                UI.Notify($"~r~Folder error: ~w~{e.Message}");
            }

            // Load player settings
            try
            {
                string settingsFilePath = Path.Combine(modFolder, "config.ini");
                ScriptSettings config = ScriptSettings.Load(settingsFilePath);

                if (File.Exists(settingsFilePath))
                {
                    JobToggleKey = config.GetValue("SETTINGS", "JobToggleKey", 176);
                    ThrowLeftKey = config.GetValue("SETTINGS", "ThrowLKey", 174);
                    ThrowRightKey = config.GetValue("SETTINGS", "ThrowRKey", 175);
                    DeliveryVehicleModel = config.GetValue("SETTINGS", "VehModel", VehicleHash.Faggio2);
                    CustomerMarkers = config.GetValue("SETTINGS", "CustomerMarkers", true);
                    JobSeconds = config.GetValue("SETTINGS", "JobSeconds", 300);
                    MaxCustomers = config.GetValue("SETTINGS", "MaxCustomers", 10);
                    BoxCount = config.GetValue("SETTINGS", "BoxCount", 5);
                    RewardBase = config.GetValue("SETTINGS", "RewardBase", 10);
                    RewardMax = config.GetValue("SETTINGS", "RewardMax", 30);
                    Language = config.GetValue("SETTINGS", "Language", "en");
                }
                else
                {
                    config.SetValue("SETTINGS", "JobToggleKey", JobToggleKey);
                    config.SetValue("SETTINGS", "ThrowLKey", ThrowLeftKey);
                    config.SetValue("SETTINGS", "ThrowRKey", ThrowRightKey);
                    config.SetValue("SETTINGS", "VehModel", DeliveryVehicleModel);
                    config.SetValue("SETTINGS", "CustomerMarkers", CustomerMarkers);
                    config.SetValue("SETTINGS", "JobSeconds", JobSeconds);
                    config.SetValue("SETTINGS", "MaxCustomers", MaxCustomers);
                    config.SetValue("SETTINGS", "BoxCount", BoxCount);
                    config.SetValue("SETTINGS", "RewardBase", RewardBase);
                    config.SetValue("SETTINGS", "RewardMax", RewardMax);
                    config.SetValue("SETTINGS", "Language", Language);
                }

                config.Save();
            }
            catch (Exception e)
            {
                UI.Notify($"~r~Settings error: ~w~{e.Message}");
            }

            // Load language file
            try
            {
                string languageFilePath = Path.Combine(langFolder, $"{Language}.xml");
                XElement langFile = XElement.Load(languageFilePath);
                Localization.Strings = langFile.Elements().ToDictionary(key => key.Name.LocalName, val => val.Value);
            }
            catch (Exception e)
            {
                UI.Notify($"~r~Language error ({Language}): ~w~{e.Message}.");
            }

            // Create pizza store blips
            foreach (Vector3 position in Constants.StoreCoords)
            {
                Blip blip = World.CreateBlip(position);
                blip.Sprite = (BlipSprite)267;
                blip.Color = (BlipColor)25;
                blip.IsShortRange = true;
                blip.Scale = 1.0f;
                blip.Name = Localization.Get("STORE_NAME");

                StoreBlips.Add(blip);
            }

            // Set up events
            Tick += ScriptTick;
            Aborted += ScriptAborted;
        }
        #endregion

        #region Event: Tick
        public void ScriptTick(object sender, EventArgs e)
        {
            if (Handler == null) return;

            Ped playerPed = Game.Player.Character;
            if (Handler.IsRunning)
            {
                // This part runs if the player is delivering pizzas
                int gameTime = Game.GameTime;
                if (CustomerMarkers) Handler.DrawPedMarkers(playerPed.Position);

                // Draw pizza store marker
                float distance = playerPed.Position.DistanceTo(Constants.StoreCoords[Handler.StoreID]);
                if (distance <= Constants.MarkerDrawDistance)
                {
                    World.DrawMarker(MarkerType.VerticalCylinder, Constants.StoreCoords[Handler.StoreID] - new Vector3(0f, 0f, 1f), Vector3.Zero, Vector3.Zero, new Vector3(2f, 2f, 0.75f), Color.ForestGreen);

                    if (distance <= 1f)
                    {
                        if (playerPed.IsInVehicle(Handler.Vehicle))
                        {
                            if (Handler.Customers.Count < 1)
                            {
                                Handler.SpawnCustomers(MaxCustomers);
                                Handler.JobEndTime = gameTime + (JobSeconds * 1000);
                            }

                            if (Handler.BoxCount < 1) Handler.BoxCount = BoxCount;
                        }
                        else
                        {
                            Methods.DisplayHelpText(Localization.Get("COME_WITH_SCRIPT_VEHICLE"));
                        }
                    }
                }

                // Display script keys
                if (gameTime < Handler.ShowInfoUntil) Methods.DisplayHelpText(Localization.Get("JOB_KEYS_TEXT", HelpTextKeys.Get(JobToggleKey), HelpTextKeys.Get(ThrowLeftKey), HelpTextKeys.Get(ThrowRightKey)));

                // Draw job info
                Handler.DrawInfo();

                if (gameTime - Handler.LastRun >= 500)
                {
                    // Cancel if the player is dead/in a mission/has a handle change (character switch?)/gets the delivery vehicle destroyed
                    if (Game.MissionFlag || playerPed.IsDead || Handler.Vehicle.IsDead || playerPed.Handle != Handler.PlayerHandle)
                    {
                        UI.ShowSubtitle(Localization.Get("DELIVERY_CANCELLED"), Constants.SubtitleTime);
                        Handler.IsRunning = false;
                        return;
                    }

                    // Cancel if the player ran out of time
                    if (gameTime > Handler.JobEndTime)
                    {
                        UI.ShowSubtitle(Localization.Get("TIMEOUT"), Constants.SubtitleTime);
                        Handler.IsRunning = false;
                        return;
                    }

                    // Update mission time display
                    TimeSpan ts = TimeSpan.FromMilliseconds(Handler.JobEndTime - gameTime);
                    Handler.TimeText = Localization.Get("MISSION_TIME", ts.Minutes, ts.Seconds);

                    // Handle thrown pizza box
                    Handler.BoxLogic(gameTime);

                    // Update last run timestamp
                    Handler.LastRun = gameTime;
                }

                // Control check
                if (Game.IsControlJustPressed(2, (Control)ThrowLeftKey))
                {
                    Handler.ThrowBox(true);
                }
                else if (Game.IsControlJustPressed(2, (Control)ThrowRightKey))
                {
                    Handler.ThrowBox(false);
                }
                else if (Game.IsControlJustPressed(2, (Control)JobToggleKey))
                {
                    Handler.Stop();
                }
            }
            else
            {
                // This part runs if the player is NOT delivering pizzas
                if (Methods.CanUseMarkers())
                {
                    foreach (Vector3 position in Constants.StoreCoords)
                    {
                        float distance = playerPed.Position.DistanceTo(position);
                        if (distance <= Constants.MarkerDrawDistance)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, position - new Vector3(0f, 0f, 1f), Vector3.Zero, Vector3.Zero, new Vector3(2f, 2f, 0.75f), Color.ForestGreen);

                            if (distance <= 1f)
                            {
                                if (!playerPed.IsInVehicle())
                                {
                                    Methods.DisplayHelpText(Localization.Get("JOB_MARKER_TEXT", HelpTextKeys.Get(JobToggleKey)));

                                    // Control check
                                    if (Game.IsControlJustPressed(2, (Control)JobToggleKey)) Handler.Start();
                                }
                                else
                                {
                                    Methods.DisplayHelpText(Localization.Get("COME_WITHOUT_VEHICLE"));
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Event: Abort
        public void ScriptAborted(object sender, EventArgs e)
        {
            // Script terminated, destroy everything
            if (Handler != null) Handler.IsRunning = false;

            if (StoreBlips != null)
            {
                foreach (Blip blip in StoreBlips) blip?.Remove();
                StoreBlips.Clear();
            }
        }
        #endregion
    }
    #endregion
}