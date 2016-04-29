namespace rt.Devices.RsScale.Types
{
    /// <summary>
    /// A accounting record
    /// </summary>
    public class AccountingItem
    {
        /// <summary>
        /// Denomination as lowest currency unit
        /// </summary>
        public decimal Denomination { get; set; }

        /// <summary>
        /// Quantity of current denomination
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Single piece weight
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// The type of denomination
        /// </summary>
        public CashType CashType { get; set; }
    }
}
