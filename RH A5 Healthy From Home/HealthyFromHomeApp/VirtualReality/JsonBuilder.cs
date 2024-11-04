using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client.Virtual_Reality
{
    public class JsonBuilder
    {
        // This class contains all of the JSON objects that are sent to the VR Server
        public static object GetStartingPacketData()
        {
            return new { id = "session/list", data = new { } };
        }

        public static object GetSessionIdPacketData(string sessionId)
        {
            return new { id = "tunnel/create", data = new { session = sessionId, key = "" } };
        }

        public static object GetTunnelCommandData(string tunnelId, string command, object jsonCommandData)
        {
            if (jsonCommandData != null)
            {
                return new { id = "tunnel/send", data = new { dest = tunnelId, data = new { id = command, data = jsonCommandData } } };
            } else
            {
                return new { id = "tunnel/send", data = new { dest = tunnelId, data = new { id = command, data = new { } } } };
            }
        }

        public static object EmptyObjectData()
        {
            return new { };
        }

        public static object GetSkyBoxTimeData(int timeTochange)
        {
            return new { time = timeTochange };
        }

        public static object GetTerrainData(int terrainSize)
        {
            // Create (wavy)terrain using cosinwaves
            float[,] heights = new float[terrainSize, terrainSize];
            for (int x = 0; x < terrainSize; x++)
                for (int y = 0; y < terrainSize; y++)
                    heights[x, y] = 2 + (float)(Math.Cos(x / 5.0) + Math.Cos(y / 5.0));
            
            // Return height casted to fit the format
            return new { size = new[] { terrainSize, terrainSize }, 
                heights = heights.Cast<float>().ToArray() };
        }

        public static object CreateTerrainData(string nameComponent, int[] positions)
        {
            return new { name = nameComponent, components = new {
                transform = new { position = positions, scale = 1 },
                terrain = new { } } };
        }

        public static object CreatePanelData(string nameComponent, string parentId, int[] positions, int[] rotations)
        {
            return new { name = nameComponent, parent = parentId, components = new {
                transform = new { position = positions, scale = 1, rotation = rotations },
                panel = new { size = new[] { 1, 1 }, resolution = new[] { 512, 512 }, background = new[] { 1, 1, 1, 1 }, castShadow = true } } };
        }

        public static object GeneralPanelData(string panelId)
        {
            return new { id = panelId };
        }

        public static object SetPanelColorData(string panelId, int[] rgbColor)
        {
            return new { id = panelId, color = rgbColor };
        }

        public static object DrawTextOnPanelData(string panelId, string setText, double[] textPosition)
        {
            return new { id = panelId, text = setText, position = textPosition, size = 128 };
        }

        public static object AddRoadsData(string routeUUID)
        {
            return new { route = routeUUID };
        }

        public static object CreateModelData(string fileName, string nameCreation, string parentId, int[] positions, int[] rotations)
        {
            return new { name = nameCreation, parent = parentId, components = new { 
                transform = new { position = positions, scale = 1, rotation = rotations },
                model = new { file = fileName, cullbackfaces = true } } };
        }

        public static object LetItemFollowRouteData(string routeUUID, string itemGUID, string rotatation, int speedObject, int[] rotationsOffset, int[] positionsOffset)
        {
            return new { route = routeUUID,  node = itemGUID, speed = speedObject, offset = 0.0, rotate = rotatation, smoothing = 1.0, 
                followHeight = true, rotateOffset = rotationsOffset, positionOffset = positionsOffset};
        }

        public static object FindNodeData(string nodeName)
        {
            return new { name = nodeName };
        }

        public static object DeleteNodeData(string nodeId)
        {
            return new { id = nodeId };
        }

        public static object UpdateNodeSpeed(string nodeId, double newSpeed)
        {
            return new { node = nodeId, speed = newSpeed };
        }

        public static object GetRouteData()
        {
            return new {
                nodes = new[] {
                    new { pos = new[] { 0, 0, 0 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 15, 0,0 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 40, 0, 40 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 45, 0, 39 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 50, 0, 45 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 90, 0, 45 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 95, 0, 50 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 95, 0, 55 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 90, 0, 57 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 80, 0, 60 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 70, 0, 60 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 40, 0, 60 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 40, 0, 55 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 30, 0, 58 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 25, 0, 57 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 20, 0, 50 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 15, 0, 30 }, dir = new[] { 0, 0, 0 } },
                    new { pos = new[] { 10, 0, 30 }, dir = new[] { 0, 0, 0 } } } };
        }
    }
}
