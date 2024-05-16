using System.Text.Json.Serialization;
using Minio.Helpers;

namespace Minio.Model;

public class InfoMessage
{
    [JsonPropertyName("mode")] public string Mode { get; init; } = string.Empty;          
    [JsonPropertyName("domain")] public List<string> Domain { get; init; } = new();       
    [JsonPropertyName("region")] public string Region { get; init; } = string.Empty;      
    [JsonPropertyName("sqsARN")] public List<string> SQSARN { get; init; } = new();       
    [JsonPropertyName("deploymentID")] public string DeploymentID { get; init; } = string.Empty;
    [JsonPropertyName("buckets")] public required CountError Buckets { get; init; }
    [JsonPropertyName("objects")] public required CountError Objects { get; init; }
    [JsonPropertyName("versions")] public required CountError Versions { get; init; }
    [JsonPropertyName("deletemarkers")] public required CountError DeleteMarkers { get; init; }
    [JsonPropertyName("usage")] public required SizeError Usage { get; init; }         
    [JsonPropertyName("services")] public required Services Services { get; init; }   
    [JsonPropertyName("backend")] public required ErasureBackend Backend { get; init; }     
    [JsonPropertyName("servers")] public List<ServerProperties> Servers { get; init; } = new();
    [JsonPropertyName("pools")] public Dictionary<int,Dictionary<int,ErasureSetInfo>> Pools { get; init; } = new();
}

public class CountError
{
    [JsonPropertyName("count")] public long Count { get; init; }
    [JsonPropertyName("error")] public string? Error { get; init; }
}

public class SizeError
{
    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

public class Services
{
    // deprecated july 2023
    [JsonPropertyName("kms")] public Kms? Kms { get; init; } 
    [JsonPropertyName("kmsStatus")] public List<Kms> KmsStatus { get; init; } = new();
    [JsonPropertyName("ldap")] public Ldap?          Ldap   { get; init; }
    [JsonPropertyName("logger")] public List<TargetIdStatus> Logger { get; init; } = new();
    [JsonPropertyName("audit")] public List<TargetIdStatus> Audit   { get; init; } = new();

    [JsonPropertyName("notifications")]
    public Dictionary<string, List<TargetIdStatus>> Notifications { get; init; } = new();
}

public class ErasureBackend
{
    [JsonPropertyName("backendType")]
    public string Type { get; init; }

    [JsonPropertyName("onlineDisks")]
    public int OnlineDisks { get; init; }

    [JsonPropertyName("offlineDisks")]
    public int OfflineDisks { get; init; }

    // disks for currently configured Standard class.
    [JsonPropertyName("standardSCParity")]
    public int StandardSCParity { get; init; }

    // disks for currently configured Reduced Redundancy class.
    [JsonPropertyName("rrSCParity")]
    public int RRSCParity { get; init; }

    // Per pool information
    [JsonPropertyName("totalSets")]
    public List<int> TotalSets { get; init; } = new();

    [JsonPropertyName("totalDrivesPerSet")]
    public List<int> DrivesPerSet { get; init; } = new();
}


public class Kms
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("encrypt")]
    public string Encrypt { get; init; } = string.Empty;

    [JsonPropertyName("decrypt")]
    public string Decrypt { get; init; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}

public class Ldap
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

public class TargetIdStatus : Dictionary<string, StatusValue>
{
}

public class StatusValue
{
    [JsonPropertyName("status")] public string Status { get; init; }
}

public class ServerProperties
{
    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; } = string.Empty;

    [JsonPropertyName("scheme")]
    public string Scheme { get; init; } = string.Empty;

    [JsonPropertyName("uptime"), JsonConverter(typeof(SecTimeSpanJsonConverter))]
    public TimeSpan Uptime { get; init; }

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("commitID")]
    public string CommitID { get; init; } = string.Empty;

    [JsonPropertyName("network")]
    public Dictionary<string, string> Network { get; init; } = new();

    [JsonPropertyName("drives")]

    public List<Disk> Disks { get; init; } = new();

    [JsonPropertyName("poolNumber")]
    public int PoolNumber { get; init; } // Only set if len(PoolNumbers) == 1     

    [JsonPropertyName("poolNumbers")]
    public List<int> PoolNumbers { get; init; } = new();

    [JsonPropertyName("mem_stats")]
    public MemStats MemStats { get; init; }

    [JsonPropertyName("go_max_procs")]
    public int GoMaxProcs { get; init; }

    [JsonPropertyName("num_cpu")]
    public int NumCPU { get; init; }

    [JsonPropertyName("runtime_version")]
    public string RuntimeVersion { get; init; } = string.Empty;

    [JsonPropertyName("gc_stats")]
    public GCStats? GCStats { get; init; }

    [JsonPropertyName("minio_env_vars")]
    public Dictionary<string, string> MinioEnvVars { get; init; } = new();
}

public class Disk
{

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; } = string.Empty;

    [JsonPropertyName("rootDisk")]
    public bool RootDisk { get; init; }

    [JsonPropertyName("path")]
    public string DrivePath { get; init; } = string.Empty;

    [JsonPropertyName("healing")]
    public bool Healing { get; init; }

    [JsonPropertyName("scanning")]
    public bool Scanning { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("uuid")]
    public string UUID { get; init; } = string.Empty;

    [JsonPropertyName("major")]
    public int Major { get; init; }

    [JsonPropertyName("minor")]
    public int Minor { get; init; }

    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("totalspace")]
    public long TotalSpace { get; init; }

    [JsonPropertyName("usedspace")]
    public long UsedSpace { get; init; }

    [JsonPropertyName("availspace")]
    public long AvailableSpace { get; init; }

    [JsonPropertyName("readthroughput")]
    public double ReadThroughput { get; init; }

    [JsonPropertyName("writethroughput")]
    public double WriteThroughPut { get; init; }

    [JsonPropertyName("readlatency")]
    public double ReadLatency { get; init; }

    [JsonPropertyName("writelatency")]
    public double WriteLatency { get; init; }

    [JsonPropertyName("utilization")]
    public double Utilization { get; init; }

    [JsonPropertyName("metrics")]
    public DiskMetrics? Metrics { get; init; }

    [JsonPropertyName("heal_info")]
    public HealingDisk? HealInfo { get; init; }

    [JsonPropertyName("used_inodes")]
    public long UsedInodes { get; init; }

    [JsonPropertyName("free_inodes")]
    public long FreeInodes { get; init; }

    [JsonPropertyName("local")]
    public bool Local { get; init; }

    // Indexes, will be -1 until assigned a set.
    [JsonPropertyName("pool_index")]
    public int PoolIndex { get; init; } = -1;

    [JsonPropertyName("set_index")]
    public int SetIndex { get; init; } = -1;

    [JsonPropertyName("disk_index")]
    public int DiskIndex { get; init; } = -1;
}

public class DiskMetrics
{
    [JsonPropertyName("lastMinute")]
    public Dictionary<string, TimedAction> LastMinute { get; init; } = new();

    [JsonPropertyName("apiCalls")]
    public Dictionary<string, long> APICalls { get; init; } = new();

    // TotalTokens set per drive max concurrent I/O.
    [JsonPropertyName("totalTokens")] public int TotalTokens { get; init; } 

    // TotalWaiting the amount of concurrent I/O waiting on disk
    [JsonPropertyName("totalWaiting")]
    public int TotalWaiting { get; init; }

    // Captures all data availability errors such as
    // permission denied, faulty disk and timeout errors.
    [JsonPropertyName("totalErrorsAvailability")] public long TotalErrorsAvailability { get; set;}

    // Captures all timeout only errors
    [JsonPropertyName("totalErrorsTimeout")] public long TotalErrorsTimeout { get; set;}

    // Total writes on disk (could be empty if the feature
    // is not enabled on the server)
    [JsonPropertyName("totalWrites")] public long TotalWrites { get; set; }

    // Total deletes on disk (could be empty if the feature
    // is not enabled on the server)
    [JsonPropertyName("totalDeletes")] public long TotalDeletes { get; set; }
}

public class TimedAction
{
    [JsonPropertyName("count")]
    public long Count { get; init; }

    [JsonPropertyName("acc_time_ns")]
    public long AccTime { get; init; }

    [JsonPropertyName("bytes")]
    public long Bytes { get; init; }
}

public class HealingDisk
{
    [JsonPropertyName("id")]
    public string ID { get; init; } = string.Empty;

    [JsonPropertyName("heal_id")]
    public string HealID { get; init; } = string.Empty;

    [JsonPropertyName("pool_index")]
    public int PoolIndex { get; init; }

    [JsonPropertyName("set_index")]
    public int SetIndex { get; init; }

    [JsonPropertyName("disk_index")]
    public int DiskIndex { get; init; }

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; }

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("started")]
    public DateTime Started { get; init; }

    [JsonPropertyName("last_update")]
    public DateTime LastUpdate { get; init; }

    [JsonPropertyName("objects_total_count")]
    public long ObjectsTotalCount { get; init; }

    [JsonPropertyName("objects_total_size")]
    public long ObjectsTotalSize { get; init; }

    [JsonPropertyName("items_healed")]
    public long ItemsHealed { get; init; }

    [JsonPropertyName("items_failed")]
    public long ItemsFailed { get; init; }

    [JsonPropertyName("items_skipped")]
    public long ItemsSkipped { get; init; }

    [JsonPropertyName("bytes_done")]
    public long BytesDone { get; init; }

    [JsonPropertyName("bytes_failed")]
    public long BytesFailed { get; init; }

    [JsonPropertyName("bytes_skipped")]
    public long BytesSkipped { get; init; }

    // Last object scanned.
    [JsonPropertyName("current_bucket")]
    public string Bucket { get; init; } = string.Empty;

    [JsonPropertyName("current_object")]
    public string Object { get; init; } = string.Empty;

    // Filled on startup/restarts.
    [JsonPropertyName("queued_buckets")]
    public List<string> QueuedBuckets { get; init; } = new();

    // Filled during heal.
    [JsonPropertyName("healed_buckets")]
    private List<string> HealedBuckets { get; init; } = new();
}

public class MemStats
{
    public long Alloc { get; init; }
    public long TotalAlloc { get; init; }
    public long Mallocs { get; init; }
    public long Frees { get; init; }
    public long HeapAlloc { get; init; }
}

// GCStats collect information about recent garbage collections.
public class GCStats
{
    // time of last collection
    [JsonPropertyName("last_gc")]
    public DateTime LastGC { get; init; }

    // number of garbage collections
    [JsonPropertyName("num_gc")]
    public long NumGC { get; init; }

    // total pause for all collections
    [JsonPropertyName("pause_total"), JsonConverter(typeof(NanoSecTimeSpanJsonConverter))]
    public TimeSpan PauseTotal { get; init; }

    // pause history, most recent first
    [JsonPropertyName("pause"), JsonConverter(typeof(JsonCollectionItemConverter<TimeSpan, NanoSecTimeSpanJsonConverter>))]
    public List<TimeSpan> Pause { get; init; } = new();

    [JsonPropertyName("pause_end")]
    public List<DateTime> PauseEnd { get; init; } = new(); // pause end times history, most recent first
}

public class ErasureSetInfo
{
    [JsonPropertyName("id")]
    public int ID { get; init; }

    [JsonPropertyName("rawUsage")]
    public long RawUsage { get; init; }

    [JsonPropertyName("rawCapacity")]
    public long RawCapacity { get; init; }

    [JsonPropertyName("usage")]
    public long Usage { get; init; }

    [JsonPropertyName("objectsCount")]
    public long ObjectsCount { get; init; }

    [JsonPropertyName("versionsCount")]
    public long VersionsCount { get; init; }

    [JsonPropertyName("deleteMarkersCount")]
    public long DeleteMarkersCount { get; init; }

    [JsonPropertyName("healDisks")]
    public int HealDisks { get; init; }
}
