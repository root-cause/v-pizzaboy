using System;
using System.Drawing;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Pizzaboy
{
    public class Mission
    {
        #region Properties
        // IDs
        public int StoreID { get; set; } = -1;
        public int PlayerHandle { get; set; } = -1;

        // Entities
        public Prop Box { get; set; } = null;
        public Vehicle Vehicle { get; set; } = null;
        public List<Ped> Customers { get; set; } = new List<Ped>();

        // Mission Data
        public string TimeText { get; set; } = string.Empty;
        public string BoxCountText { get; set; } = string.Empty; 
        public bool LastThrowSuccessful { get; set; } = false;
        public int ShowInfoUntil { get; set; } = 0;
        public int BoxThrowTime { get; set; } = 0;
        public int LastRun { get; set; } = 0;

        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }

            set
            {
                if (!value)
                {
                    StoreID = -1;
                    PlayerHandle = -1;

                    Box?.Delete();
                    Vehicle?.Delete();
                    foreach (Ped customer in Customers) customer?.Delete();

                    Box = null;
                    Vehicle = null;
                    Customers.Clear();

                    foreach (Blip blip in Main.StoreBlips)
                    {
                        if (blip != null)
                        {
                            blip.IsShortRange = true;
                            blip.Alpha = 255;
                        }
                    }
                }

                _isRunning = value;
                _generatingPeds = false;
            }
        }

        public int JobEndTime
        {
            get
            {
                return _endTime;
            }

            set
            {
                _endTime = value;

                TimeSpan ts = TimeSpan.FromMilliseconds(_endTime);
                TimeText = Localization.Get("MISSION_TIME", ts.Minutes, ts.Seconds);
            }
        }

        public int BoxCount
        {
            get
            {
                return _boxCount;
            }

            set
            {
                _boxCount = value;
                BoxCountText = Localization.Get("BOX_COUNT", _boxCount);

                if (_boxCount < 1)
                {
                    if (!_blipsHidden)
                    {
                        foreach (Ped customer in Customers) customer.CurrentBlip.Alpha = 0;

                        Main.StoreBlips[StoreID].IsShortRange = false;
                        _blipsHidden = true;
                    }
                }
                else
                {
                    if (_blipsHidden)
                    {
                        foreach (Ped customer in Customers) customer.CurrentBlip.Alpha = 255;

                        UI.ShowSubtitle(Localization.Get("GO_DELIVER"), Constants.SubtitleTime);
                        Main.StoreBlips[StoreID].IsShortRange = true;
                        _blipsHidden = false;
                    }
                }
            }
        }

        // Private
        private bool _isRunning = false;
        private int _endTime = 0;
        private int _boxCount = 0;
        private bool _generatingPeds = false;
        private bool _blipsHidden = false;
        #endregion

        #region Methods
        public void DrawInfo()
        {
            if (_generatingPeds) return;

            float safeZone = Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE);
            float finalDrawX = Constants.UIDrawX - (1.0f - safeZone) * 0.5f;
            float finalDrawY = Constants.UIDrawY + (1.0f - safeZone) * 0.5f;

            Methods.DrawAlignedText(TimeText, finalDrawX, finalDrawY, 4, 0.65f);
            Methods.DrawAlignedText(BoxCountText, finalDrawX, finalDrawY + 0.05f, 4, 0.65f);
        }

        public void DrawPedMarkers(Vector3 position)
        {
            if (_generatingPeds) return;

            foreach (Ped customer in Customers)
            {
                if (customer.Position.DistanceTo(position) > Constants.MarkerDrawDistance) continue;

                World.DrawMarker(
                    MarkerType.ThickChevronUp,
                    customer.Position + new Vector3(0f, 0f, 1.3f),
                    Vector3.Zero,
                    new Vector3(180f, 0f, 0f),
                    new Vector3(0.65f, 0.65f, 0.65f),
                    Color.ForestGreen,
                    true,
                    false,
                    2,
                    false,
                    "",
                    "",
                    false
                );
            }
        }

        public void SpawnCustomers(int max)
        {
            if (!_generatingPeds)
            {
                _generatingPeds = true;

                int createdPeds = 0;
                int customerCount = Main.RNG.Next(1, max + 1);
                Vector3 playerPos = Game.Player.Character.Position;
                Vector3 spawnPos = Vector3.Zero;
                float spawnRange = 300f;

                while (createdPeds < customerCount)
                {
                    // credits to alexguirre
                    while ((spawnPos = World.GetSafeCoordForPed(playerPos.Around(spawnRange), true, 16)) == Vector3.Zero)
                    {
                        Script.Yield();
                        spawnRange -= 5f;
                    }

                    Ped ped = World.CreateRandomPed(spawnPos);
                    ped.IsInvincible = true;
                    ped.BlockPermanentEvents = true;
                    ped.AlwaysKeepTask = true;
                    Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped.Handle, "WORLD_HUMAN_STAND_IMPATIENT", 0, true);

                    Blip pedBlip = ped.AddBlip();
                    pedBlip.Sprite = BlipSprite.Friend;
                    pedBlip.Color = (BlipColor)2;
                    pedBlip.IsShortRange = false;
                    pedBlip.Scale = 1.0f;
                    pedBlip.Name = Localization.Get("CUSTOMER_NAME");

                    Customers.Add(ped);
                    createdPeds++;
                }

                _generatingPeds = false;
            }
        }

        public void BoxLogic(int time)
        {
            if (Box != null)
            {
                if (time - BoxThrowTime >= Constants.PizzaBoxLifeTime)
                {
                    Box.Delete();
                    Box = null;

                    if (!LastThrowSuccessful)
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, PlayerHandle, "GENERIC_CURSE_MED", "SPEECH_PARAMS_FORCE");
                    }

                    if (BoxCount < 1) UI.ShowSubtitle(Localization.Get("RETURN_TO_STORE_OUT"), Constants.SubtitleTime);

                    if (Customers.Count < 1)
                    {
                        Main.StoreBlips[StoreID].IsShortRange = false;
                        UI.ShowSubtitle(Localization.Get("RETURN_TO_STORE_DONE"), Constants.SubtitleTime);
                    }
                }
                else
                {
                    for (int i = Customers.Count - 1; i >= 0; i--)
                    {
                        if (Box.Position.DistanceTo(Customers[i].Position) <= 3f)
                        {
                            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Customers[i].Handle, "GENERIC_THANKS", "SPEECH_PARAMS_FORCE");

                            Customers[i].CurrentBlip.Remove();
                            Customers[i].Task.ClearAllImmediately();

                            using (TaskSequence tasks = new TaskSequence())
                            {
                                tasks.AddTask.GoTo(Box.Position, true, Constants.PizzaBoxLifeTime);
                                tasks.AddTask.PlayAnimation("anim@mp_snowball", "pickup_snowball");
                                tasks.AddTask.WanderAround();
                                tasks.Close();

                                Customers[i].Task.PerformSequence(tasks);
                            }

                            Customers[i].IsInvincible = false;
                            Customers[i].MarkAsNoLongerNeeded();
                            LastThrowSuccessful = true;

                            int money = Main.RNG.Next(Main.RewardBase, Main.RewardMax + 1);
                            Game.Player.Money += money;

                            UI.ShowSubtitle(Localization.Get("DELIVERED", money), Constants.SubtitleTime);
                            Customers.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        public void Start()
        {
            if (!Methods.CanUseMarkers() || Game.Player.Character.IsInVehicle()) return;

            int closest = Methods.FindClosestStore(Game.Player.Character.Position);
            if (closest == -1) return;

            Game.FadeScreenOut(Constants.WaitTime);
            Script.Wait(Constants.WaitTime);

            PlayerHandle = Game.Player.Character.Handle;
            Vehicle = World.CreateVehicle(Main.DeliveryVehicleModel, Game.Player.Character.Position, Game.Player.Character.Heading);
            Vehicle.PrimaryColor = VehicleColor.MetallicGreen;
            Vehicle.SecondaryColor = VehicleColor.MetallicRed;
            Game.Player.Character.SetIntoVehicle(Vehicle, VehicleSeat.Driver);

            SpawnCustomers(Main.MaxCustomers);

            for (int i = 0, max = Main.StoreBlips.Count; i < max; i++)
            {
                if (i == closest) continue;
                Main.StoreBlips[i].Alpha = 0;
            }

            ShowInfoUntil = Game.GameTime + Constants.SubtitleTime;
            JobEndTime = Game.GameTime + (Main.JobSeconds * 1000);
            StoreID = closest;
            BoxCount = Main.BoxCount;
            IsRunning = true;

            UI.ShowSubtitle(Localization.Get("GO_DELIVER"), Constants.SubtitleTime);
            Game.FadeScreenIn(Constants.WaitTime);
        }

        public void Stop()
        {
            Game.FadeScreenOut(Constants.WaitTime);
            Script.Wait(Constants.WaitTime);

            IsRunning = false;
            Game.FadeScreenIn(Constants.WaitTime);

            UI.ShowSubtitle(Localization.Get("DELIVERY_CANCELLED"), Constants.SubtitleTime);
        }

        public void ThrowBox(bool toLeft)
        {
            if (_isRunning && Box == null && Game.Player.Character.IsInVehicle() && _boxCount > 0)
            {
                LastThrowSuccessful = false;

                Box = World.CreateProp(Constants.PropName, Game.Player.Character.GetOffsetInWorldCoords(new Vector3((toLeft ? -1.0f : 1.0f), 0.5f, 0.15f)), new Vector3(0f, 0f, Game.Player.Character.Rotation.Z + (toLeft ? -90f : 90f)), true, false);
                Box.ApplyForce((toLeft ? -Game.Player.Character.RightVector : Game.Player.Character.RightVector) * 10.0f);

                BoxCount--;
                BoxThrowTime = Game.GameTime;
            }
        }
        #endregion
    }
}
