namespace rt.Devices.RsScale.Types
{
    /// <summary>
    /// The type of denomination
    /// </summary>
    public enum CashType : byte
    {
        /// <summary>
        /// A coin
        /// </summary>
        Coin = 0x0,

        /// <summary>
        /// A roll of coins
        /// </summary>
        CoinRoll = 0x040,

        /// <summary>
        /// A banknote
        /// </summary>
        Banknote = 0x80
    }
}
