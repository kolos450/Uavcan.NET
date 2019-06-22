namespace CanardSharp.Drivers.Slcan
{
    /// <summary>
    /// Modes for opening a CAN channel.
    /// </summary>
    public enum UsbTinOpenMode
    {
        /// <summary>
        /// Send and receive on CAN bus
        /// </summary>
        Active,

        /// <summary>
        /// Listen only, sending messages is not possible.
        /// </summary>
        Listenonly,

        /// <summary>
        /// Loop back the sent CAN messages. Disconnected from physical CAN bus.
        /// </summary>
        Loopback
    }
}
