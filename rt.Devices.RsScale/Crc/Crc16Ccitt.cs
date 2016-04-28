namespace rt.Devices.RsScale.Crc
{
    internal class Crc16Ccitt
    {
        private const ushort Polynom = 0x1021;
        private readonly ushort[] _table = new ushort[256];
        private readonly ushort _initialValue;

        public ushort ComputeChecksum(byte[] bytes)
        {
            var crc = _initialValue;
            for (var i = 0; i < bytes.Length; i++)
            {
                crc = (ushort)((crc << 8) ^ _table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }

            return crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes)
        {
            var crc = ComputeChecksum(bytes);
            return new[] { (byte)(crc >> 8), (byte)(crc & 0x00ff) };
        }

        public Crc16Ccitt(InitialCrcValue initialValue)
        {
            _initialValue = (ushort)initialValue;
            for (var i = 0; i < _table.Length; i++)
            {
                ushort temp = 0;
                var a = (ushort)(i << 8);
                for (var j = 0; j < 8; j++)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                    {
                        temp = (ushort)((temp << 1) ^ Polynom);
                    }
                    else
                    {
                        temp <<= 1;
                    }
                    a <<= 1;
                }
                _table[i] = temp;
            }
        }
    }
}
