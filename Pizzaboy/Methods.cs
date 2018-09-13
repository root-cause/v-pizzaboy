using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Pizzaboy
{
    public static class Methods
    {
        public static bool CanUseMarkers()
        {
            return !Game.MissionFlag && Game.Player.IsAlive && Game.Player.WantedLevel < 1;
        }

        public static int FindClosestStore(Vector3 position, float range = 2f)
        {
            float minDistance = 9999f;
            int returnValue = -1;

            for (int i = 0, max = Constants.StoreCoords.Count; i < max; i++)
            {
                float dist = Constants.StoreCoords[i].DistanceTo(position);
                if (dist > range) continue;

                if (dist < minDistance)
                {
                    minDistance = dist;
                    returnValue = i;
                }
            }

            return returnValue;
        }

        public static void DisplayHelpText(string message)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "CELL_EMAIL_BCON");
            for (int i = 0, maxStringLength = 99; i < message.Length; i += maxStringLength)
            {
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message.Substring(i, Math.Min(maxStringLength, message.Length - i)));
            }

            Function.Call(Hash._0x238FFE5C7B0498A6, 0, 0, 1, -1);
        }

        public static void DrawAlignedText(string message, float drawX, float drawY, int font, float scale)
        {
            Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
            for (int i = 0, maxStringLength = 99; i < message.Length; i += maxStringLength)
            {
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message.Substring(i, Math.Min(maxStringLength, message.Length - i)));
            }

            Function.Call(Hash.SET_TEXT_FONT, font);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash.SET_TEXT_OUTLINE);
            Function.Call(Hash.SET_TEXT_RIGHT_JUSTIFY, true);
            Function.Call(Hash.SET_TEXT_WRAP, 0, drawX);
            Function.Call(Hash._DRAW_TEXT, drawX, drawY);
        }
    }
}
