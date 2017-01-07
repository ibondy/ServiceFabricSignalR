namespace ScaleOut
{
    /// <summary>
    ///     Used to pass messages from SF to SignalR
    /// </summary>
    public class ScaleOutMessage
    {
        #region

        public string ConnectionId { get; set; }
        public string Payload { get; set; }

        #endregion
    }
}