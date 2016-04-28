using rt.Devices.RsScale.Crc;
using rt.Devices.RsScale.Types;
using rt.Hid.v2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using rt.Devices.RsScale.ExtensionsMethods;

namespace rt.Devices.RsScale
{
    public class RsScale : IDisposable
    {
        // Legacy dev configuration
        private const int VidDev = 0x0fff;
        private const int PidDev = 0x0002;

        // Current release configuration
        private const int VidRel = 0x16d0;
        private const int PidRel = 0x050b;

        /// <summary>
        /// Maximum of available currencies
        /// </summary>
        public const int MaxCurrencies = 3;

        /// <summary>
        /// Maximum of denomination text length
        /// </summary>
        public const int MaxDenominationTextLength = 8;

        /// <summary>
        /// Default timeout (10 seconds)
        /// </summary>
        public const int Timeout = 10000;

        private HidDevice _device;
        private int _protocolVersion;

        /// <summary>
        /// Is a scale cable-connected
        /// </summary>
        public bool IsConnected => _device?.IsConnected ?? false;

        /// <summary>
        /// Is a connection established
        /// </summary>
        public bool IsOpen => _device?.IsOpen ?? false;

        /// <summary>
        /// Contains a seperate memory for coin rolls
        /// </summary>
        /// <returns></returns>
        public bool ContainsRollMemory => _protocolVersion == 5 || _protocolVersion == 6;

        /// <summary>
        /// Opens a connected scale
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync() => await OpenAsync(Timeout);

        /// <summary>
        /// Opens a connected scale
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task OpenAsync(int timeout)
        {
            if (IsOpen)
                return;

            var devices = HidDevices.Enumerate(VidDev, PidDev).ToList();
            if (!devices.Any())
                devices = HidDevices.Enumerate(VidRel, PidRel).ToList();

            var device = devices.FirstOrDefault();
            if (device != null && device.IsConnected && !device.IsOpen)
            {
                _device = device;
                device.OpenDevice();
                _protocolVersion = await GetProtocolVersionAsync(timeout);
            }
        }

        /// <summary>
        /// Opens a connected scale
        /// </summary>
        public void Open() => Open(Timeout);

        /// <summary>
        /// Opens a connected scale
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        public void Open(int timeout) => OpenAsync(timeout).RunTaskSynchronously();

        /// <summary>
        /// Close an opened scale connection
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync() => await CloseAsync(Timeout);

        /// <summary>
        /// Close an opened scale connection
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task CloseAsync(int timeout)
        {
            if (IsOpen)
            {
                byte[] closeCommand = { 0xf6, 0x00, 0x55, 0xaa, 0x55, 0xaa };

                await SendPacketAsync(closeCommand, timeout);
                _device.CloseDevice();
            }
        }

        /// <summary>
        /// Close an opened scale connection
        /// </summary>
        public void Close() => Close(Timeout);

        /// <summary>
        /// Close an opened scale connection
        /// </summary>
        /// <param name="timeout">milliseconds</param>c
        public void Close(int timeout) => CloseAsync(timeout).RunTaskSynchronously();
        
        /// <summary>
        /// Gets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <returns></returns>
        public async Task<string> GetCurrencyNameAsync(int currencyIndex) => await GetCurrencyNameAsync(currencyIndex, Timeout);

        /// <summary>
        /// Gets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task<string> GetCurrencyNameAsync(int currencyIndex, int timeout)
        {
            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            if (currencyIndex < 0 || currencyIndex > MaxCurrencies)
                throw new ArgumentException($"Currency currencyIndex has to be between 0-{MaxCurrencies}.");

            //if (currencyIndex == 0)
            //    currencyIndex = await GetCurrentCurrencyPositionAsync(timeout);

            var currencyName = new StringBuilder();
            for (byte i = 1; i <= 3; i++)
            {
                byte[] currentCharCommand = { 0xfc, (byte)currencyIndex, i, 0xaa, 0x55, 0xaa };
                var buffer = await SendPacketAsync(currentCharCommand, timeout);

                // Protocol 1 uses reverse byte order
                currencyName.Append(_protocolVersion == 1
                    ? GetLetterChar(buffer[4], buffer[3])
                    : GetLetterChar(buffer[3], buffer[4]));
            }

            return currencyName.ToString();
        }

        /// <summary>
        /// Gets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <returns></returns>
        public string GetCurrencyName(int currencyIndex) => GetCurrencyName(currencyIndex, Timeout);

        /// <summary>
        /// Gets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="timeout">milliseconds</param>
        public string GetCurrencyName(int currencyIndex, int timeout) => GetCurrencyNameAsync(currencyIndex, timeout).RunTaskSynchronously();

        /// <summary>
        /// Sets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="name">three letter currency ISO code</param>
        /// <returns></returns>
        public async Task SetCurrencyNameAsync(int currencyIndex, string name) => await SetCurrencyNameAsync(currencyIndex, name, Timeout);

        /// <summary>
        /// Sets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="name">three letter currency ISO code</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task SetCurrencyNameAsync(int currencyIndex, string name, int timeout)
        {
            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            if (currencyIndex < 0 || currencyIndex > MaxCurrencies + 1)
                throw new ArgumentException($"Currency currencyIndex has to be between 0-{MaxCurrencies + 1}.");

            if (currencyIndex == MaxCurrencies + 1 && _protocolVersion < 6)
                throw new ArgumentException($"{MaxCurrencies + 1} currencyIndex is not available.");

            if (string.IsNullOrEmpty(name) || name.Length != 3)
                throw new ArgumentException("New currencyIndex name has to be 3 character in size.");

            if (currencyIndex == 0)
                currencyIndex = await GetCurrentCurrencyPositionAsync(timeout);

            for (var i = 0; i < 3; i++)
            {
                var letter = GetLetterCode(name[i].ToString());
                var nameCommand = new byte[] { 0xff, (byte)currencyIndex, (byte)(i + 1), letter[0], letter[1], 0xaa };
                await SendPacketAsync(nameCommand, timeout);
            }
        }

        /// <summary>
        /// Sets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="name">three letter currency ISO code</param>
        /// <returns></returns>
        public void SetCurrencyName(int currencyIndex, string name) => SetCurrencyName(currencyIndex, name, Timeout);

        /// <summary>
        /// Sets the currency name as three letter ISO code
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="name">three letter currency ISO code</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public void SetCurrencyName(int currencyIndex, string name, int timeout) => SetCurrencyNameAsync(currencyIndex, name, timeout).RunTaskSynchronously();

        /// <summary>
        /// Gets a list with measured denomination details for the current currency
        /// </summary>
        /// <returns></returns>
        public async Task<IList<ScaleMeasurement>> GetValuesAsync() => await GetValuesAsync(Timeout);

        /// <summary>
        /// Gets a list with measured denomination details for the current currency
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task<IList<ScaleMeasurement>> GetValuesAsync(int timeout)
        {
            return await GetValuesAsync(0, timeout);
        }

        /// <summary>
        /// Gets a list with measured denomination details for the current currency
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task<IList<ScaleMeasurement>> GetValuesAsync(int currencyIndex, int timeout)
        {
            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            if (currencyIndex < 0 || currencyIndex > MaxCurrencies)
                throw new ArgumentException($"Currency currencyIndex has to be between 0-{MaxCurrencies}.");

            var results = new List<ScaleMeasurement>();
            int currentValue;
            byte denominationIndex = 0;

            if (currencyIndex == 0)
                currencyIndex = await GetCurrentCurrencyPositionAsync(timeout);

            do
            {
                // Get current denominationIndex value
                byte[] denominationValueCommand = { 0xfb, 0x0, 0x55, 0xaa, 0x55, 0xaa };
                denominationValueCommand[1] = (byte)((currencyIndex << 6) | ++denominationIndex); // currencyIndex + current denominationIndex
                var receiveBufferDenominationValue = await SendPacketAsync(denominationValueCommand, timeout);
                byte[] valueBuffer = { receiveBufferDenominationValue[5], receiveBufferDenominationValue[4], receiveBufferDenominationValue[3], receiveBufferDenominationValue[2] };
                currentValue = BitConverter.ToInt32(valueBuffer, 0);

                // No more denominations exist
                if (currentValue <= 0)
                    break;

                // Get current denominationIndex weight
                byte[] denominationWeightCommand = { 0xfa, (byte)currencyIndex, denominationIndex, 0xaa, 0x55, 0xaa };
                var receiveBufferDenominationWeight = await SendPacketAsync(denominationWeightCommand, timeout);

                byte[] weightBuffer = { receiveBufferDenominationWeight[5], receiveBufferDenominationWeight[4], receiveBufferDenominationWeight[3], 0x0 };
                weightBuffer[2] = weightBuffer[2] >= 128 ? (byte)(weightBuffer[2] - 128) : weightBuffer[2];
                var currentWeight = Convert.ToDouble(BitConverter.ToInt32(weightBuffer.ToArray(), 0) * 0.0001);

                if (currentWeight <= 0)
                    break;

                // Get current denominationIndex quantity
                byte[] denominationQtyCommand = { 0xf9, (byte)(denominationIndex + 0x40), 0x55, 0xaa, 0x55, 0xaa };
                var receiveBufferDenominationQty = await SendPacketAsync(denominationQtyCommand, timeout);
                byte[] qtyBuffer = { receiveBufferDenominationQty[5], receiveBufferDenominationQty[4], receiveBufferDenominationQty[3], receiveBufferDenominationQty[2] };
                var currentQty = BitConverter.ToInt32(qtyBuffer, 0);

                // Get current denominationIndex type
                var currentCashType = await GetCashTypeAsync(currencyIndex, denominationIndex, timeout);
                results.Add(new ScaleMeasurement { Denomination = currentValue, Weight = currentWeight, Quantity = currentQty, CashType = currentCashType });
            } while (currentValue > 0);

            return results;
        }

        /// <summary>
        /// Gets a list with measured denomination details for the current currency
        /// </summary>
        /// <returns></returns>
        public IList<ScaleMeasurement> GetValues() => GetValues(Timeout);

        /// <summary>
        /// Gets a list with measured denomination details for the current currency
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public IList<ScaleMeasurement> GetValues(int timeout) => GetValues(0, timeout);

        /// <summary>
        /// Gets a list with measured denomination details for the current currency
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public IList<ScaleMeasurement> GetValues(int currencyIndex, int timeout) => GetValuesAsync(currencyIndex, timeout).RunTaskSynchronously();

        /// <summary>
        /// Removes a denomination from currency
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <returns></returns>
        public async Task RemoveDenominationAsync(int currencyIndex, int denominationIndex) => await RemoveDenominationAsync(currencyIndex, denominationIndex, Timeout);

        /// <summary>
        /// Removes a denomination from currency
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task RemoveDenominationAsync(int currencyIndex, int denominationIndex, int timeout)
        {
            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            if (currencyIndex < 0 || currencyIndex > MaxCurrencies)
                throw new ArgumentException($"Currency currencyIndex has to be between 0-{MaxCurrencies}.");

            if (currencyIndex == 0)
                currencyIndex = await GetCurrentCurrencyPositionAsync(timeout);

            var values = await GetValuesAsync(currencyIndex, timeout);
            for (var i = denominationIndex; i < values.Count - 1; i++)
            {
                var type = values[i + 1].CashType;

                var nextValue = i + 1 < values.Count ? (double)values[i + 1].Denomination / 100 : 0.0;
                var nextWeight = i + 1 < values.Count ? values[i + 1].Weight : 0.0;

                await SetDenominationValueAsync(currencyIndex, i, nextValue, timeout);
                await SetDenominationWeightAsync(currencyIndex, i, nextWeight, type, timeout);
            }

            await SetDenominationValueAsync(currencyIndex, values.Count - 1, 0, timeout);
            await SetDenominationWeightAsync(currencyIndex, values.Count - 1, 0, CashType.Coin, timeout);
        }

        /// <summary>
        /// Removes a denomination from currency
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <returns></returns>
        public void RemoveDenomination(int currencyIndex, int denominationIndex) => RemoveDenomination(currencyIndex, denominationIndex, Timeout);

        /// <summary>
        /// Removes a denomination from currency
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public void RemoveDenomination(int currencyIndex, int denominationIndex, int timeout) => RemoveDenominationAsync(currencyIndex, denominationIndex, timeout).RunTaskSynchronously();

        /// <summary>
        /// Set the value for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="newValue">new denomination value</param>
        /// <returns></returns>
        public async Task SetDenominationValueAsync(int currencyIndex, int denominationIndex, double newValue) => await SetDenominationValueAsync(currencyIndex, denominationIndex, newValue, Timeout);

        /// <summary>
        /// Set the value for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="newValue">new denomination value</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task SetDenominationValueAsync(int currencyIndex, int denominationIndex, double newValue, int timeout)
        {
            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            if (currencyIndex < 0 || currencyIndex > MaxCurrencies)
                throw new ArgumentException($"Currency currencyIndex has to be between 0-{MaxCurrencies}.");

            if (currencyIndex == 0)
                currencyIndex = await GetCurrentCurrencyPositionAsync(timeout);

            var value = BitConverter.GetBytes((int)(newValue * 100));
            byte[] valueCommand = { 0xfe, 0, value[3], value[2], value[1], value[0] };
            valueCommand[1] = (byte)((currencyIndex << 6) | denominationIndex + 1);

            // set denominationIndex
            await SendPacketAsync(valueCommand, timeout);

            // set factory-reset memory
            valueCommand[0] = 0xee;
            await SendPacketAsync(valueCommand, timeout);
        }

        /// <summary>
        /// Set the value for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="newValue">new denomination value</param>
        /// <returns></returns>
        public void SetDenominationValue(int currencyIndex, int denominationIndex, double newValue) => SetDenominationValue(currencyIndex, denominationIndex, newValue, Timeout);

        /// <summary>
        /// Set the value for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="newValue">new denomination value</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public void SetDenominationValue(int currencyIndex, int denominationIndex, double newValue, int timeout) => SetDenominationValueAsync(currencyIndex, denominationIndex, newValue, timeout).RunTaskSynchronously();

        /// <summary>
        /// Sets the coin or banknote weight for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="value">new denomination weight</param>
        /// <param name="cashType">type of denomination</param>
        /// <returns></returns>
        public async Task SetDenominationWeightAsync(int currencyIndex, int denominationIndex, double value, CashType cashType) => await SetDenominationWeightAsync(currencyIndex, denominationIndex, value, cashType, Timeout);

        /// <summary>
        /// Sets the coin or banknote weight for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="value">new denomination weight</param>
        /// <param name="cashType">type of denomination</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task SetDenominationWeightAsync(int currencyIndex, int denominationIndex, double value, CashType cashType, int timeout)
        {
            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            if (currencyIndex < 0 || currencyIndex > MaxCurrencies + 1)
                throw new ArgumentException($"Currency currencyIndex has to be between 0-{MaxCurrencies + 1}.");

            if (currencyIndex == MaxCurrencies + 1 && _protocolVersion < 6)
                throw new ArgumentException($"{MaxCurrencies + 1} currencyIndex is not available.");

            if (currencyIndex == 0)
                currencyIndex = await GetCurrentCurrencyPositionAsync(timeout);

            var weight = BitConverter.GetBytes(Convert.ToInt32(value * 10000));
            byte[] weightCommand = { 0xfd, (byte)currencyIndex, (byte)(denominationIndex + 1), (byte)(weight[2] + (byte)cashType), weight[1], weight[0] };

            // set denominationIndex
            await SendPacketAsync(weightCommand, timeout);

            // set factory-reset memory
            weightCommand[0] = 0xed;
            await SendPacketAsync(weightCommand, timeout);
        }

        /// <summary>
        /// Sets the coin or banknote weight for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="value">new denomination weight</param>
        /// <param name="cashType">type of denomination</param>
        /// <returns></returns>
        public void SetDenominationWeight(int currencyIndex, int denominationIndex, double value, CashType cashType) => SetDenominationWeight(currencyIndex, denominationIndex, value, cashType, Timeout);

        /// <summary>
        /// Sets the coin or banknote weight for a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="value">new denomination weight</param>
        /// <param name="cashType">type of denomination</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public void SetDenominationWeight(int currencyIndex, int denominationIndex, double value, CashType cashType, int timeout) => SetDenominationWeightAsync(currencyIndex, denominationIndex, value, cashType, Timeout).RunTaskSynchronously();

        /// <summary>
        /// Clears the denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <returns></returns>
        public async Task ClearDenomionationTextAsync(int currencyIndex, int denominationIndex) => await ClearDenomionationTextAsync(currencyIndex, denominationIndex, Timeout);

        /// <summary>
        /// Clears the denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task ClearDenomionationTextAsync(int currencyIndex, int denominationIndex, int timeout)
        {
            await SetDenomionationTextAsync(currencyIndex, denominationIndex, string.Empty, timeout);
        }

        /// <summary>
        /// Clears the denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <returns></returns>
        public void ClearDenomionationText(int currencyIndex, int denominationIndex) => ClearDenomionationText(currencyIndex, denominationIndex, Timeout);

        /// <summary>
        /// Clears the denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public void ClearDenomionationText(int currencyIndex, int denominationIndex, int timeout) => ClearDenomionationTextAsync(currencyIndex, denominationIndex, timeout).RunTaskSynchronously();

        /// <summary>
        /// Sets a 8-letter denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="text">new text label</param>
        /// <returns></returns>
        public async Task SetDenomionationTextAsync(int currencyIndex, int denominationIndex, string text) => await SetDenomionationTextAsync(currencyIndex, denominationIndex, text, Timeout);

        /// <summary>
        /// Sets a 8-letter denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="text">new text label</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task SetDenomionationTextAsync(int currencyIndex, int denominationIndex, string text, int timeout)
        {
            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            if (currencyIndex < 0 || currencyIndex > MaxCurrencies)
                throw new ArgumentException($"Currency currencyIndex has to be between 0-{MaxCurrencies}.");

            if (currencyIndex == 0)
                currencyIndex = await GetCurrentCurrencyPositionAsync(timeout);

            if (string.IsNullOrEmpty(text))
                text = "        ";

            if (text.Length < 8)
            {
                var len = text.Length;
                for (int i = 0; i < 8 - len; i++)
                    text += " ";
            }

            byte[] textCommand = { 0xbb, 0x00, 0x00, 0x00, 0x00, 0xaa };
            textCommand[1] = (byte)((currencyIndex << 6) | ++denominationIndex);
            if (text.Length > 8) text = text.Substring(0, 8);
            for (byte digitIndex = 1; digitIndex <= 8; digitIndex++)
            {
                textCommand[2] = digitIndex;
                var letter = digitIndex > text.Length ? new byte[] { 0x01, 0x01 } : GetLetterCode(text.Substring(digitIndex - 1, 1));
                textCommand[3] = letter[0];
                textCommand[4] = letter[1];

                await SendPacketAsync(textCommand, timeout);

                // set factory-reset memory
                var resetCommand = new byte[textCommand.Length];
                textCommand.CopyTo(resetCommand, 0);
                resetCommand[0] = 0xed;
                await SendPacketAsync(textCommand, timeout);
            }
        }

        /// <summary>
        /// Sets a 8-letter denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="text">new text label</param>
        /// <returns></returns>
        public void SetDenomionationText(int currencyIndex, int denominationIndex, string text) => SetDenomionationText(currencyIndex, denominationIndex, text, Timeout);

        /// <summary>
        /// Sets a 8-letter denomination text (RS 2000 devices only)
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="text">new text label</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public void SetDenomionationText(int currencyIndex, int denominationIndex, string text, int timeout) => SetDenomionationTextAsync(currencyIndex, denominationIndex, text, timeout).RunTaskSynchronously();

        /// <summary>
        /// Gets the measured weight
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetWeightAsync() => await GetWeightAsync(Timeout);

        /// <summary>
        /// Gets the measured weight
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task<double> GetWeightAsync(int timeout)
        {
            if (!IsConnected)
                throw new ArgumentException("RS scale is not connected.");

            var weightCommand = new byte[] { 0xd9, 0xaa, 0x55, 0xaa, 0x55, 0xaa };
            var buffer = await SendPacketAsync(weightCommand, timeout);

            return Convert.ToDouble(BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0)) * Math.Pow(10, -1);
        }

        /// <summary>
        /// Gets the measured weight
        /// </summary>
        /// <returns></returns>
        public double GetWeight() => GetWeight(Timeout);

        /// <summary>
        /// Gets the measured weight
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public double GetWeight(int timeout) => GetWeightAsync(timeout).RunTaskSynchronously();
        
        /// <summary>
        /// Gets the current float value
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetFloatValueAsync() => await GetFloatValueAsync(Timeout);

        /// <summary>
        /// Gets the current float value
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task<int> GetFloatValueAsync(int timeout)
        {
            var floatSettings = await GetFloatSettingsAsync(timeout);
            if (floatSettings[1] == 0)
                return 0;

            if (_protocolVersion == 1 || _protocolVersion == 2)
            {
                // received float value
                return BitConverter.ToInt32(floatSettings, 0);
            }

            // received float factor
            var denominationValues = await GetValuesAsync(0);
            var minBanknoteValue = denominationValues.Where(x => x.CashType == CashType.Banknote).Min(x => x.Denomination);
            var floatFactor = floatSettings[0];

            return (int)(floatFactor * minBanknoteValue);
        }

        /// <summary>
        /// Gets the current float value
        /// </summary>
        /// <returns></returns>
        public int GetFloatValue() => GetFloatValue(Timeout);

        /// <summary>
        /// Gets the current float value
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public int GetFloatValue(int timeout) => GetFloatValueAsync(timeout).RunTaskSynchronously();

        /// <summary>
        /// Gets float enabled-status indication (on/off)
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetFloatStatusAsync() => await GetFloatStatusAsync(Timeout);

        /// <summary>
        /// Gets float enabled-status indication (on/off)
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public async Task<bool> GetFloatStatusAsync(int timeout)
        {
            var floatSettings = await GetFloatSettingsAsync(timeout);
            return floatSettings[1] != 0;
        }

        /// <summary>
        /// Gets float enabled-status indication (on/off)
        /// </summary>
        /// <returns></returns>
        public bool GetFloatStatus() => GetFloatStatus(Timeout);

        /// <summary>
        /// Gets float enabled-status indication (on/off)
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        public bool GetFloatStatus(int timeout) => GetFloatStatusAsync(timeout).RunTaskSynchronously();

        /// <summary>
        /// Gets the scale protocol version
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        private async Task<int> GetProtocolVersionAsync(int timeout)
        {
            byte[] buffer;
            byte[] protocolCommand = { 0xe9, 0xaa, 0x55, 0xaa, 0x55, 0xaa };

            try
            {
                buffer = await SendPacketAsync(protocolCommand, timeout);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Cannot receive RS scale protocol version.", e);
            }

            var protocolVersion = Encoding.ASCII.GetString(buffer, 1, 4);
            var caption = _device.ReadProduct();

            switch (protocolVersion)
            {
                // type 1
                case "\0\0\02": return 1;
                // type 2
                case "0002":
                    if (caption == "iCount")
                        return 2;
                    // type 3-4
                    return caption == "RS 1000" ? 3 : 4;
                // type 5
                case "0003": return 5;
                // type 6
                case "0004": return 6;
                default:
                    throw new InvalidOperationException("Received unknown protocol version from RS scale.");
            }
        }

        /// <summary>
        /// Gets the recent used currency index
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        private async Task<int> GetCurrentCurrencyPositionAsync(int timeout)
        {
            var currencyName = await GetCurrencyNameAsync(0, timeout);
            for (var i = 1; i <= MaxCurrencies; i++)
            {
                if (currencyName == await GetCurrencyNameAsync(i, timeout))
                    return i;
            }

            return 0;
        }

        /// <summary>
        /// Gets the current float settings
        /// </summary>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        private async Task<byte[]> GetFloatSettingsAsync(int timeout)
        {
            var settingsCommand = new byte[] { 0xda, 0xaa, 0x55, 0xaa, 0x55, 0xaa };
            var receiveBuffer = await SendPacketAsync(settingsCommand, timeout);

            return receiveBuffer.Skip(1).Take(5).OrderByDescending(x => x).ToArray();
        }

        /// <summary>
        /// Get the cash type of a denomination
        /// </summary>
        /// <param name="currencyIndex">0: current currency, 1: first currency, 2: second currency, 3: third currency</param>
        /// <param name="denominationIndex">index of the denomination (0 to n-1)</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        private async Task<CashType> GetCashTypeAsync(int currencyIndex, int denominationIndex, int timeout)
        {
            byte[] typeCommand = { 0xfa, (byte)currencyIndex, (byte)denominationIndex, 0xaa, 0x55, 0xaa };
            var receiveBuffer = await SendPacketAsync(typeCommand, timeout);

            return receiveBuffer[3] - 128 >= 0 ? CashType.Banknote : CashType.Coin;
        }

        /// <summary>
        /// Sends a packet buffer
        /// </summary>
        /// <param name="buffer">byte buffer</param>
        /// <param name="timeout">milliseconds</param>
        /// <returns></returns>
        private async Task<byte[]> SendPacketAsync(byte[] buffer, int timeout)
        {
            if (buffer == null || buffer.Length != 6)
                throw new ArgumentException("Buffer is null or out of range. Expected 8 bytes to send.");

            if (!IsConnected)
                throw new InvalidOperationException("RS scale is not connected.");

            if (!IsOpen)
                throw new InvalidOperationException("No RS scale connection opened.");

            var sendBuffer = new byte[buffer.Length + 2];
            buffer.CopyTo(sendBuffer, 0);

            // add CRC to send buffer
            var crc = new Crc16Ccitt(InitialCrcValue.Zeros);
            crc.ComputeChecksumBytes(buffer).CopyTo(sendBuffer, buffer.Length);

            var written = await _device.WriteAsync(sendBuffer, timeout);
            if (written)
            {
                var read = await _device.ReadAsync(timeout);
                if (read.Status == HidDeviceData.ReadStatus.Success && read.Data.Length >= 6)
                    return read.Data.Skip(1).Take(6).ToArray();
            }

            throw new InvalidOperationException("Error transfering data packet to/from RS scale");
        }

        /// <summary>
        /// Get a single character for a given word
        /// </summary>
        /// <param name="low">low byte</param>
        /// <param name="high">high byte</param>
        /// <returns></returns>
        private static string GetLetterChar(byte low, byte high)
        {
            if (low == 0xd3 && high == 0xa8) { return "A"; }
            if (low == 0xca && high == 0x9b) { return "B"; }
            if (low == 0xd0 && high == 0x23) { return "C"; }
            if (low == 0xca && high == 0x1b) { return "D"; }
            if (low == 0xd1 && high == 0xa3) { return "E"; }
            if (low == 0xd1 && high == 0xa0) { return "F"; }
            if (low == 0xd0 && high == 0xab) { return "G"; }
            if (low == 0x13 && high == 0xa8) { return "H"; }
            if (low == 0xc8 && high == 0x13) { return "I"; }
            if (low == 0xc8 && high == 0x12) { return "J"; }
            if (low == 0x15 && high == 0x24) { return "K"; }
            if (low == 0x10 && high == 0x23) { return "L"; }
            if (low == 0x36 && high == 0x28) { return "M"; }
            if (low == 0x32 && high == 0x2c) { return "N"; }
            if (low == 0xd2 && high == 0x2b) { return "O"; }
            if (low == 0xd3 && high == 0xa0) { return "P"; }
            if (low == 0xd2 && high == 0x2f) { return "Q"; }
            if (low == 0xd3 && high == 0xa4) { return "R"; }
            if (low == 0xd1 && high == 0x8b) { return "S"; }
            if (low == 0xc8 && high == 0x10) { return "T"; }
            if (low == 0x12 && high == 0x2b) { return "U"; }
            if (low == 0x22 && high == 0x0c) { return "V"; }
            if (low == 0x1a && high == 0x6c) { return "W"; }
            if (low == 0x24 && high == 0x44) { return "X"; }
            if (low == 0x13 && high == 0x8b) { return "Y"; }
            if (low == 0xc4 && high == 0x43) { return "Z"; }

            if (low == 0x00 && high == 0x00) { return " "; }
            if (low == 0x00 && high == 0x01) { return "."; }

            if (low == 0xd2 && high == 0x2b) { return "0"; }
            if (low == 0x02 && high == 0x08) { return "1"; }
            if (low == 0xc3 && high == 0xa3) { return "2"; }
            if (low == 0xc3 && high == 0x8b) { return "3"; }
            if (low == 0x13 && high == 0x88) { return "4"; }
            if (low == 0xd1 && high == 0x8b) { return "5"; }
            if (low == 0xd1 && high == 0xab) { return "6"; }
            if (low == 0xc2 && high == 0x08) { return "7"; }
            if (low == 0xd3 && high == 0xab) { return "8"; }
            if (low == 0xd3 && high == 0x8b) { return "9"; }

            return string.Empty;
        }

        /// <summary>
        /// Get a character word for a given character
        /// </summary>
        /// <param name="letter"></param>
        /// <returns></returns>
        private static byte[] GetLetterCode(string letter)
        {
            switch (letter.ToLower())
            {
                case "a": return new byte[] { 0xd3, 0xa8 };
                case "b": return new byte[] { 0xca, 0x9b };
                case "c": return new byte[] { 0xd0, 0x23 };
                case "d": return new byte[] { 0xca, 0x1b };
                case "e": return new byte[] { 0xd1, 0xa3 };
                case "f": return new byte[] { 0xd1, 0xa0 };
                case "g": return new byte[] { 0xd0, 0xab };
                case "h": return new byte[] { 0x13, 0xa8 };
                case "i": return new byte[] { 0xc8, 0x13 };
                case "j": return new byte[] { 0xc8, 0x12 };
                case "k": return new byte[] { 0x15, 0x24 };
                case "l": return new byte[] { 0x10, 0x23 };
                case "m": return new byte[] { 0x36, 0x28 };
                case "n": return new byte[] { 0x32, 0x2c };
                case "o": return new byte[] { 0xd2, 0x2b };
                case "p": return new byte[] { 0xd3, 0xa0 };
                case "q": return new byte[] { 0xd2, 0x2f };
                case "r": return new byte[] { 0xd3, 0xa4 };
                case "s": return new byte[] { 0xd1, 0x8b };
                case "t": return new byte[] { 0xc8, 0x10 };
                case "u": return new byte[] { 0x12, 0x2b };
                case "v": return new byte[] { 0x22, 0x0c };
                case "w": return new byte[] { 0x1a, 0x6c };
                case "x": return new byte[] { 0x24, 0x44 };
                case "y": return new byte[] { 0x13, 0x8b };
                case "z": return new byte[] { 0xc4, 0x43 };

                case " ": return new byte[] { 0x00, 0x00 };
                case ".": return new byte[] { 0x00, 0x01 };

                case "0": return new byte[] { 0xd2, 0x2b };
                case "1": return new byte[] { 0x02, 0x08 };
                case "2": return new byte[] { 0xc3, 0xa3 };
                case "3": return new byte[] { 0xc3, 0x8b };
                case "4": return new byte[] { 0x13, 0x88 };
                case "5": return new byte[] { 0xd1, 0x8b };
                case "6": return new byte[] { 0xd1, 0xab };
                case "7": return new byte[] { 0xc2, 0x08 };
                case "8": return new byte[] { 0xd3, 0xab };
                case "9": return new byte[] { 0xd3, 0x8b };

                default: throw new ArgumentException("Invalid character");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
