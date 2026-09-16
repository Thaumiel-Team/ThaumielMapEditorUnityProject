using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Assets.Scripts.Collab
{
    public static class CollabTypes
    {
        public const string Hello = "hello";
        public const string Welcome = "welcome";
        public const string SnapshotChunk = "snapshot_chunk";
        public const string SnapshotDone = "snapshot_done";
        public const string Create = "create";
        public const string Delete = "delete";
        public const string Reparent = "reparent";
        public const string Transform = "transform";
        public const string Props = "props";
        public const string Selection = "selection";
        public const string View = "view";
        public const string Presence = "presence";
        public const string Ping = "ping";
        public const string Pong = "pong";
        public const string Ack = "ack";
        public const string Error = "error";
        public const string Bye = "bye";
        public const string JoinDenied = "join_denied";

        public static bool IsReliable(string type)
        {
            return type switch
            {
                Transform or Selection or View or Presence or Ping or Pong or Ack => false,
                _ => true,
            };

        }
    }

    [Serializable]
    public class CollabEnvelope
    {
        public int v = 1;
        public string type = string.Empty;
        public string msgId = string.Empty; // N Guid
        public string sender = string.Empty; // userId N Guid
        public string senderName = string.Empty;
        public string senderColor = "#33AAFF";
        public long seq; // host assigned sequence
        public long ts; // DateTime.UtcNow.Ticks
        public string ackFor = string.Empty; // for Ack type
        public string data = string.Empty; // JSON of inner payload
    }

    [Serializable]
    public class SyncField
    {
        public string key = string.Empty;
        public string json = "null";
    }

    [Serializable]
    public class SyncToolState
    {
        public int toolType;
        public List<SyncField> fields = new();
    }

    [Serializable]
    public class SyncObject
    {
        public string guid = string.Empty;
        public string parentGuid = string.Empty;
        public string builderGuid = string.Empty;
        public int objectType;
        public string prefabPath = string.Empty; // sender resolved, receiver validates
        public string prefabHint = string.Empty; // subtype
        public string name = string.Empty;
        public float[] pos = new float[3];
        public float[] rot = new float[3];
        public float[] scale = new float[] { 1, 1, 1 };
        public bool isStatic;
        public float movementSmoothing;
        public float[] culling; // null or [x,y,z]
        public string animatorName = string.Empty;
        public bool serverSide;
        public int siblingIndex;
        public List<SyncField> fields = new();
        public List<SyncToolState> tools = new();
    }

    [Serializable]
    public class HelloData
    {
        public string userId = string.Empty;
        public string userName = string.Empty;
        public string color = "#33AAFF";
        public string unityVersion = string.Empty;
    }

    [Serializable]
    public class WelcomeData
    {
        public string snapshotId = string.Empty;
        public int totalChunks;
        public int builderCount;
        public long hostSeq;
    }

    [Serializable]
    public class SnapshotChunkData
    {
        public string snapshotId = string.Empty;
        public int index;
        public int total;
        public List<SyncObject> objects = new();
        public List<BuilderInfo> builders = new();
    }

    [Serializable]
    public class BuilderInfo
    {
        public string guid = string.Empty;
        public string name = string.Empty;
        public float[] pos = new float[3];
        public float[] rot = new float[3];
        public float[] scale = new float[] { 1, 1, 1 };
    }

    [Serializable]
    public class DeleteData
    {
        public string guid = string.Empty;
    }

    [Serializable]
    public class ReparentData
    {
        public string guid = string.Empty;
        public string parentGuid = string.Empty;
        public string builderGuid = string.Empty;
        public int siblingIndex;
    }

    [Serializable]
    public class TransformData
    {
        public string guid = string.Empty;
        public float[] pos = new float[3];
        public float[] rot = new float[3];
        public float[] scale = new float[] { 1, 1, 1 };
    }

    [Serializable]
    public class PropsData
    {
        public string guid = string.Empty;
        public string name = string.Empty;
        public bool isStatic;
        public float movementSmoothing;
        public float[] culling;
        public string animatorName = string.Empty;
        public bool serverSide;
        public List<SyncField> fields = new();
        public List<SyncToolState> tools = new();
    }

    [Serializable]
    public class SelectionData
    {
        public List<string> guids = new();
    }

    [Serializable]
    public class ViewData
    {
        public float[] pivot = new float[3];
        public float[] camPos = new float[3];
    }

    [Serializable]
    public class UserInfo
    {
        public string id = string.Empty;
        public string name = string.Empty;
        public string color = "#33AAFF";
        public int pingMs;
    }

    [Serializable]
    public class PresenceData
    {
        public List<UserInfo> users = new(); 
    }

    [Serializable]
    public class ErrorData
    {
        public string reason = string.Empty; 
    }

    public static class CollabJson
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            Culture = CultureInfo.InvariantCulture,
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public static string Serialize(object o)
        {
            return JsonConvert.SerializeObject(o, Settings);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        public static string NewMsgId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static long NowTicks()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}
