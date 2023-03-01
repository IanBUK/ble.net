// Copyright M. Griffie <nexus@nexussays.com>
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Runtime.InteropServices;
using nexus.core;
using nexus.core.logging;

namespace nexus.protocols.ble.scan.advertisement.link
{
   /// <summary>
   /// A link-layer BLE packet. 10-47 Bytes
   /// TODO: In-progress
   /// </summary>
   [StructLayout( LayoutKind.Sequential, Pack = 1 )]
   internal struct BlePacket
   {
      public Byte preamble;
      public UInt32 accessAddress;
      /// <summary>
      /// 5-42 bytes representing the joined (2-39 byte) PDU data packet and its (3-byte) CRC
      /// </summary>
      public Byte[ /*5-42*/] pduAndCrc;

      /// <summary>
      /// Either an advertisement or data PDU. <see cref="PduAsAdvertisement" />
      /// </summary>
      public Byte[ /*2-39*/] Pdu
      {
         get
         {
            if(pduAndCrc != null && pduAndCrc.Length > 3)
            {
               try
               {
                  return pduAndCrc.Slice( 0, pduAndCrc.Length - 3 );
               }
               catch (Exception e)
               {
                  Log.Trace($"Exception in Pdu: {e.Message}.");
               }
            }
            return new Byte[0];
         }
      }

      /// <summary>
      /// The 3 byte CRC
      /// </summary>
      public Byte[ /*3*/] Crc
      {
         get
         {
            if(pduAndCrc != null && pduAndCrc.Length > 3)
            {
               try
               {
                  return pduAndCrc.Slice( pduAndCrc.Length - 4 );
               }
               catch (Exception e)
               {
                  Log.Trace($"Exception in Crc: {e.Message}.");
               }

            }
            return new Byte[3];
         }
      }

      /// <summary>
      /// Parse <see cref="Pdu" /> as an advertisement
      /// </summary>
      public AdvertisingChannelPdu PduAsAdvertisement()
      {
         try
         {
            if (!this.IsAdvertisingChannelPDU())
            {
               return default(AdvertisingChannelPdu);
            }

            var pdu = Pdu;
            // TODO: Can I just memory map the byte array into a AdvertisingChannelPDU struct?
            return new AdvertisingChannelPdu { header = (UInt16)pdu.Slice(0, 2).ToInt16(), payload = pdu.Slice(3) };
         }
         catch (Exception e)
         {
            Log.Trace($"Exception in PduAsAdvertisement: {e.Message}.");
         }

         return new AdvertisingChannelPdu();

      }
   }
}
