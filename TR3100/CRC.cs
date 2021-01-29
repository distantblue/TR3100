using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TR3100
{
    public static class CRC
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /*
            The process of simple CRC calculation using a shift register is as follow:
            1. Initialize the register with 0.
            2. Shift in the input stream bit by bit. If the popped out MSB is a '1', XOR the register value with the generator polynomial.
            3. If all input bits are handled, the CRC shift register contains the CRC value.

            Following standard parameters are used to define a CRC algorithm instance:
            1. Name: Well, a CRC instance has to be identified somehow, so each public defined CRC parameter set has a name like e.g. CRC-16/CCITT.
            2. Width (in bits): Defines the width of the result CRC value (n bits). 
            Simultaneously, also the width of the generator polynomial is defined (n+1 bits). 
            Most common used widths are 8, 16 and 32 bit. 
            But thereotically all widths beginning from 1 are possible. 
            In practice, even quite big (80 bit) or uneven (5 bit or 31 bit) widths are used.
            3. Polynomial: Used generator polynomial value. 
            Note that different respresentations exist, see chapter 7.2.
            4. Initial Value: The value used to initialize the CRC value / register. 
            In the examples above, always zero is used, but it could be any value.
            5. Input reflected: If this value is TRUE, each input byte is reflected before being used in the calculation. 
            Reflected means that the bits of the input byte are used in reverse order. 
            So this also means that bit 0 is treated as the most significant bit and bit 7 as least significant.
            Example with byte 0x82 = b10000010: Reflected(0x82) = Reflected(b10000010) = b01000001 = 0x41.
            6. Result reflected: If this value is TRUE, the final CRC value is reflected before being returned. 
            The reflection is done over the whole CRC value, so e.g. a CRC-32 value is reflected over all 32 bits.
            7. Final XOR value: The Final XOR value is xored to the final CRC value before being returned. This is done after the 'Result reflected' step. 
            Obviously a Final XOR value of 0 has no impact.
            8. Check value [Optional]: This value is not required but often specified to help to validate the implementation. 
            This is the CRC value of input string "123456789" or as byte array: [0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39].
        */
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// CRC-8 non-reversive algorithm
        /// </summary>
        /// <param name="data">Data bytes</param>
        /// <returns>8-bit CRC value</returns>
        private static byte CRC8_non_reversive_algorithm(byte[] data, byte poly, byte crc)
        {
            foreach (var item in data) // For each byte in buffer
            {
                crc ^= item; // Carry out XOR with CRC 

                for (int i = 0; i < 8; i++) // For each bit in byte
                {
                    if ((crc & 0x80) != 0) // If MSB = 1
                    {
                        crc <<= 1;
                        crc ^= poly;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// CRC-8 reversive algorithm
        /// </summary>
        /// <param name="data">Data bytes</param>
        /// <returns>8-bit CRC value</returns>
        private static byte CRC8_reversive_algorithm(byte[] data, byte poly, byte crc)
        {
            foreach (var item in data) // For each byte in buffer
            {
                crc ^= item; // Carry out XOR with CRC 

                for (int i = 0; i < 8; i++) // For each bit in byte
                {
                    if ((crc & 0x01) != 0) // If LSB = 1
                    {
                        crc >>= 1;
                        crc ^= poly;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// CRC-16 non-reversive algorithm
        /// </summary>
        /// <param name="data">Data bytes</param>
        /// <param name="poly">Generator polynomial</param>
        /// <param name="crc">Initial CRC-value</param>
        /// <returns>16-bit CRC value</returns>
        private static ushort CRC16_non_reversive_algorithm(byte[] data, ushort poly, ushort crc)
        {
            foreach (var item in data) // For each byte in buffer
            {
                crc ^= (ushort)(item << 8); // Move byte into HI-byte to be the same length as 16bit CRC

                for (int i = 0; i < 8; i++) // For each bit in byte
                {
                    if ((crc & 0x8000) != 0) // If MSB = 1
                    {
                        crc <<= 1;
                        crc ^= poly;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// CRC-16 non-reversive algorithm with XOR-out
        /// </summary>
        /// <param name="data">Data bytes</param>
        /// <param name="poly">Generator polynomial</param>
        /// <param name="crc">Initial CRC-value</param>
        /// <param name="XOR_out_value">XOR-out value</param>
        /// <returns>16-bit CRC value</returns>
        private static ushort CRC16_XOR_out_non_reversive_algorithm(byte[] data, ushort poly, ushort crc, ushort XOR_out_value)
        {
            ushort res = CRC16_non_reversive_algorithm(data, poly, crc);
            return res ^= XOR_out_value;
        }

        /// <summary>
        /// CRC-16 reversive algorithm
        /// </summary>
        /// <param name="data">Data bytes</param>
        /// <param name="poly">Generator polynomial (reversed value)</param>
        /// <param name="crc">Initial CRC-value</param>
        /// <returns>16-bit CRC value</returns>
        private static ushort CRC16_reversive_algorithm(byte[] data, ushort poly, ushort crc)
        {
            foreach (var item in data) // For each byte in buffer
            {
                crc ^= item; // Carry out XOR with CRC 

                for (int i = 0; i < 8; i++) // For each bit in byte
                {
                    if ((crc & 0x0001) != 0) // If LSB of CRC == 1
                    {
                        crc >>= 1;
                        crc ^= poly;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// CRC-16 reversive algorithm with XOR-out
        /// </summary>
        /// <param name="data">Data bytes</param>
        /// <param name="poly">Generator polynomial</param>
        /// <param name="crc">Initial CRC-value</param>
        /// <param name="XOR_out_value">XOR-out value</param>
        /// <returns>16-bit CRC value</returns>
        private static ushort CRC16_XOR_out_reversive_algorithm(byte[] data, ushort poly, ushort crc, ushort XOR_out_value)
        {
            ushort res = CRC16_reversive_algorithm(data, poly, crc);
            return res ^= XOR_out_value;
        }

        /// <summary>
        /// Method reverses(reflects) bits order in byte
        /// </summary>
        /// <param name="reg">8-bit register</param>
        /// <returns>8-bit value</returns>
        public static byte ReverseOrderOfBits(byte reg)
        {
            byte result = 0x00;

            for (byte mask = 0x80; mask > 0; mask >>= 1) // 0x80 (HEX) - 1000 0000 (BIN)
            {
                result >>= 1;
                byte temp = (byte)(reg & mask);

                if (temp != 0) // If MSB == 1
                {
                    result |= 0x80;
                }
            }
            return result;
        }

        //////////////////////////////////////////////////////////////
        ///The most common CRC-standards
        //////////////////////////////////////////////////////////////
        ///

        // Generator polynomial: 0x07
        // Initial CRC-value: 0x00
        public static byte CRC8(byte[] data)
        {
            return CRC8_non_reversive_algorithm(data, 0x07, 0x00);
        }

        // Generator polynomial: 0x9B
        // Initial CRC-value: 0xFF
        public static byte CRC8_CDMA_2000(byte[] data)
        {
            return CRC8_non_reversive_algorithm(data, 0x9B, 0xFF);
        }

        // Generator polynomial: 0xD5
        // Initial CRC-value: 0x00
        public static byte CRC8_DVB_S2(byte[] data)
        {
            return CRC8_non_reversive_algorithm(data, 0xD5, 0x00);
        }

        // Generator polynomial: 0x1D
        // Initial CRC-value: 0xFD
        public static byte CRC8_I_CODE(byte[] data)
        {
            return CRC8_non_reversive_algorithm(data, 0x1D, 0xFD);
        }

        // Generator polynomial: 0x9C (reversed 0x39)
        // Initial CRC-value: 0x00
        public static byte CRC8_DARC(byte[] data)
        {
            return CRC8_reversive_algorithm(data, 0x9C, 0x00);
        }

        // Generator polynomial: 0xB8 (reversed 0x1D)
        // Initial CRC-value: 0xFF
        public static byte CRC8_EBU(byte[] data)
        {
            return CRC8_reversive_algorithm(data, 0xB8, 0xFF);
        }

        // Generator polynomial: 0x8C (reversed 0x31)
        // Initial CRC-value: 0x00
        public static byte CRC8_MAXIM(byte[] data)
        {
            return CRC8_reversive_algorithm(data, 0x8C, 0x00);
        }

        // Generator polynomial: 0xE0 (reversed 0x07)
        // Initial CRC-value: 0xFF
        public static byte CRC8_ROHC(byte[] data)
        {
            return CRC8_reversive_algorithm(data, 0xE0, 0xFF);
        }

        // Generator polynomial: 0xD9 (reversed 0x9B)
        // Initial CRC-value: 0x00
        public static byte CRC8_WCDMA(byte[] data)
        {
            return CRC8_reversive_algorithm(data, 0xD9, 0x00);
        }

        // Generator polynomial: 0x1021
        // Initial CRC-value: 0x0000
        public static ushort CRC16_XMODEM(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0x1021, 0x0000);
        }

        // Generator polynomial: 0xA097
        // Initial CRC-value: 0x0000
        public static ushort CRC16_TELEDISK(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0xA097, 0x0000);
        }

        // Generator polynomial: 0x8BB7
        // Initial CRC-value: 0x0000
        public static ushort CRC16_T10_DIF(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0x8BB7, 0x0000);
        }

        // Generator polynomial: 0x0589
        // Initial CRC-value: 0x0000
        public static ushort CRC16_DECT_X(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0x0589, 0x0000);
        }

        // Generator polynomial: 0x8005
        // Initial CRC-value: 0x800D
        public static ushort CRC16_DDS_110(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0x8005, 0x800D);
        }

        // Generator polynomial: 0xC867
        // Initial CRC-value: 0xFFFF
        public static ushort CRC16_CDMA_2000(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0xC867, 0xFFFF);
        }

        // Generator polynomial: 0x8005
        // Initial CRC-value: 0x0000
        public static ushort CRC16_BUYPASS(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0x8005, 0x0000);
        }

        // Generator polynomial: 0x1021
        // Initial CRC-value: 0x1D0F
        public static ushort CRC16_AUG_CCITT(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0x1021, 0x1D0F);
        }

        // Generator polynomial: 0x1021
        // Initial CRC-value: 0xFFFF
        public static ushort CRC16_CCITT_FALSE(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0x1021, 0xFFFF);
        }

        // Generator polynomial: 0xA001
        // Initial CRC-value: 0x0000
        public static ushort CRC16_IBM(byte[] data)
        {
            return CRC16_non_reversive_algorithm(data, 0xA001, 0x0000);
        }

        // Generator polynomial: 0x0589
        // Initial CRC-value: 0x0000
        // XOR-out value: 0x0001
        public static ushort CRC16_DECT_R(byte[] data)
        {
            return CRC16_XOR_out_non_reversive_algorithm(data, 0x0589, 0x0000, 0x0001);
        }

        // Generator polynomial: 0x3D65
        // Initial CRC-value: 0x0000
        // XOR-out value: 0xFFFF
        public static ushort CRC16_EN_13757(byte[] data)
        {
            return CRC16_XOR_out_non_reversive_algorithm(data, 0x3D65, 0x0000, 0xFFFF);
        }

        // Generator polynomial: 0x1021
        // Initial CRC-value: 0xFFFF
        // XOR-out value: 0xFFFF
        public static ushort CRC16_GENIBUS(byte[] data)
        {
            return CRC16_XOR_out_non_reversive_algorithm(data, 0x1021, 0xFFFF, 0xFFFF);
        }

        // Generator polynomial: 0xA001 (reversed 0x8005 value)
        // Initial CRC-value: 0xFFFF
        public static ushort CRC16_MODBUS(byte[] data)
        {
            return CRC16_reversive_algorithm(data, 0xA001, 0xFFFF);
        }

        // Generator polynomial: 0xA001 (reversed 0x8005 value)
        // Initial CRC-value: 0x0000
        public static ushort CRC16_ARC(byte[] data)
        {
            return CRC16_reversive_algorithm(data, 0xA001, 0x0000);
        }

        // Generator polynomial: 0x8408 (reversed 0x1021 value)
        // Initial CRC-value: 0x0000
        public static ushort CRC16_KERMIT(byte[] data)
        {
            return CRC16_reversive_algorithm(data, 0x8408, 0x0000);
        }

        // Generator polynomial: 0x8408 (reversed 0x1021 value)
        // Initial CRC-value: 0x0000
        public static ushort CRC16_CCITT(byte[] data)
        {
            return CRC16_reversive_algorithm(data, 0x8408, 0x0000);
        }

        // Generator polynomial: 0xA6BC (reversed 0x3D65 value)
        // Initial CRC-value: 0x0000
        // XOR-out value: 0xFFFF
        public static ushort CRC16_DNP(byte[] data)
        {
            return CRC16_XOR_out_reversive_algorithm(data, 0xA6BC, 0x0000, 0xFFFF);
        }

        // Generator polynomial: 0xA001 (reversed 0x8005 value)
        // Initial CRC-value: 0x0000
        // XOR-out value: 0xFFFF
        public static ushort CRC16_MAXIM(byte[] data)
        {
            return CRC16_XOR_out_reversive_algorithm(data, 0xA001, 0x0000, 0xFFFF);
        }

        // Generator polynomial: 0xA001 (reversed 0x8005 value)
        // Initial CRC-value: 0xFFFF
        // XOR-out value: 0xFFFF
        public static ushort CRC16_USB(byte[] data)
        {
            return CRC16_XOR_out_reversive_algorithm(data, 0xA001, 0xFFFF, 0xFFFF);
        }

        // Generator polynomial: 0x8408 (reversed 0x1021 value)
        // Initial CRC-value: 0xFFFF
        // XOR-out value: 0xFFFF
        public static ushort CRC16_X_25(byte[] data)
        {
            return CRC16_XOR_out_reversive_algorithm(data, 0x8408, 0xFFFF, 0xFFFF);
        }
    }
}
