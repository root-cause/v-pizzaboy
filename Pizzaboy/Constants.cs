using System.Collections.Generic;
using GTA.Math;

namespace Pizzaboy
{
    public static class Constants
    {
        public const string PropName = "prop_pizza_box_01";

        public const int WaitTime = 1000;
        public const int PizzaBoxLifeTime = 3000;
        public const int SubtitleTime = 5000;

        public const float MarkerDrawDistance = 75f;
        public const float UIDrawX = 0.9999f;
        public const float UIDrawY = 0.25f;

        public static List<Vector3> StoreCoords = new List<Vector3>
        {
            new Vector3(480.827f, 75.32801f, 96.8651f),
            new Vector3(537.1926f, 100.4036f, 96.5052f),
            new Vector3(287.6422f, -962.8013f, 29.41847f),
            new Vector3(-1529.7f, -909.4878f, 10.16166f)
        };
    }
}
