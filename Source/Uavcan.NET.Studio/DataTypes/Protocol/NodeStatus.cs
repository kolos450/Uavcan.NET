using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol
{
    /// <summary>
    /// Abstract node status information.
    /// All UAVCAN nodes are required to publish this message periodically.
    /// </summary>
    [DataContract(Name = "NodeStatus", Namespace = "uavcan.protocol")]
    public sealed class NodeStatus
    {
        public const ushort MaxBroadcastingPeriodMs = 1000;
        public const ushort MinBroadcastingPeriodMs = 2;

        /// <summary>
        /// If a node fails to publish this message in this amount of time, it should be considered offline.
        /// </summary>
        public const ushort OfflineTimeoutMs = 3000;

        /// <summary>
        /// Uptime counter should never overflow.
        /// </summary>
        [DataMember(Name = "uptime_sec")]
        public uint UptimeSec { get; set; }

        [DataMember(Name = "health")]
        public NodeHealth Health { get; set; }

        /// <summary>
        /// Current mode.
        /// </summary>
        [DataMember(Name = "mode")]
        public NodeMode Mode { get; set; }

        /// <summary>
        /// Not used currently, keep zero when publishing, ignore when receiving.
        /// </summary>
        [DataMember(Name = "sub_mode")]
        public byte SubMode { get; set; }

        /// <summary>
        /// Optional, vendor-specific node status code, e.g. a fault code or a status bitmask.
        /// </summary>
        [DataMember(Name = "vendor_specific_status_code")]
        public ushort VendorSpecificStatusCode { get; set; }
    }

    /// <summary>
    /// Abstract node health.
    /// </summary>
    public enum NodeHealth : byte
    {
        /// <summary>
        /// The node is functioning properly.
        /// </summary>
        Ok = 0,

        /// <summary>
        /// A critical parameter went out of range or the node encountered a minor failure.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// The node encountered a major failure.
        /// </summary>
        Error = 2,

        /// <summary>
        /// The node suffered a fatal malfunction.
        /// </summary>
        Critical = 3,
    }

    /// <summary>
    /// Current mode.
    /// </summary>
    /// <remarks>
    /// Mode OFFLINE can be actually reported by the node to explicitly inform other network
    /// participants that the sending node is about to shutdown. In this case other nodes will not
    /// have to wait OFFLINE_TIMEOUT_MS before they detect that the node is no longer available.
    /// 
    /// Reserved values can be used in future revisions of the specification.
    /// </remarks>
    public enum NodeMode : byte
    {
        /// <summary>
        /// Normal operating mode.
        /// </summary>
        Operational = 0,

        /// <summary>
        /// Initialization is in progress; this mode is entered immediately after startup.
        /// </summary>
        Initialization = 1,

        /// <summary>
        /// E.g. calibration, the bootloader is running, etc.
        /// </summary>
        Maintenance = 2,

        /// <summary>
        /// New software/firmware is being loaded.
        /// </summary>
        Software_update = 3,

        /// <summary>
        /// The node is no longer available.
        /// </summary>
        Offline = 7,
    }
}
