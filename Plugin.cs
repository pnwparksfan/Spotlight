﻿namespace Spotlight
{
    // System
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;

    // RPH
    using Rage;
    
    using Spotlight.SpotlightControllers;

    internal static class Plugin
    {
        public static Settings Settings { get; private set; }

        public static readonly List<VehicleSpotlight> Spotlights = new List<VehicleSpotlight>();
        public static readonly List<SpotlightController> SpotlightControllers = new List<SpotlightController>();

        private static void Main()
        {
            while (Game.IsLoading)
                GameFiber.Sleep(500);

            if (!Directory.Exists(@"Plugins\Spotlight Resources\"))
                Directory.CreateDirectory(@"Plugins\Spotlight Resources\");

            Settings = new Settings(@"Plugins\Spotlight Resources\General.ini",
                                    @"Plugins\Spotlight Resources\Offsets.ini",
                                    @"Plugins\Spotlight Resources\Spotlight Data - Cars.xml",
                                    @"Plugins\Spotlight Resources\Spotlight Data - Helicopters.xml",
                                    @"Plugins\Spotlight Resources\Spotlight Data - Boats.xml",
                                    true);

            LoadSpotlightControllers();

            Game.FrameRender += OnRawFrameRenderDrawCoronas;
            
            while (true)
            {
                GameFiber.Yield();
                
                Update();
            }
        }

        private static void Update()
        {
            if (Game.IsKeyDown(Settings.ToggleSpotlightKey))
            {
                VehicleSpotlight s = GetPlayerCurrentVehicleSpotlight();

                if (s != null)
                {
                    s.IsActive = !s.IsActive;
                }
            }

            
            for (int i = Spotlights.Count - 1; i >= 0; i--)
            {
                VehicleSpotlight s = Spotlights[i];
                if (!s.Vehicle || s.Vehicle.IsDead)
                {
                    Spotlights.Remove(s);
                    continue;
                }

                s.Update(SpotlightControllers);
            }
        }

        private static unsafe void OnRawFrameRenderDrawCoronas(object sender, GraphicsEventArgs e)
        {
            for (int i = 0; i < Spotlights.Count; i++)
            {
                VehicleSpotlight s = Spotlights[i];
                if (s.IsActive)
                {
                    Utility.DrawCorona(s.Position, s.Direction, s.Data.Color);
                }
            }
        }

        private static void OnUnload(bool isTerminating)
        {
            if (!isTerminating)
            {
                // native calls: delete entities, blips, etc.
            }

            // dispose objects
            Spotlights.Clear();
        }


        private static VehicleSpotlight GetPlayerCurrentVehicleSpotlight()
        {
            Vehicle v = Game.LocalPlayer.Character.CurrentVehicle;
            if (v)
            {
                return GetVehicleSpotlight(v);
            }

            return null;
        }

        private static VehicleSpotlight GetVehicleSpotlight(Vehicle vehicle)
        {
            VehicleSpotlight s = Spotlights.FirstOrDefault(l => l.Vehicle == vehicle);

            if (s != null)
                return s;

            s = new VehicleSpotlight(vehicle);
            Spotlights.Add(s);
            return s;
        }

        private static void LoadSpotlightControllers()
        {
            IEnumerable<Type> types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();

            foreach (Type type in types)
            {
                if (!type.IsAbstract && !type.IsInterface && typeof(SpotlightController).IsAssignableFrom(type))
                {
                    string iniKeyName = type.Name.Replace("SpotlightController", "") + "ControlsEnabled";
                    if (Settings.GeneralSettingsIniFile.DoesKeyExist("Controls", iniKeyName) && Settings.GeneralSettingsIniFile.ReadBoolean("Controls", iniKeyName, false))
                    {
                        SpotlightController c = (SpotlightController)Activator.CreateInstance(type, true);
                        SpotlightControllers.Add(c);
                    }
                }
            }
        }
    }
}
