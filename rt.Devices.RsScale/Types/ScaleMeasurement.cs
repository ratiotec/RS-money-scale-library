namespace rt.Devices.RsScale.Types
{
    public class ScaleMeasurement
    {
        public decimal Denomination { get; set; }
        public int Quantity { get; set; }
        public double Weight { get; set; }
        public CashType CashType { get; set; }
    }
}
